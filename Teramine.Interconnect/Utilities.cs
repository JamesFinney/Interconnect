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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Teramine.Interconnect.Exceptions;

namespace Teramine.Interconnect
{
    public static class Utilities
    {
        public static void Noop()
        {
            ((Action)(() => { }))();
        }

        internal static async Task<IPHostEntry> GetHostEntryAsync(string host)
        {
            return await Dns.GetHostEntryAsync(host).ConfigureAwait(false);
        }

        internal static async Task<IPAddress[]> GetHostAddressesAsync(string host)
        {
            return await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
        }

        internal static List<IPAddress> ParseHost(string host)
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();

            try
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(host, out ipAddress))
                {
                    ipAddresses.Add(ipAddress);
                }
                else if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(Environment.GetEnvironmentVariable("COMPUTERNAME"), StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(Utilities.GetHostEntryAsync(string.Empty).Result.HostName, StringComparison.OrdinalIgnoreCase))
                {
                    if (Socket.OSSupportsIPv4)
                    {
                        ipAddresses.Add(IPAddress.Any);
                    }

                    if (Socket.OSSupportsIPv6)
                    {
                        ipAddresses.Add(IPAddress.IPv6Any);
                    }
                }
                else
                {
                    ipAddresses.AddRange(Utilities.GetHostAddressesAsync(host).GetAwaiter().GetResult());
                }

                return ipAddresses;
            }
            catch (SocketException se) when (se.Message.StartsWith("No such host is known"))
            {
                throw new ConnectionException("No such host is known");
            }
            catch (Exception e)
            {
                throw new ConnectionException(e.Message);
            }
        }
    }
}
