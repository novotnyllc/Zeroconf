using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Heijden.DNS;
using DnsType = Heijden.DNS.Type;


namespace Zeroconf
{
    /// <summary>
    /// Looks for ZeroConf devices
    /// </summary>
    public static class ZeroconfResolver
    {
        private static readonly AsyncLock ResolverLock = new AsyncLock();
        /// <summary>
        /// Resolves available ZeroConf services
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="protocol"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IZeroconfRecord>> ResolveAsync(string protocol, TimeSpan scanTime = default (TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, Action<IZeroconfRecord> callback = null, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException("protocol");

            Action<IPAddress, Response> wrappedAction = null;
            if (callback != null)
            {
                wrappedAction = (address, resp) =>
                {
                    var zc = ResponseToZeroconf(resp, address);
                    callback(zc);
                };
            }

            var dict = await ResolveInternal(protocol, scanTime, retries, retryDelayMilliseconds, wrappedAction, cancellationToken).ConfigureAwait(false);

            return dict.Select(pair => ResponseToZeroconf(pair.Value, pair.Key)).ToList();
        }

        public static async Task<ILookup<string, IPAddress>> BrowseDomainsAsync(TimeSpan scanTime = default (TimeSpan), int retries = 2,
            int retryDelayMilliseconds = 2000, Action<string, IPAddress> callback = null, CancellationToken cancellationToken = default (CancellationToken))
        {
            const string protocol = "_services._dns-sd._udp.local.";

            Action<IPAddress, Response> wrappedAction = null;
            if (callback != null)
            {
                wrappedAction = (address, response) =>
                {
                    foreach (var service in BrowseResponseParser(response))
                    {
                        callback(service, address);
                    }
                };
            }

            var dict = await ResolveInternal(protocol, scanTime, retries, retryDelayMilliseconds, wrappedAction, cancellationToken).ConfigureAwait(false);

            var r = from kvp in dict
                    from service in BrowseResponseParser(kvp.Value)
                    select new { Service = service, Address = kvp.Key };

            return r.ToLookup(k => k.Service, k => k.Address);
        }

        private static IEnumerable<string> BrowseResponseParser(Response response)
        {
            return response.RecordsRR.Select(record => record.RECORD)
                                     .OfType<RecordPTR>()
                                     .Select(ptr => ptr.PTRDNAME);
        }

        private static async Task<IDictionary<IPAddress, Response>> ResolveInternal(string protocol, TimeSpan scanTime = default (TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, Action<IPAddress, Response> callback = null, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (scanTime == default(TimeSpan))
                scanTime = TimeSpan.FromSeconds(2);

            using (await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine("Looking for {0} with scantime {1}", protocol, scanTime);

                using (var client = new UdpClient())
                {
                    for (var i = 0; i < retries; i++)
                    {
                        try
                        {
                            var localEp = new IPEndPoint(IPAddress.Any, 5353);

                            // There could be multiple adapters, get the default one
                            uint index = 0;
                            GetBestInterface(0, out index);
                            var ifaceIndex = (int)index;

                            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(ifaceIndex));

                            client.ExclusiveAddressUse = false;
                            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, scanTime.Milliseconds);
                            client.ExclusiveAddressUse = false;

                            client.Client.Bind(localEp);

                            var multicastAddress = IPAddress.Parse("224.0.0.251");
                            var multOpt = new MulticastOption(multicastAddress, ifaceIndex);
                            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);

                            Debug.WriteLine("Bound to multicast address");

                            var list = new Dictionary<IPAddress, Response>();

                            // Start a receive loop
                            var shouldCancel = false;
                            var recTask = Task.Run(async () =>
                            {
                                try
                                {
                                    while (!shouldCancel)
                                    {
                                        var res = await client.ReceiveAsync().ConfigureAwait(false);
                                        var byteCount = res.Buffer.Length;
                                        var resp = new Response(res.Buffer);
                                        Debug.WriteLine("IP: {0}, Bytes: {1}, IsResponse: {2}", res.RemoteEndPoint.Address, byteCount, resp.header.QR);

                                        if (resp.header.QR)
                                        {
                                            var zr = ResponseToZeroconf(resp, res.RemoteEndPoint.Address);
                                            lock (list)
                                            {
                                                list[res.RemoteEndPoint.Address] = resp;
                                            }

                                            if (callback != null)
                                                callback(res.RemoteEndPoint.Address, resp);
                                        }
                                    }
                                }
                                catch (ObjectDisposedException)
                                {
                                }

                            }, cancellationToken);

                            var broadcastEp = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
                            var buffer = GetRequestBytes(protocol);
                            await client.SendAsync(buffer, buffer.Length, broadcastEp).ConfigureAwait(false);
                            Debug.WriteLine("Sent mDNS query");


                            // wait for responses
                            await Task.Delay(scanTime, cancellationToken).ConfigureAwait(false);
                            shouldCancel = true;
                            client.Close();
                            Debug.WriteLine("Done Scanning");


                            await recTask.ConfigureAwait(false);

                            return list;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Execption: ", e);
                            if (i + 1 >= retries) // last one, pass underlying out
                                throw;
                        }

                        await Task.Delay(retryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                    }

                    return new Dictionary<IPAddress, Response>();
                }
            }
        }
            
            
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto)]
        private static extern int GetBestInterface(UInt32 DestAddr, out UInt32 BestIfIndex);

        private static byte[] GetRequestBytes(string protocol)
        {
            var req = new Request();

            var question = new Question(protocol, QType.ANY, QClass.ANY);

            req.AddQuestion(question);

            return req.Data;
        }

        private static ZeroconfRecord ResponseToZeroconf(Response response, IPAddress remoteAddress)
        {
            // records by type
            var records = response.RecordsRR.ToLookup(record => record.Type);


            var z = new ZeroconfRecord();

            if (records.Contains(DnsType.PTR))
            {
                var ptr = (RecordPTR)records[DnsType.PTR].First().RECORD;
                z.Name = ptr.PTRDNAME.Split('.')[0];
            }

            if (records.Contains(DnsType.A))
            {
                var rr = records[DnsType.A].First();
                z.Host = rr.NAME.Split('.')[0];
                z.IPAddress = ((RecordA)rr.RECORD).Address;
            }
            else
            {
                z.IPAddress = remoteAddress;
            }

            if (records.Contains(DnsType.SRV))
            {
                var srv = (RecordSRV)records[DnsType.SRV].First().RECORD;
                z.Port = srv.PORT;
            }

            if (records.Contains(DnsType.TXT))
            {
                foreach (var rr in records[DnsType.TXT])
                {
                    var txtRecord = (RecordTXT)rr.RECORD;
                    foreach (var txt in txtRecord.TXT)
                    {
                        var split = txt.Split(new[] { '=' }, 2);
                        if (split.Length == 1)
                        {
                            z.AddProperty(split[0], null);
                        }
                        else
                        {
                            z.AddProperty(split[0], split[1]);
                        }
                    }
                }
            }

            return z;
        }
    }

}
