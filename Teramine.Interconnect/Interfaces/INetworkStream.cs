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
using System.Threading.Tasks;

namespace Teramine.Interconnect.Interfaces
{
    public interface INetworkStream
    {
        /// <summary>
        /// The position within the network stream
        /// </summary>
        long Position { get; set; }

        /// <summary>
        /// The length of the network stream
        /// </summary>
        long Length { get; }

        /// <summary>
        /// True if the network stream can be written to; False if it cannot
        /// </summary>
        bool CanWrite { get; set; }

        /// <summary>
        /// Used for SSL connections to authenticate as the server side of the connection
        /// </summary>
        void AuthenticateAsServer();

        /// <summary>
        /// Used for SSL connections to authenticate as the client side of the connection
        /// </summary>
        void AuthenticateAsClient();

        /// <summary>
        /// Writes data to the netowrk stream
        /// </summary>
        /// <param name="buffer">A buffer containing the data to be written</param>
        /// <param name="offset">The offset to start from within the supplied the buffer</param>
        /// <param name="size">The number of bytes to write</param>
        void Write(byte[] buffer, int offset, int size);

        /// <summary>
        /// Asynchronously writes data to the network stream
        /// </summary>
        /// <param name="buffer">A buffer containing the data to be written</param>
        /// <param name="offset">The offset to start from within the supplied the buffer</param>
        /// <param name="size">The number of bytes to write</param>
        /// <returns>The write task</returns>
        Task WriteAsync(byte[] buffer, int offset, int size);

        /// <summary>
        /// Asynchronously writes data to the network stream with a cancellation token
        /// </summary>
        /// <param name="buffer">A buffer containing the data to be written</param>
        /// <param name="offset">The offset to start from within the supplied the buffer</param>
        /// <param name="size">The number of bytes to write</param>
        /// <param name="token">the csncellation token</param>
        /// <returns>The write task</returns>
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token);

        /// <summary>
        /// Reads data from the network stream from its current position
        /// </summary>
        /// <param name="buffer">the buffer in which to read the data</param>
        /// <param name="offset">the offset at which to start in the buffer</param>
        /// <param name="size">the number of bytes to read</param>
        /// <returns>the number of bytes actually read</returns>
        int Read(byte[] buffer, int offset, int size);

        /// <summary>
        /// Reads data from the network stream from its current position with a timeout
        /// </summary>
        /// <param name="buffer">the buffer in which to read the data</param>
        /// <param name="offset">the offset at which to start in the buffer</param>
        /// <param name="size">the number of bytes to read</param>
        /// <param name="timeoutMs">the timeout in milliseconds</param>
        /// <returns>the number of bytes actually read</returns>
        int Read(byte[] buffer, int offset, int size, int timeoutMs);

        /// <summary>
        /// Closes the network stream
        /// </summary>
        void Close();

        /// <summary>
        /// Closes and disposed of the network stream
        /// </summary>
        void Dispose();
    }
}
