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

using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Teramine.DSC.Interconnect.Exceptions;
using Teramine.DSC.Interconnect.Framing;
using Teramine.DSC.Interconnect.Models;
using Teramine.DSC.Interconnect.Types;
using Teramine.DSC.Interconnect.Codecs;
using log4net;
using System.Net;
using Teramine.DSC.Interconnect.Interfaces;
using Teramine.DSC.Interconnect.Factories;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

[assembly: InternalsVisibleTo("Teramine.DSC.Interconnect.Test")]
namespace Teramine.DSC.Interconnect
{
    public class Connection : IConnection
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<Session> OnSession;
        public event EventHandler OnClosed;
        public event EventHandler OnError;

        public string ID { get; }
        public Dictionary<string,string> Data { get; private set; }
        public bool IsClient { get; }
        public IComponentFactory ComponentFactory { get; set; } = new ComponentFactory();

        internal ConnectionState _state = ConnectionState.Start;
        internal ConnectionState State
        {
            get { return _state; }
            set
            {
                // all of this is temporary for development and debugging
                if(_state != value)
                {
                    Log.DebugFormat("Connection state change: From [{0}], To [{1}]", _state, value);
                    _state = value;
                }
            }
        }

        private List<IPAddress> _ipAddresses;
        private int _port;
        private ITcpClient _tcpClient;
        private INetworkStream _networkStream;
        private SslSettings _sslSettings;

        private Task _streamDecoderTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private IdProvider _sessionIdProvider;
        private ManualResetEvent _connectionOpen = new ManualResetEvent(false);
        private ManualResetEvent _connectionError = new ManualResetEvent(false);
        private Dictionary<uint, Session> _pendingSessions = new Dictionary<uint, Session>();    
        private Dictionary<uint, Session> _activeSessions = new Dictionary<uint, Session>();    

        internal Connection(SslSettings sslSettings)
        {
            _sslSettings = sslSettings;
        }

        /// <summary>
        /// Constructor for incoming connections from clients
        /// </summary>
        /// <param name="client"></param>
        internal Connection(ITcpClient client, SslSettings sslSettings) : this(sslSettings)
        {
            // set random ID and connection direction
            ID = Guid.NewGuid().ToString().Substring(0, 8);
            IsClient = false;

            _tcpClient = client;
            _networkStream = _tcpClient.GetStream();
            _networkStream.AuthenticateAsServer();

            // create session ID provider - IDs are ODD for server-side initiated sessions
            _sessionIdProvider = new IdProvider(1, 2);

            // create and start frame decoder
            _streamDecoderTask = _decodeStreamAsync(_networkStream, _cts.Token);

            // block until the connection is active or it timesout
            var index = WaitHandle.WaitAny(new[] { _connectionOpen, _connectionError }, 2000);

            switch (index)
            {
                case 0: return;
                case 1: throw new ConnectionException();
                case WaitHandle.WaitTimeout: throw new ConnectionTimeoutException();
                default: throw new Exception();
            }
        }

        /// <summary>
        /// Constructor for client initiated connections
        /// </summary>
        /// <param name="address">The address of the server to connect to</param>
        /// <param name="port">the port of the server to connect</param>
        public Connection(string address, int port, SslSettings sslSettings, Dictionary<string, string> data = null) : this(sslSettings)
        {
            // set random ID and connection direction
            ID = Guid.NewGuid().ToString().Substring(0, 8);
            IsClient = true;

            _ipAddresses = new List<IPAddress>();
            _port = port;
            _sessionIdProvider = new IdProvider(0, 2);

            IPAddress ip;
            if (IPAddress.TryParse(address, out ip))
            {
                _ipAddresses.Add(ip);
            }
            else
            {
                _ipAddresses.AddRange(Utilities.GetHostAddressesAsync(address).ConfigureAwait(false).GetAwaiter().GetResult());
            }

            _connect(data);
        }

