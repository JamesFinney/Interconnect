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
using System.Threading.Tasks;

namespace Teramine.Interconnect.Interfaces
{
    public interface ITcpClient : IDisposable
    {
        int Available { get; }
        Socket Client { get; set; }
        bool Connected { get; }
        bool ExclusiveAddressUse { get; set; }
        LingerOption LingerState { get; set; }
        bool NoDelay { get; set; }
        int ReceiveBufferSize { get; set; }
        int ReceiveTimeout { get; set; }
        int SendBufferSize { get; set; }
        int SendTimeout { get; set; }

        IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state);
        IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state);
        IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state);
        void Close();
        void Connect(IPAddress address, int port);
        void Connect(IPAddress[] ipAddresses, int port);
        void Connect(IPEndPoint remoteEP);
        void Connect(string hostname, int port);
        Task ConnectAsync(IPAddress address, int port);
        Task ConnectAsync(IPAddress[] addresses, int port);
        Task ConnectAsync(string host, int port);
        void EndConnect(IAsyncResult asyncResult);
        INetworkStream GetStream();
        void AuthenticateAsServer();
        void AuthenticateAsClient();
    }
}