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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Teramine.Interconnect.Models;

namespace Teramine.Interconnect.Interfaces
{
    public interface IComponentFactory
    {
        /// <summary>
        /// Builds a ITcpListener using the provided parameters
        /// </summary>
        /// <param name="address">The address on which to listen for TCP connections</param>
        /// <param name="port">The port on which to listen for TCP connections</param>
        /// <param name="sslSettings">The SSL settings for connections. Set it to null or SslSettings.NoSsl for an unencrypted connection</param>
        /// <returns>Returns the created ITcpListener</returns>
        ITcpListener BuildTcpListener(IPAddress address, int port, SslSettings sslSettings);

        /// <summary>
        /// Builds a ITcpClient using the provided connection paramters
        /// </summary>
        /// <param name="address">The address of the host to connect to</param>
        /// <param name="port">The port on which to connect</param>
        /// <param name="sslSettings">The SSL settings for connections. Set it to null or SslSettings.NoSsl for an unencrypted connection</param>
        /// <returns>Returns the created ITcpClient</returns>
        ITcpClient BuildTcpClient(string address, int port, SslSettings sslSettings);
    }
}