        /// <summary>
        /// Creates a new session between both endpoints
        /// </summary>
        /// <param name="name">The application-defined name for the session</param>
        /// <param name="data">Optional payload data to be sent to the remote endpoint</param>
        /// <returns>The created session</returns>
        public Session CreateSession(string name, byte[] data = null)
        {
            if (State != ConnectionState.Open)
                throw new ConnectionException();

            uint id = uint.MaxValue;

            try
            {
                // create the session descriptor which is sent to the remote endpoint
                var descriptor = new SessionDescriptor
                {
                    Name = name,
                    Data = data
                };

                // construct new session frame
                Session session;
                lock (_pendingSessions)
                {
                    id = _sessionIdProvider.Next();
                    session = new Session(this, descriptor, id);
                    _pendingSessions.Add(session.ID, session);
                }

                // send the session start frame
                _sendFrame(FrameType.SessionStart, Frame.CreateSessionStartFrame(session.ID, BsonCodec.Encode(descriptor)));

                // block until the connection is settled or a timeout occurs
                var index = WaitHandle.WaitAny(new[] { session.Settled, session.SessionError }, 5000);

                switch (index)
                {
                    // session was settled (application needs to check if it was accepted/rejected
                    case 0: return session;
                    // the session was rejected by the remote endpoint
                    case 1: throw new SessionRejectedException($"The session was rejected by the remote endpoint");
                    // the session creation timedout
                    case WaitHandle.WaitTimeout: throw new SessionTimeoutException($"Session settlement timed out after {5000} ms");
                    // should never hit here, but have to return something
                    default: throw new Exception();
                }
            }
            catch(Exception)
            {
                lock (_pendingSessions)
                {
                    if(_pendingSessions.ContainsKey(id))
                        _pendingSessions.Remove(id);

                    _sessionIdProvider.Remove(id);
                }
                    
                throw;
            }
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Close()
        {
            // already closed/not established - nothing to do
            if (State == ConnectionState.End)
                return;

            Log.DebugFormat("Closing Connection: ID [{0}]", ID);

            // send connection close frame
            _sendFrame(FrameType.ConnectionClose, Frame.CreateConnectionCloseFrame());

            // close and clear any pending sessions
            _closeInternal();

            // set state to end
            State = ConnectionState.End;
        }

        /// <summary>
        /// Disposes of the resources
        /// </summary>
        public void Dispose()
        {
            // call close to ensure everything cleaned up - only do so if NOT in errored state - if so it would already have been called
            if(State != ConnectionState.ConnectionError)
                Close();

            try
            {
                // dispose of network stream
                _networkStream.Dispose();
            }
            catch { _networkStream = null; }

            try
            {
                // dispose of tcpclient
                _tcpClient.Dispose();
            }
            catch { _tcpClient = null; }
        }


        #region Session callbacks

        internal void AcceptSession(uint sessionId, byte[] data = null)
        {
            // move unsettled sesison to settled state
            if(_pendingSessions.ContainsKey(sessionId))
            {
                // send acceptance frame
                _sendFrame(FrameType.SessionAccept, Frame.CreateSessionAcceptFrame(sessionId, data));
            }
        }

        internal void RejectSession(uint sessionId, byte[] data = null)
        {
            // remove pending session and send reject frame
            Session session;
            if (_pendingSessions.Remove(sessionId, out session))
            {
                // send reject frame
                _sendFrame(FrameType.SessionReject, Frame.CreateSessionRejectFrame(sessionId, data));
            }
        }

        internal void CloseSession(uint sessionId, byte[] data = null)
        {
            // move unsettled sesison to settled state
            Session session;
            if (_activeSessions.Remove(sessionId, out session))
            {
                // send acceptance frame
                _sendFrame(FrameType.SessionClose, Frame.CreateSessionOpenFrame(sessionId, data));
            }
        }

        #endregion

        #region Message callbacks

        internal void SendMessage(uint sessionId, uint messageId, byte[] data = null)
        {
            if (_activeSessions.ContainsKey(sessionId))
            {
                _sendFrame(FrameType.MessageSend, Frame.CreateMessageSendFrame(sessionId, messageId, data));
            }
        }

        internal void AcceptMessage(uint sessionId, uint messageId, byte[] data = null)
        {
            if(_activeSessions.ContainsKey(sessionId))
            {
                _sendFrame(FrameType.MessageAccept, Frame.CreateMessageAcceptFrame(sessionId, messageId, data));
            }
        }

        internal void RejectMessage(uint sessionId, uint messageId, byte[] data = null)
        {
            if (_activeSessions.ContainsKey(sessionId))
            {
                _sendFrame(FrameType.MessageReject, Frame.CreateMessageAcceptFrame(sessionId, messageId, data));
            }
        }

        #endregion

        #region Transport

        private void _connect(Dictionary<string, string> data = null)
        {
            for (int i = 0; i < _ipAddresses.Count; i++)
            {
                try
                {
                    // if the IP address is not supported then skip, e.g. IPv6 address but not supported by network card
                    if (_ipAddresses[i] == null ||
                        (_ipAddresses[i].AddressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4) ||
                        (_ipAddresses[i].AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6))
                    {
                        continue;
                    }

                    // attempt to connect to remote endpoint (will throw if not available)
                    _tcpClient = ComponentFactory.BuildTcpClient(_ipAddresses[i].ToString(), _port, _sslSettings);
                    _networkStream = _tcpClient.GetStream();
                    _networkStream.AuthenticateAsClient();

                    Log.InfoFormat("[{0}] Connected to {1}:{2}", ID, _ipAddresses[i], _port);

                    break;
                }
                catch (Exception e)
                {
                    Log.ErrorFormat($"Unable to connect to remote endpoint: {e.Message}");

                    _tcpClient = null;
                    _networkStream = null;
                }
            }

            // did one of the addresses make a connection
            if (_tcpClient != null && _networkStream != null)
            {
                // create and start the frame decoder
                _streamDecoderTask = _decodeStreamAsync(_networkStream, _cts.Token);

                // update state and send start frame
                State = ConnectionState.ConnectionStartSent;
                _sendFrame(FrameType.ConnectionStart, Frame.CreateConnectionStartFrame(data));

                // block until the connection is established, errors, or times out
                var index = WaitHandle.WaitAny(new[] { _connectionOpen, _connectionError }, 2000);

                // what was the result?
                switch (index)
                {
                    // success - return the established connection
                    case 0: return;
                    // the connection failed
                    case 1: throw new ConnectionException("Unable to establish connection");
                    // establishing the connection timed out
                    case WaitHandle.WaitTimeout: throw new ConnectionTimeoutException("Establishing a conneciton the remote endpoint timed out");
                    // should never each this point
                    default: throw new Exception();
                }
            }
            else
            {
                // failed to connect to the remote endpoint
                throw new ConnectionException("Unable to establish connection");
            }
        }

        /// <summary>
        /// Asynchrnously sends a frame over the connection
        /// </summary>
        /// <param name="type">DEBUG ONLY: The type of the frame being sent</param>
        /// <param name="frame">The binary of the frame to be sent</param>
        private void _sendFrame(FrameType type, byte[] frame)
        {
            if (State == ConnectionState.End || State == ConnectionState.ConnectionError)
                return;

            try
            {
                Log.DebugFormat("[{0}] Sending frame: Type [{1}]", ID, type.ToString());
                _networkStream.WriteAsync(frame, 0, frame.Length, _cts.Token).ConfigureAwait(false);
            }
            catch(Exception)
            {
                // change connection state to errored first
                State = ConnectionState.ConnectionError;

                // close dependent components and resources
                _closeInternal();

                // raise the OnError event - application should call Dispose()
                OnError?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Processes a received frame depending on frame type and current connection state
        /// </summary>
        /// <param name="frame">The frame to process</param>
        private void _processFrame(Frame frame)
        {
            // do NOT process anything if in a closed or errored state
            if (State == ConnectionState.End || State == ConnectionState.ConnectionError)
                return;

            switch(frame.Type)
            {
                #region Connection Frame Handling

                case FrameType.ConnectionStart:
                    {
                        if(State == ConnectionState.Start)
                        {
                            Log.DebugFormat("[{0}] FrameType.ConnectionStart received", ID);

                            // decode connection descriptor content
                            var descriptor = BsonCodec.Decode<ConnectionDescriptor>(frame.Payload);

                            // store any data in the property - used by application to provide details about the connection
                            Data = descriptor.Data;

                            // is the protocol compatible
                            if (descriptor.ProtocolVersion.Major == Frame.ProtocolVersion.Major)
                            {
                                if (descriptor.MaxFrameSize < Frame.MaxFrameSize)
                                {
                                    // protocol will use the smallest of the two max frame sizes
                                    Frame.MaxFrameSize = descriptor.MaxFrameSize;
                                }

                                // update connection state
                                State = ConnectionState.ConnectionAcceptSent;

                                // send accept frame (version and (new) frame size will be returned)
                                _sendFrame(FrameType.ConnectionAccept, Frame.CreateConnectionAcceptFrame());
                            }
                            else
                            {
                                // set state
                                State = ConnectionState.End;

                                // reject the connection (description of why provided)
                                _sendFrame(FrameType.ConnectionReject, Frame.CreateConnectionRejectFrame("Protocol version incompatible"));

                                // should close here
                                _closeInternal();
                            }
                        }

                    } break;
                case FrameType.ConnectionAccept:
                    {
                        if(State == ConnectionState.ConnectionStartSent)
                        {
                            Log.DebugFormat("[{0}] FrameType.ConnectionAccept received", ID);

                            // now in open state
                            State = ConnectionState.Open;

                            // send the open frame to the endpoint
                            _sendFrame(FrameType.ConnectionOpen, Frame.CreateConnectionOpenFrame());

                            // signal that the connection is open
                            _connectionOpen.Set();
                        }

                    } break;
                case FrameType.ConnectionReject:
                    {
                        if (State == ConnectionState.ConnectionStartSent)
                        {
                            Log.DebugFormat("[{0}] FrameType.ConnectionReject received", ID);

                            // transition to end state
                            State = ConnectionState.End;

                            // close everything
                            _closeInternal();
                        }

                    } break;
                case FrameType.ConnectionOpen:
                    {
                        // used for both client and server endpoints
                        if (State == ConnectionState.ConnectionAcceptSent || State == ConnectionState.ConnectionOpenSent)
                        {
                            Log.DebugFormat("[{0}] FrameType.ConnectionOpen received", ID);

                            // connection is now open - set state
                            State = ConnectionState.Open;

                            // signal that connection is open
                            _connectionOpen.Set();
                        }

                    } break;
                case FrameType.ConnectionClose:
                    {
                        // valid in any state (except errored - but shoulnd't have received it in the first place)

                        Log.DebugFormat("[{0}] FrameType.ConnectionClose received", ID);

                        // close the connection
                        State = ConnectionState.End;

                        // close the physcial connection and any associated components
                        _closeInternal();

                        // raise the closed event
                        OnClosed?.Invoke(this, null);

                    } break;

                #endregion

                #region Session Frame Handling

                case FrameType.SessionStart:
                    {
                        var descriptor = BsonCodec.Decode<SessionDescriptor>(frame.Payload);

                        // TODO: getting a null descriptor here!
                        Log.DebugFormat("[{0}] FrameType.SessionStart received: ID [{1}], Name [{2}]", ID, frame.SessionID, descriptor.Name);

                        // validate
                        if (_pendingSessions.ContainsKey(frame.SessionID))
                        {
                            Log.DebugFormat("[{0}] Received SessionStart from when one is already pending", ID);
                            return;
                        }

                        if (_activeSessions.ContainsKey(frame.SessionID))
                        {
                            Log.DebugFormat("[{0}] Received SessionStart from when one is already active", ID);
                            return;
                        }

                        // create the incoming session and add to the pending dictionary
                        var session = new Session(this, descriptor, frame.SessionID);
                        _pendingSessions.Add(frame.SessionID, session);

                        // notify application
                        _ = Task.Run(() => OnSession?.Invoke(this, session));

                    } break;
                case FrameType.SessionAccept:
                    {
                        Log.DebugFormat("[{0}] FrameType.SessionAccept received: ID [{1}]", ID, frame.SessionID);

                        // move to active session but do not trigger Settle until Open received
                        Session session;
                        if (_pendingSessions.Remove(frame.SessionID, out session))
                        {
                            // move session from pending to active
                            _activeSessions.Add(frame.SessionID, session);

                            Log.DebugFormat("[{0}] Session moved from pending to active: ID [{1}], Pending [{2}], Active [{3}]", ID, frame.SessionID, _pendingSessions.Count, _activeSessions.Count);

                            // send session open
                            _sendFrame(FrameType.SessionOpen, Frame.CreateSessionOpenFrame(frame.SessionID, null));
                        }

                    } break;
                case FrameType.SessionReject:
                    {
                        Log.DebugFormat("[{0}] FrameType.SessionReject received: ID [{1}]", ID, frame.SessionID);

                        _pendingSessions.Remove(frame.SessionID);

                        Log.DebugFormat("[{0}] Session removed from pending sessions: ID [{1}], Pending [{2}], Active [{3}]", ID, frame.SessionID, _pendingSessions.Count, _activeSessions.Count);

                    } break;
                case FrameType.SessionOpen:
                    {
                        Log.DebugFormat("[{0}] FrameType.SessionOpen received: ID [{1}]", ID, frame.SessionID);

                        Session session;
                        if (_pendingSessions.Remove(frame.SessionID, out session))
                        {
                            // move session from pending to active
                            _activeSessions.Add(frame.SessionID, session);

                            Log.DebugFormat("[{0}] Session moved from pending to active: ID [{1}], Pending [{2}], Active [{3}]", ID, frame.SessionID, _pendingSessions.Count, _activeSessions.Count);

                            // respond with session open
                            _sendFrame(FrameType.SessionOpen, Frame.CreateSessionOpenFrame(frame.SessionID, null));

                            // signal that the session is now Open
                            session.NotifySessionOpen();
                        }
                        else if(_activeSessions.ContainsKey(frame.SessionID))
                        {
                            // signal that the session is now Open
                            _activeSessions[frame.SessionID].NotifySessionOpen();
                        }
                    }
                    break;
                case FrameType.SessionClose:
                    {
                        Log.DebugFormat("[{0}] FrameType.SessionClose received: ID [{1}]", ID, frame.SessionID);

                        Session session;
                        if(_activeSessions.Remove(frame.SessionID, out session))
                        {
                            // remove session from active collection
                            _activeSessions.Remove(frame.SessionID);

                            // call close on the session
                            session.Close();
                        }

                    } break;

                #endregion

                #region Message Frame Handling

                case FrameType.MessageSend:
                case FrameType.MessageAccept:
                case FrameType.MessageReject:
                    {
                        Log.DebugFormat("[{0}] {1} received", ID, frame.Type.ToString());

                        var sessionId = frame.SessionID;
                        if (_activeSessions.ContainsKey(sessionId))
                        {
                            _activeSessions[sessionId].NotifyMessage(frame.Type, frame.MessageID, frame.Payload);
                        }

                    } break;

                #endregion

                default:
                    break;
            }
        }

        /// <summary>
        /// Asynchronous task for decoding frames off the networking stream. The method calls _receiveFrame for each frame received
        /// which determines what to do with it based on the current Connection state, frame type, and frame contents
        /// </summary>
        /// <param name="stream">The network stream from which frames will be decoded</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The asynchronous task</returns>
        private Task _decodeStreamAsync(INetworkStream stream, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                var frameSizeUsed = Frame.MaxFrameSize;
                byte[] buffer = new byte[Frame.MaxFrameSize * 3];
                int bufferOffset = 0;
                int availableBytes = 0;
                
                while(!ct.IsCancellationRequested && State != ConnectionState.End && State != ConnectionState.ConnectionError)
                {
                    int read = 0;
                    while((read = stream.Read(buffer, bufferOffset + availableBytes, buffer.Length - (bufferOffset + availableBytes))) > 0)
                    {
                        availableBytes += read;

                        var frames = Frame.DecodeFrameBuffer(buffer, bufferOffset, availableBytes, out bufferOffset, out availableBytes);
                        foreach(var frame in frames)
                        {
                            // pass for processing
                            _processFrame(frame);
                        }

                        // shift any remaining data to the beginning of the buffer
                        if(bufferOffset > 0 && availableBytes > 0)
                        {
                            Log.DebugFormat("[{0}] Moving remaining bytes to beginning of buffer: Offset [{1}], Available [{2}]", ID, bufferOffset, availableBytes);

                            Array.Copy(buffer, bufferOffset, buffer, 0, availableBytes);
                            bufferOffset = 0;
                        }

                        if(frameSizeUsed != Frame.MaxFrameSize)
                        {
                            Log.DebugFormat("[{0}] Resizing frame buffer: Current [{1}], New [{2}]", ID, frameSizeUsed, Frame.MaxFrameSize);

                            // the protocol negotiation has adjusted the frame size - resize buffer
                            var resizedBuffer = new byte[Frame.MaxFrameSize * 3];
                            Array.Copy(buffer, bufferOffset, resizedBuffer, 0, availableBytes);
                            buffer = resizedBuffer;
                            bufferOffset = 0;
                            frameSizeUsed = Frame.MaxFrameSize;
                        }
                    }
                }
            }).ContinueWith(t =>
            {
                if(t.Exception != null)
                {
                    // change state to error
                    State = ConnectionState.ConnectionError;

                    // close dependent components and resources
                    _closeInternal();

                    // raise error event
                    OnError?.Invoke(this, null);
                }

                Log.DebugFormat("Decode stream task has ended");
            });
        }

        /// <summary>
        /// Notifies sessions of closing and then closes the network stream and client (does not raise events)
        /// </summary>
        private void _closeInternal()
        {
            // close and clear any pending sessions
            foreach (var s in _pendingSessions)
            {
                s.Value.NotifyConnectionClose();
            }
            _pendingSessions.Clear();

            // close and clear any active sessions
            foreach (var s in _activeSessions)
            {
                s.Value.NotifyConnectionClose();
            }
            _activeSessions.Clear();

            // TODO: use in network stream read
            _cts.Cancel();

            try
            {
                _streamDecoderTask.Dispose();
            }
            catch { }

            // close transport resources
            try
            {
                _networkStream.Close();
            }
            catch { }

            try
            {
                _tcpClient.Close();
            }
            catch { }
        }

        #endregion  
    }
}
