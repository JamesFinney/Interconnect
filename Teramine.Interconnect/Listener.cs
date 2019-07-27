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

using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Teramine.DSC.Interconnect.Factories;
using Teramine.DSC.Interconnect.Interfaces;
using Teramine.DSC.Interconnect.Exceptions;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Teramine.DSC.Interconnect.Models;

namespace Teramine.DSC.Interconnect
{
    public class Listener : IListener
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<IConnection> OnConnection;
        public static IComponentFactory ComponentFactory { get; set; } = new ComponentFactory();

        private List<IPAddress> _addresses;
        private List<ITcpListener> _listeners;
        private int _port;
        private CancellationTokenSource _cts;
        private CancellationToken _cancelToken;
        private SslSettings _sslSettings;


        public Listener(string host, int port, SslSettings sslSettings)
        {
            _ = host ?? throw new ArgumentNullException();
            _port = port > 0 && port <= 65535 ? port : throw new ArgumentOutOfRangeException();
            _sslSettings = sslSettings;

            _cts = new CancellationTokenSource();
            _listeners = new List<ITcpListener>();
            _addresses = Utilities.ParseHost(host);
        }

        public Listener Start()
        {
            _cancelToken = _cts.Token;

            try
            {
                for (int i = 0; i < _addresses.Count; i++)
                {
                    var listener = ComponentFactory.BuildTcpListener(_addresses[i], _port, _sslSettings);

                    _listeners.Add(listener);

                    listener.Start();
                    listener.BeginAcceptTcpClient(_incomingTcpClient, listener);

                    Log.InfoFormat("Interconnect listening on {0}:{1} ...", _addresses[i], _port);
                }

                return this;
            }
            catch(Exception e)
            {
                Log.Error("Exception caught starting interconnect listener", e);
                throw new ConnectionException("Exception caught starting interconnect listener", e);
            }
        }

        public void Stop()
        {
            _cts.Cancel();

            for(int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].Stop();
            }

            _listeners.Clear();
        }

        public void _incomingTcpClient(IAsyncResult result)
        {
            var listener = result.AsyncState as ITcpListener;
            if (listener == null)
            {
                return;
            }

            if(_cts.IsCancellationRequested)
            {
                return;

            }
            // starts waiting for the next request.
            listener.BeginAcceptTcpClient(_incomingTcpClient, listener);

            // gets client and starts processing received request.
            var client = listener.EndAcceptTcpClient(result);
            if(client == null)
            {
                return;
            }

            bool accept = true;

            // double check not cancelled
            if (_cancelToken.IsCancellationRequested)
            {
                Log.Warn("Incoming connection but cancellation requested");
                accept = false;
            }

            // application not listening to conneciton events - what's the point - close
            if(OnConnection == null)
            {
                Log.Warn("Received incoming connection but the application is not listening for connection events. Connection will be closed!");
                accept = false;
            }

            // should we accept it?
            if(!accept)
            {
                Log.Warn("Closing incoming connection");
                return;
            }

            Log.DebugFormat("Connection received from {0}:{1} ...", ((IPEndPoint)client.Client.RemoteEndPoint).Address, ((IPEndPoint)client.Client.RemoteEndPoint).Port);

            Connection connection = null;
            try
            {
                // this will open the connection (or throw if that fails)
                // cert and validate not actually required, but passing in so available from connection - keeps it consistent
                connection = new Connection(client, _sslSettings);

                // pass new connection to application
                _ = Task.Run(() => OnConnection?.Invoke(this, connection));
            }
            catch(Exception e)
            {
                Log.Error("Exception caught processing incoming connection", e);

                try
                {
                    // dispose of any resources that were setup
                    connection?.Dispose();

                } catch { }
            }
        }
    }
}
