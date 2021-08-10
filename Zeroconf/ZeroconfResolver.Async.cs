using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Heijden.DNS;

#if __IOS__
using UIKit;
#endif

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
        /// <param name="netInterfacesToSendRequestOn">The network interfaces/adapters to use. Use all if null</param>
        /// <returns></returns>
        public static Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(string protocol,
                                                                      TimeSpan scanTime = default(TimeSpan),
                                                                      int retries = 2,
                                                                      int retryDelayMilliseconds = 2000,
                                                                      Action<IZeroconfHost> callback = null,
                                                                      CancellationToken cancellationToken = default(CancellationToken),
                                                                      System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException(nameof(protocol));

            return ResolveAsync(new[] { protocol },
                                scanTime,
                                retries,
                                retryDelayMilliseconds, callback, cancellationToken, netInterfacesToSendRequestOn);
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
        /// <param name="netInterfacesToSendRequestOn">The network interfaces/adapters to use. Use all if null</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(IEnumerable<string> protocols,
                                                                            TimeSpan scanTime = default(TimeSpan),
                                                                            int retries = 2,
                                                                            int retryDelayMilliseconds = 2000,
                                                                            Action<IZeroconfHost> callback = null,
                                                                            CancellationToken cancellationToken = default(CancellationToken),
                                                                            System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (retries <= 0) throw new ArgumentOutOfRangeException(nameof(retries));
            if (retryDelayMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(retryDelayMilliseconds));
            if (scanTime == default(TimeSpan))
                scanTime = TimeSpan.FromSeconds(2);

            var options = new ResolveOptions(protocols)
            {
                Retries = retries,
                RetryDelay = TimeSpan.FromMilliseconds(retryDelayMilliseconds),
                ScanTime = scanTime
            };

            return await ResolveAsync(options, callback, cancellationToken, netInterfacesToSendRequestOn).ConfigureAwait(false);   
        }


        /// <summary>
        ///     Resolves available ZeroConf services
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <param name="netInterfacesToSendRequestOn">The network interfaces/adapters to use. Use all if null</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(ResolveOptions options,
                                                                            Action<IZeroconfHost> callback = null,
                                                                            CancellationToken cancellationToken = default(CancellationToken),
                                                                            System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
#if !__IOS__
            Action<string, Response> wrappedAction = null;
            
            if (callback != null)
            {
                wrappedAction = (address, resp) =>
                {
                    var zc = ResponseToZeroconf(resp, address, options);
                    if (zc.Services.Any(s => options.Protocols.Contains(s.Key)))
                    {
                        callback(zc);
                    }
                };
            }
            
            var dict = await ResolveInternal(options,
                                             wrappedAction,
                                             cancellationToken,
                                             netInterfacesToSendRequestOn)
                                 .ConfigureAwait(false);

            return dict.Select(pair => ResponseToZeroconf(pair.Value, pair.Key, options))
                       .Where(zh => zh.Services.Any(s => options.Protocols.Contains(s.Key))) // Ensure we only return records that have matching services
                       .ToList();
#else
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5))
            {
                return await ZeroconfNetServiceBrowser.ResolveAsync(options, callback, cancellationToken, netInterfacesToSendRequestOn);
            }
            else
            {
                Action<string, Response> wrappedAction = null;

                if (callback != null)
                {
                    wrappedAction = (address, resp) =>
                    {
                        var zc = ResponseToZeroconf(resp, address, options);
                        if (zc.Services.Any(s => options.Protocols.Contains(s.Key)))
                        {
                            callback(zc);
                        }
                    };
                }

                var dict = await ResolveInternal(options,
                                                 wrappedAction,
                                                 cancellationToken,
                                                 netInterfacesToSendRequestOn)
                                     .ConfigureAwait(false);

                return dict.Select(pair => ResponseToZeroconf(pair.Value, pair.Key, options))
                           .Where(zh => zh.Services.Any(s => options.Protocols.Contains(s.Key))) // Ensure we only return records that have matching services
                           .ToList();
            }
