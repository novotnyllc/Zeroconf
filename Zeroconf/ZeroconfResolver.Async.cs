using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Heijden.DNS;

namespace Zeroconf
{
    static partial class ZeroconfResolver
    {

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
                                                                      TimeSpan scanTime = default(TimeSpan),
                                                                      int retries = 2,
                                                                      int retryDelayMilliseconds = 2000,
                                                                      Action<IZeroconfHost> callback = null,
                                                                      CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException(nameof(protocol));

            return ResolveAsync(new[] { protocol },
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
                                                                            TimeSpan scanTime = default(TimeSpan),
                                                                            int retries = 2,
                                                                            int retryDelayMilliseconds = 2000,
                                                                            Action<IZeroconfHost> callback = null,
                                                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            Action<string, Response> wrappedAction = null;

            var protos = new HashSet<string>(protocols, StringComparer.OrdinalIgnoreCase);
            if (callback != null)
            {
                wrappedAction = (address, resp) =>
                {
                    var zc = ResponseToZeroconf(resp, address);
                    if (zc.Services.Any(s => protos.Contains(s.Key)))
                    {
                        callback(zc);
                    }
                };
            }
            
            var buffer = GetRequestBytes(protos);
            var dict = await ResolveInternal(protos,
                                             buffer,
                                             scanTime,
                                             retries,
                                             retryDelayMilliseconds,
                                             wrappedAction,
                                             cancellationToken)
                                 .ConfigureAwait(false);

            return dict.Select(pair => ResponseToZeroconf(pair.Value, pair.Key))
                       .Where(zh => zh.Services.Any(s => protos.Contains(s.Key))) // Ensure we only return records that have matching services
                       .ToList();
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
        public static async Task<ILookup<string, string>> BrowseDomainsAsync(TimeSpan scanTime = default(TimeSpan),
                                                                             int retries = 2,
                                                                             int retryDelayMilliseconds = 2000,
                                                                             Action<string, string> callback = null,
                                                                             CancellationToken cancellationToken = default(CancellationToken))
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

            var protocols = new[] { protocol };
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
                    select new { Service = service, Address = kvp.Key };

            return r.ToLookup(k => k.Service, k => k.Address);
        }

        /// <summary>
        /// Listens for mDNS Service Announcements
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task ListenForAnnouncementsAsync(Action<ServiceAnnouncement> callback, CancellationToken cancellationToken)
        {
            return NetworkInterface.ListenForAnnouncementsAsync((adapter, address, buffer) =>
            {
                var response = new Response(buffer);
                if (response.IsQueryResponse)
                    callback(new ServiceAnnouncement(adapter, ResponseToZeroconf(response, address)));
            }, cancellationToken);
        }
    }
}
