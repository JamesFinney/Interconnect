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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Teramine.Interconnect.Models
{
    public class Message
    {
        /// <summary>
        /// The unique message ID for the session
        /// </summary>
        public uint ID { get; }

        /// <summary>
        /// The binary message dat
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// The Session to which the message belongs
        /// </summary>
        public Session Session { get; }


        internal ManualResetEvent Settled = new ManualResetEvent(false);
        internal bool Accepted { get; set; }
        internal byte[] ResponseData { get; set; }


        internal Message(Session session, uint id, byte[] data)
        {
            Session = session;
            ID = id;
            Data = data;
        }

        /// <summary>
        /// Responds with the positive acceptance of the message with optional response data
        /// </summary>
        /// <param name="data">The data to be returned to the sender</param>
        public void Accept(byte[] data = null)
        {
            // notify the session that the message has been accepted
            Session.AcceptMessage(ID, data);
        }

        /// <summary>
        /// Responds with the negative acceptance of the message with optional response data
        /// </summary>
        /// <param name="data">The data to be returned to the sender</param>
        public void Reject(byte[] data = null)
        {
            // notify the session that the message has been rejected
            Session.RejectMessage(ID, data);
        }
    }
}
