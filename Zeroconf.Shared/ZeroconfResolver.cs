using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Heijden.DNS;
using DnsType = Heijden.DNS.Type;


namespace Zeroconf
{
    /// <summary>
    ///     Looks for ZeroConf devices
    /// </summary>
    public static class ZeroconfResolver
    {
        private static readonly AsyncLock ResolverLock = new AsyncLock();

        private static readonly INetworkInterface NetworkInterface = LoadPlatformNetworkInterface();

        private static INetworkInterface LoadPlatformNetworkInterface()
        {
#if PCL
            throw new NotSupportedException("This PCL assembly must not be used at runtime. Make sure to add the Zeroconf Nuget reference to your main project.");
#else
            return new NetworkInterface();
#endif
        }

        /// <summary>
        ///     Resolves available ZeroConf services
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="protocol"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <returns></returns>
        public static Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(string protocol,
                                                                      TimeSpan scanTime = default (TimeSpan),
                                                                      int retries = 2,
                                                                      int retryDelayMilliseconds = 2000,
                                                                      Action<IZeroconfHost> callback = null,
                                                                      CancellationToken cancellationToken = default (CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException("protocol");

            return ResolveAsync(new[] {protocol},
                                scanTime,
                                retries,
                                retryDelayMilliseconds, callback, cancellationToken);
        }

        /// <summary>
        ///     Resolves available ZeroConf services
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="protocols"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(IEnumerable<string> protocols,
                                                                            TimeSpan scanTime = default (TimeSpan),
                                                                            int retries = 2,
                                                                            int retryDelayMilliseconds = 2000,
                                                                            Action<IZeroconfHost> callback = null,
                                                                            CancellationToken cancellationToken = default (CancellationToken))
        {
            Action<string, Response> wrappedAction = null;
            if (callback != null)
            {
                wrappedAction = (address, resp) =>
                                {
                                    var zc = ResponseToZeroconf(resp, address);
                                    callback(zc);
                                };
            }

            var protos = protocols.ToList(); // prevent multiple enumeration
            var buffer = GetRequestBytes(protos);
            var dict = await ResolveInternal(protos,
                                             buffer,
                                             scanTime,
                                             retries,
                                             retryDelayMilliseconds,
                                             wrappedAction,
                                             cancellationToken)
                                 .ConfigureAwait(false);

            return dict.Select(pair => ResponseToZeroconf(pair.Value, pair.Key)).ToList();
        }

        /// <summary>
        ///     Returns all available domains with services on them
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <returns></returns>
        public static async Task<ILookup<string, string>> BrowseDomainsAsync(TimeSpan scanTime = default (TimeSpan),
                                                                             int retries = 2,
                                                                             int retryDelayMilliseconds = 2000,
                                                                             Action<string, string> callback = null,
                                                                             CancellationToken cancellationToken = default (CancellationToken))
        {
            const string protocol = "_services._dns-sd._udp.local.";

            Action<string, Response> wrappedAction = null;
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

            var protocols = new[] {protocol};
            var buffer = GetRequestBytes(protocols);
            var dict = await ResolveInternal(protocols,
                                             buffer,
                                             scanTime,
                                             retries,
                                             retryDelayMilliseconds,
                                             wrappedAction,
                                             cancellationToken)
                                 .ConfigureAwait(false);

            var r = from kvp in dict
                    from service in BrowseResponseParser(kvp.Value)
                    select new {Service = service, Address = kvp.Key};

            return r.ToLookup(k => k.Service, k => k.Address);
        }

        private static IEnumerable<string> BrowseResponseParser(Response response)
        {
            return response.RecordsRR.Select(record => record.RECORD)
                           .OfType<RecordPTR>()
                           .Select(ptr => ptr.PTRDNAME);
        }


        private static async Task<IDictionary<string, Response>> ResolveInternal(IEnumerable<string> protocols,
                                                                                 byte[] requestBytes,
                                                                                 TimeSpan scanTime,
                                                                                 int retries,
                                                                                 int retryDelayMilliseconds,
                                                                                 Action<string, Response> callback,
                                                                                 CancellationToken cancellationToken)
        {
            using (await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (scanTime == default(TimeSpan))
                    scanTime = TimeSpan.FromSeconds(2);

                var dict = new Dictionary<string, Response>();

                Action<string, byte[]> converter = (address, buffer) =>
                                                   {
                                                       var resp = new Response(buffer);
                                                       Debug.WriteLine("IP: {0}, Bytes: {1}, IsResponse: {2}",
                                                                       address,
                                                                       buffer.Length,
                                                                       resp.header.QR);

                                                       if (resp.header.QR)
                                                       {
                                                           lock (dict)
                                                           {
                                                               dict[address] = resp;
                                                           }

                                                           if (callback != null)
                                                               callback(address, resp);
                                                       }
                                                   };

                Debug.WriteLine("Looking for {0} with scantime {1}", string.Join(", ", protocols), scanTime);

                await NetworkInterface.NetworkRequestAsync(requestBytes,
                                                           scanTime,
                                                           retries,
                                                           retryDelayMilliseconds,
                                                           converter,
                                                           cancellationToken)
                                      .ConfigureAwait(false);

                return dict;
            }
        }

        private static byte[] GetRequestBytes(IEnumerable<string> protocols)
        {
            var req = new Request();

            foreach (var protocol in protocols)
            {
                var question = new Question(protocol, QType.ANY, QClass.ANY);

                req.AddQuestion(question);
            }

            return req.Data;
        }

        private static ZeroconfHost ResponseToZeroconf(Response response, string remoteAddress)
        {
            var z = new ZeroconfHost();

            // Get the Id and IP address from the A record
            var aRecord = response.Additionals
                                  .Select(r => r.RECORD)
                                  .OfType<RecordA>()
                                  .FirstOrDefault();

            if (aRecord != null)
            {
                z.Id = aRecord.RR.NAME.Split('.')[0];
                z.IPAddress = aRecord.Address;
            }
            else
            {
                // Is this valid?
                z.Id = remoteAddress;
                z.IPAddress = remoteAddress;
            }

            var dispNameSet = false;

            var services = response.Answers
                                   .Select(r => r.RECORD)
                                   .OfType<RecordPTR>();

            foreach (var ptrRec in services)
            {
                // set the display name if needed
                if (!dispNameSet)
                {
                    z.DisplayName = ptrRec.PTRDNAME.Split('.')[0];
                    dispNameSet = true;
                }

                // Get the matching service records
                var serviceRecords = response.Additionals
                                             .Where(r => r.NAME == ptrRec.PTRDNAME)
                                             .Select(r => r.RECORD)
                                             .ToList();

                var srvRec = serviceRecords.OfType<RecordSRV>().FirstOrDefault();
                if (srvRec == null)
                    continue; // Missing the SRV record, not valid

                var svc = new Service
                {
                    Name = ptrRec.RR.NAME,
                    Port = srvRec.PORT
                };

                // There may be 0 or more text records - property sets
                foreach (var txtRec in serviceRecords.OfType<RecordTXT>())
                {
                    var set = new Dictionary<string, string>();
                    foreach (var txt in txtRec.TXT)
                    {
                        var split = txt.Split(new[] {'='}, 2);
                        if (split.Length == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(split[0]))
                                set[split[0]] = null;
                        }
                        else
                        {
                            set[split[0]] = split[1];
                        }
                    }
                    svc.AddPropertySet(set);
                }

                z.AddService(svc);
            }

            return z;
        }
    }
}