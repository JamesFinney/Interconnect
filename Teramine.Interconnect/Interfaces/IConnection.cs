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

namespace Teramine.Interconnect.Interfaces
{
    public interface IConnection
    {
        /// <summary>
        /// Raised when a new session has been initiated. The session must be Accepted or Reject to avoid a timeout
        /// </summary>
        event EventHandler<Session> OnSession;

        /// <summary>
        /// Raised when the Session's underlying connection has been closed
        /// </summary>
        event EventHandler OnClosed;

        /// <summary>
        /// Raised when an error has occured
        /// </summary>
        event EventHandler OnError;

        /// <summary>
        /// Unique connection identifier
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Application specific connection data which is set when the connection is created
        /// </summary>
        Dictionary<string, string> Data { get; }

        /// <summary>
        /// True if the connection is the client side of the connection; False if it is the server side
        /// </summary>
        bool IsClient { get; }

        /// <summary>
        /// Creates a new session with the specified name and initial session data
        /// </summary>
        /// <param name="name">The name of the session</param>
        /// <param name="data">Optional application specific data</param>
        /// <returns>Returns the created session. If the session is rejected or times out an exception will be thrown</returns>
        Session CreateSession(string name, byte[] data = null);

        /// <summary>
        /// Closes the session
        /// </summary>
        void Close();

        /// <summary>
        /// Closes and disposed of the session
        /// </summary>
        void Dispose();
    }
}