#endif
        }

        // Should be set to the list of allowed protocols from info.plist; entries must include domain including terminating dot (usually ".local.")
        // Used by BrowseDomainAsync only; the hack is that "browsing" is really just ResolveAsync() with the result formatted differently
        static List<string> browseDomainProtocolList = new List<string>();

        /// <summary>
        ///     Sets browse domain protocols (provided using pattern "[protocol].[domain].") for Xamarin iOS 14.5+ integration
        /// </summary>
        /// <param name="protocols">IEnumerable of string browse domain protocols</param>
        /// <returns></returns>
        public static void SetBrowseDomainProtocols(IEnumerable<string> protocols)
        {
            if (protocols == null) { throw new ArgumentException(nameof(protocols)); }
            browseDomainProtocolList.Clear();

            foreach (var protocol in protocols)
            {
                if (protocol != null)
                {
                    browseDomainProtocolList.Add(protocol);
                }
            }
        }

        /// <summary>
        ///     Returns all available domains with services on them
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <param name="netInterfacesToSendRequestOn">The network interfaces/adapters to use. Use all if null</param>
        /// <returns></returns>
        public static async Task<ILookup<string, string>> BrowseDomainsAsync(TimeSpan scanTime = default(TimeSpan),
                                                                             int retries = 2,
                                                                             int retryDelayMilliseconds = 2000,
                                                                             Action<string, string> callback = null,
                                                                             CancellationToken cancellationToken = default(CancellationToken),
                                                                             System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)

        {
            if (retries <= 0) throw new ArgumentOutOfRangeException(nameof(retries));
            if (retryDelayMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(retryDelayMilliseconds));
            if (scanTime == default(TimeSpan))
                scanTime = TimeSpan.FromSeconds(2);

            var options = new BrowseDomainsOptions
            {
                Retries = retries,
                RetryDelay = TimeSpan.FromMilliseconds(retryDelayMilliseconds),
                ScanTime = scanTime
            };

            return await BrowseDomainsAsync(options, callback, cancellationToken, netInterfacesToSendRequestOn).ConfigureAwait(false);
        }

        /// <summary>
        ///     Returns all available domains with services on them
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="netInterfacesToSendRequestOn">The network interfaces/adapters to use. Use all if null</param>
        /// <returns></returns>
        public static async Task<ILookup<string, string>> BrowseDomainsAsync(BrowseDomainsOptions options,
                                                                             Action<string, string> callback = null,
                                                                             CancellationToken cancellationToken = default(CancellationToken),
                                                                             System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
       
#if !__IOS__
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
            
            var dict = await ResolveInternal(options,
                                             wrappedAction,
                                             cancellationToken,
                                             netInterfacesToSendRequestOn)
                                 .ConfigureAwait(false);

            var r = from kvp in dict
                    from service in BrowseResponseParser(kvp.Value)
                    select new { Service = service, Address = kvp.Key };

            return r.ToLookup(k => k.Service, k => k.Address);
#else
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5))
            {
                return await ZeroconfNetServiceBrowser.BrowseDomainsAsync(browseDomainProtocolList, options, callback, cancellationToken, netInterfacesToSendRequestOn);
            }
            else
            {
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

                var dict = await ResolveInternal(options,
                                                 wrappedAction,
                                                 cancellationToken,
                                                 netInterfacesToSendRequestOn)
                                     .ConfigureAwait(false);

                var r = from kvp in dict
                        from service in BrowseResponseParser(kvp.Value)
                        select new { Service = service, Address = kvp.Key };

                return r.ToLookup(k => k.Service, k => k.Address);
            }
#endif
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
                    callback(new ServiceAnnouncement(adapter, ResponseToZeroconf(response, address, null)));
            }, cancellationToken);
        }
    }
}
