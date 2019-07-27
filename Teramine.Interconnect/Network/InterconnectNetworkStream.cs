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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Teramine.Interconnect;
using Teramine.Interconnect.Interfaces;
using Teramine.Interconnect.Models;

namespace Teramine.Interconnect.Network
{
    public class InterconnectNetworkStream : INetworkStream
    {
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Stream _stream;
        private SslSettings _sslSettings;


        public InterconnectNetworkStream(NetworkStream stream)
        {
            _stream = stream;
        }

        public InterconnectNetworkStream(NetworkStream stream, SslSettings sslSettings)
        {
            _sslSettings = sslSettings;
            _stream = new SslStream(stream, false, _validateRemoteCertificate, _selectClientCertificate);
        }

        public int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public bool CanWrite
        {
            get => _stream.CanWrite;
            set => throw new InvalidOperationException("CanWrite is readonly on a NetworkStream object");
        }

        public bool CanTimeout => _stream.CanTimeout;

        public bool CanSeek => _stream.CanSeek;

        public bool CanRead => _stream.CanRead;

        public int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }



        public int Read(byte[] buffer, int offset, int size)
        {
            return _stream.Read(buffer, offset, size);
        }

        public int Read(byte[] buffer, int offset, int size, int timeoutMs)
        {
            if (timeoutMs <= 0)
            {
                return _stream.Read(buffer, offset, size);
            }
            else
            {
                var t = Task.Run(() => Read(buffer, offset, size));
                var success = t.Wait(timeoutMs);

                if (!success)
                {
                    try
                    {
                        t.Dispose();
                    }
                    catch { }

                    return -1;
                }
                else
                {
                    return t.Result;
                }
            }
        }

        public int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
        }

        public int ReadByte()
        {
            return _stream.ReadByte();
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            _stream.Write(buffer, offset, size);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int size)
        {
            await _stream.WriteAsync(buffer, offset, size);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token)
        {
            await _stream.WriteAsync(buffer, offset, size, token);
        }

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void Close()
        {
            _stream.Close();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void AuthenticateAsServer()
        {
            if(_stream is SslStream)
            {
                var sslStream = (_stream as SslStream);
                sslStream .AuthenticateAsServer(_sslSettings.Certificate, clientCertificateRequired: true, checkCertificateRevocation: true);

                // log out details (may take this out in the future)
                Log.DebugFormat("Secure connection successfully created");
                Log.DebugFormat("IsAuthenticated: {0}", sslStream.IsAuthenticated);
                Log.DebugFormat("IsSecured: {0}", sslStream.IsEncrypted);
                Log.DebugFormat("MutualAuthentication: {0}", sslStream.IsMutuallyAuthenticated);
                Log.DebugFormat("CipherAlgorithm: {0}", sslStream.CipherAlgorithm);
                Log.DebugFormat("HashAlgorithm: {0}", sslStream.HashAlgorithm);
                Log.DebugFormat("KeyExchangeAlgorithm: {0}", sslStream.KeyExchangeAlgorithm);
                Log.DebugFormat("NegotiatedCipherSuite: {0}", sslStream.NegotiatedCipherSuite);
                Log.DebugFormat("SslProtocol: {0}", sslStream.SslProtocol);
            }
        }

        public void AuthenticateAsClient()
        {
            if (_stream is SslStream)
            {
                var sslStream = (_stream as SslStream);

                // the value provided pops up in _selectClientCertificate => targetHost. Can be empty bu NOT null
                sslStream.AuthenticateAsClient(string.Empty);

                // log out details (may take this out in the future)
                Log.DebugFormat("Secure connection successfully created");
                Log.DebugFormat("IsAuthenticated: {0}", sslStream.IsAuthenticated);
                Log.DebugFormat("IsSecured: {0}", sslStream.IsEncrypted);
                Log.DebugFormat("MutualAuthentication: {0}", sslStream.IsMutuallyAuthenticated);
                Log.DebugFormat("CipherAlgorithm: {0}", sslStream.CipherAlgorithm);
                Log.DebugFormat("HashAlgorithm: {0}", sslStream.HashAlgorithm);
                Log.DebugFormat("KeyExchangeAlgorithm: {0}", sslStream.KeyExchangeAlgorithm);
                Log.DebugFormat("NegotiatedCipherSuite: {0}", sslStream.NegotiatedCipherSuite);
                Log.DebugFormat("SslProtocol: {0}", sslStream.SslProtocol);
            }
        }


        private bool _validateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
        {
            if(_sslSettings == null)
            {
                Log.Error("Remote certificate validation called but no SSL settings available. Refusing connection.");
                return false;
            }

            if (!_sslSettings.ValidateRemoteCertificate)
                return true;

            var cert2 = cert as X509Certificate2;

            // validate the issuer
            if(_sslSettings.AllowedIssuers.Count > 0)
            {
                // get the issuer attributes of the provided certificate
                var remoteIssuerAttributes = cert2.GetIssuerCertificateAttributes();

                // if none of the allowed issuers matched then fail validation
                if(!remoteIssuerAttributes.MatchesAny(_sslSettings.AllowedIssuers))
                {
                    Log.ErrorFormat("The remote certificate failed issuer validation. The connection will be denied.");
                    return false;
                }
            }
            
            // validate the cert
            if(_sslSettings.AllowedCertificates.Count > 0)
            {
                var certAttributes = cert2.GetCertificateAttributes();

                // if none of the allowed issuers matched then fail validation
                if (!certAttributes.MatchesAny(_sslSettings.AllowedCertificates))
                {
                    Log.ErrorFormat("The remote certificate failed certificate validation. The connection will be denied.");
                    return false;
                }
            }

            return true;
        }

        private X509Certificate _selectClientCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _sslSettings.Certificate;
        }
    }
}
