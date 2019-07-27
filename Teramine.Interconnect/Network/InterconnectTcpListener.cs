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
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Teramine.Interconnect.Interfaces;
using Teramine.Interconnect.Models;
using Teramine.Interconnect.Network;

namespace Teramine.Interconnect.Newtork
{
    public class InterconnectTcpListener : ITcpListener
    {
        private TcpListener _listener;
        private SslSettings _sslSettings;


        public InterconnectTcpListener(TcpListener listener, SslSettings sslSettings)
        {
            _listener = listener;
            _sslSettings = sslSettings;
        }

        public bool ExclusiveAddressUse { get => _listener.ExclusiveAddressUse; set { _listener.ExclusiveAddressUse = value; } }
        public EndPoint LocalEndpoint { get => _listener.LocalEndpoint; }
        public Socket Server { get => _listener.Server; }

        public Socket AcceptSocket()
        {
            return _listener.AcceptSocket();
        }
        public Task<Socket> AcceptSocketAsync()
        {
            return _listener.AcceptSocketAsync();
        }
        public ITcpClient AcceptTcpClient()
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                return new InterconnectTcpClient(client, _sslSettings);
            }
            catch(SocketException) { }

            return null;       
        }
        public void AllowNatTraversal(bool allowed)
        {
            _listener.AllowNatTraversal(allowed);
        }
        public IAsyncResult BeginAcceptSocket(AsyncCallback callback, object state)
        {
            return _listener.BeginAcceptSocket(callback, state);
        }
        public IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state)
        {
            return _listener.BeginAcceptTcpClient(callback, state);
        }
        public Socket EndAcceptSocket(IAsyncResult asyncResult)
        {
            return _listener.EndAcceptSocket(asyncResult);
        }
        public ITcpClient EndAcceptTcpClient(IAsyncResult asyncResult)
        {
            var client = _listener.EndAcceptTcpClient(asyncResult);
            return new InterconnectTcpClient(client, _sslSettings);
        }
        public bool Pending()
        {
            return _listener.Pending();
        }
        public void Start()
        {
            _listener.Start();
        }
        public void Start(int backlog)
        {
            _listener.Start(backlog);
        }
        public void Stop()
        {
            _listener.Stop();
        }
    }
}
