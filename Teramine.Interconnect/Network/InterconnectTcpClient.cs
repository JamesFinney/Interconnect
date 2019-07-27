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

namespace Teramine.Interconnect.Network
{
    public class InterconnectTcpClient : ITcpClient
    {
        private TcpClient _tcpClient;
        private InterconnectNetworkStream _stream;
        private SslSettings _sslSettings;


        public InterconnectTcpClient(TcpClient tcpClient, SslSettings sslSettings)
        {
            _tcpClient = tcpClient;
            _sslSettings = sslSettings;
        }

        public int ReceiveTimeout { get => _tcpClient.ReceiveTimeout; set { _tcpClient.ReceiveTimeout = value; } }
        public int ReceiveBufferSize { get => _tcpClient.ReceiveTimeout; set { _tcpClient.ReceiveTimeout = value; } }
        public bool NoDelay { get => _tcpClient.NoDelay; set { _tcpClient.NoDelay = value; } }
        public LingerOption LingerState { get => _tcpClient.LingerState; set { _tcpClient.LingerState = value; } }
        public bool ExclusiveAddressUse { get => _tcpClient.ExclusiveAddressUse; set { _tcpClient.ExclusiveAddressUse = value; } }
        public bool Connected { get => _tcpClient.Connected; }
        public Socket Client { get => _tcpClient.Client; set { _tcpClient.Client = value; } }
        public int Available { get => _tcpClient.Available; }
        public int SendBufferSize { get => _tcpClient.SendBufferSize; set { _tcpClient.SendBufferSize = value; } }
        public int SendTimeout { get => _tcpClient.SendTimeout; set { _tcpClient.SendTimeout = value; } }

        public void AuthenticateAsClient()
        {
            _stream.AuthenticateAsClient();
        }

        public void AuthenticateAsServer()
        {
            _stream.AuthenticateAsServer();
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
        {
            return _tcpClient.BeginConnect(host, port, requestCallback, state);
        }

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
        {
            return _tcpClient.BeginConnect(addresses, port, requestCallback, state);
        }

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
        {
            return BeginConnect(address, port, requestCallback, state);
        }

        public void Close()
        {
            _tcpClient.Close();
        }

        public void Connect(string hostname, int port)
        {
            _tcpClient.Connect(hostname, port);
        }

        public void Connect(IPEndPoint remoteEP)
        {
            _tcpClient.Connect(remoteEP);
        }

        public void Connect(IPAddress[] ipAddresses, int port)
        {
            _tcpClient.Connect(ipAddresses, port);
        }

        public void Connect(IPAddress address, int port)
        {
            _tcpClient.Connect(address, port);
        }

        public Task ConnectAsync(IPAddress[] addresses, int port)
        {
            return _tcpClient.ConnectAsync(addresses, port);
        }

        public Task ConnectAsync(IPAddress address, int port)
        {
            return _tcpClient.ConnectAsync(address, port);
        }

        public Task ConnectAsync(string host, int port)
        {
            return _tcpClient.ConnectAsync(host, port);
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            _tcpClient.EndConnect(asyncResult);
        }

        public INetworkStream GetStream()
        {
            if (_sslSettings == null)
            {
                _stream = new InterconnectNetworkStream(_tcpClient.GetStream());
            }
            else
            { 
                _stream = new InterconnectNetworkStream(_tcpClient.GetStream(), _sslSettings);
            }

            return _stream;
        }
    }
}
