#region License
// Copyright (c) 2019 Teramine Ltd
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using log4net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Teramine.Interconnect.Exceptions;
using Teramine.Interconnect.Framing;
using Teramine.Interconnect.Interfaces;
using Teramine.Interconnect.Models;
using Teramine.Interconnect.Types;


namespace Teramine.Interconnect
{
    public class Session : ISession
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<Message> OnMessage;

        public uint ID { get; private set; }
        public string Name { get { return Descriptor?.Name; } }
        public SessionDescriptor Descriptor { get; }
        public Connection Connection { get; }

        private IdProvider _messageIdProvider;
        private Dictionary<uint, Message> _pendingMessages = new Dictionary<uint, Message>();

        internal SessionState State { get; private set; } = SessionState.Start;
        internal ManualResetEvent Settled { get; set; } = new ManualResetEvent(false);
        internal ManualResetEvent SessionError { get; set; } = new ManualResetEvent(false);


        internal Session(Connection connection, SessionDescriptor sessionDescriptor, uint id)
        {
            Connection = connection;
            Connection.OnClosed += _onConnectionClosed;
            Descriptor = sessionDescriptor;
            ID = id;
            _messageIdProvider = new IdProvider(Connection.IsClient ? 0u : 1u, 2);
        }


        /// <summary>
        /// Call to signal the acceptance of the incoming session
        /// </summary>
        /// <param name="data">Application data to return to the session initiator</param>
        public void Accept(byte[] data = null)
        {
            // accept the session
            Connection.AcceptSession(ID, data);

            // do not signal - this will be signalled when the session open frame is received/confirmed
        }

        /// <summary>
        /// Call to signal the rejection of the incoming session
        /// </summary>
        /// <param name="data">Application data to return to the session initiator</param>
        public void Reject(byte[] data = null)
        {
            Connection.RejectSession(ID, data);

            // signal a session error to release the blocking call
            SessionError.Set();
        }

        /// <summary>
        /// Send application data to the connected endpoint
        /// </summary>
        /// <remarks>This method is blocking</remarks>
        /// <param name="payload"></param>
        public Response Send(byte[] payload)
        {
            var id = _messageIdProvider.Next();

            try
            {
                // disallow sending if the session is not in the open state
                if (State != SessionState.Open)
                    throw new SessionException();

                // create message with next message ID - do not add the payload to the message as message is just for tracking
                var message = new Message(this, id, null);

                // add to the message pending settlement collection - will be accessed async
                lock(_pendingMessages)
                {
                    _pendingMessages.Add(message.ID, message);
                }

                // pass the message down to the connection for transmission
                Connection.SendMessage(ID, message.ID, payload);

                // wait for the message to be settled or time out
                if (!message.Settled.WaitOne(60000))
                {
                    throw new MessageTimeoutException();
                }

                // return the response
                return new Response(message.Accepted, message.ResponseData);
            }
            finally
            {
                lock(_pendingMessages)
                {
                    _pendingMessages.Remove(id);
                }

                _messageIdProvider.Remove(id);
            }
        }

        public void Close()
        {
            Log.DebugFormat("Closing Session: ID [{0}], Name [{1}], Pending [{2}]", ID, Descriptor.Name, _pendingMessages.Count);

            // set state to prevent any further sends
            State = SessionState.End;

            // remove any pending messages
            _pendingMessages.Clear();

            // reset message id provider
            _messageIdProvider.Reset();

            // Send session close frame
            Connection.CloseSession(ID);
        }

        #region Session internals

        internal void NotifySessionOpen()
        {
            // state is now open
            State = SessionState.Open;

            // signal that the session is now settled
            Settled.Set();
        }

        internal void NotifyConnectionClose()
        {
            Log.DebugFormat("Closing Session: ID [{0}], Name [{1}], Pending [{2}]", ID, Descriptor.Name, _pendingMessages.Count);

            // set state to prevent any further sends
            State = SessionState.End;

            // remove any pending messages
            _pendingMessages.Clear();

            // do not send Session close - connection already closed so no point
        }

        #endregion

        #region Messaging internals

        /// <summary>
        /// Called by Connection when a new message (send/accept/reject) has been received on the session
        /// </summary>
        /// <param name="m">The message received on the connection</param>
        internal void NotifyMessage(FrameType type, uint id, byte[] payload)
        {
            // prevent processing of message if the session is not open
            if (State != SessionState.Open)
                return;

            switch (type)
            {
                case FrameType.MessageSend:
                    {
                        // wrap the data in a message for the application
                        var message = new Message(this, id, payload);

                        // invoke event hanlder asynchronously
                        _ = Task.Run(() => OnMessage?.Invoke(this, message));

                    } break;
                case FrameType.MessageAccept:
                    {
                        lock (_pendingMessages)
                        {
                            if (_pendingMessages.ContainsKey(id))
                            {
                                // set result fields and then signal that message is complete
                                _pendingMessages[id].Accepted = true;
                                _pendingMessages[id].ResponseData = payload;
                                _pendingMessages[id].Settled.Set();
                            }
                        }

                    } break;
                case FrameType.MessageReject:
                    {
                        lock (_pendingMessages)
                        {
                            if (_pendingMessages.ContainsKey(id))
                            {
                                // set result fields and then signal that message is complete
                                _pendingMessages[id].Accepted = false;
                                _pendingMessages[id].ResponseData = payload;
                                _pendingMessages[id].Settled.Set();
                            }
                        }

                    } break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called by the Message to trigger a sending of an Accept frame to the sender
        /// </summary>
        /// <param name="messageId">The ID of the message</param>
        /// <param name="data">Optional additional response data</param>
        internal void AcceptMessage(uint messageId, byte[] data = null)
        {
            // prevent calling down to the connection if the session is not open
            if (State != SessionState.Open)
                return;

            // call down to the connection
            Connection.AcceptMessage(ID, messageId, data);
        }

        /// <summary>
        /// Called by the Message to trigger the sending of a Reject frame to the sender
        /// </summary>
        /// <param name="messageId">The ID of the message</param>
        /// <param name="data">Optional additional response data</param>
        internal void RejectMessage(uint messageId, byte[] data = null)
        {
            // prevent calling down to the connection if the session is not open
            if (State != SessionState.Open)
                return;

            // call down to the connection
            Connection.RejectMessage(ID, messageId, data);
        }

        #endregion


        private void _onConnectionClosed(object sender, EventArgs e)
        {
            State = SessionState.End;
        }
    }
}
