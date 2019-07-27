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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Teramine.Interconnect.Interfaces;
using Teramine.Interconnect.Models;
using Teramine.Interconnect.Network;
using Teramine.Interconnect.Newtork;


namespace Teramine.Interconnect.Factories
{
    public class ComponentFactory : IComponentFactory
    {
        public ITcpClient BuildTcpClient(string address, int port, SslSettings sslSettings)
        {
            var client = new TcpClient(address, port);
            return new InterconnectTcpClient(client, sslSettings);
        }

        public ITcpListener BuildTcpListener(IPAddress address, int port, SslSettings sslSettings)
        {
            var listener = new TcpListener(address, port);
            return new InterconnectTcpListener(listener, sslSettings);
        }
    }
}
