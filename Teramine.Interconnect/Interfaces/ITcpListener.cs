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

namespace Teramine.DSC.Interconnect.Interfaces
{
    public interface ITcpListener
    {
        bool ExclusiveAddressUse { get; set; }
        EndPoint LocalEndpoint { get; }
        Socket Server { get; }

        Socket AcceptSocket();
        Task<Socket> AcceptSocketAsync();
        ITcpClient AcceptTcpClient();
        void AllowNatTraversal(bool allowed);
        IAsyncResult BeginAcceptSocket(AsyncCallback callback, object state);
        IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state);
        Socket EndAcceptSocket(IAsyncResult asyncResult);
        ITcpClient EndAcceptTcpClient(IAsyncResult asyncResult);
        bool Pending();
        void Start();
        void Start(int backlog);
        void Stop();
    }
}