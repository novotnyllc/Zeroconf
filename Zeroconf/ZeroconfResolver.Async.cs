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
            return await ResolveAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
#else
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5))
            {
                if (UseBSDSocketsZeroconfOniOS)
                {
                    return await ResolveAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
                }
                else
                {
                    return await ZeroconfNetServiceBrowser.ResolveAsync(options, callback, cancellationToken, netInterfacesToSendRequestOn);
                }
            }
            else
            {
                return await ResolveAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
            }
#endif
        }

        internal static async Task<IReadOnlyList<IZeroconfHost>> ResolveAsyncOriginal(ResolveOptions options,
                                                                                Action<IZeroconfHost> callback = null,
                                                                                CancellationToken cancellationToken = default(CancellationToken),
                                                                                System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            Action<string, Response> wrappedAction = null;

            if (callback != null)
            {
                wrappedAction = (address, resp) =>
                {
                    var zc = ResponseToZeroconf(resp, address, options);
                    if (options.Protocols.Any(p => zc.Services.Keys.Any(k => k.Contains(p))))
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
                       .Where(zh => options.Protocols.Any(p => zh.Services.Keys.Any(k => k.Contains(p)))) // Ensure we only return records that have matching services
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
            return await BrowseDomainsAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
#else
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5))
            {
                if (UseBSDSocketsZeroconfOniOS)
                {
                    return await BrowseDomainsAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
                }
                else
                {
                    return await ZeroconfNetServiceBrowser.BrowseDomainsAsync(options, callback, cancellationToken, netInterfacesToSendRequestOn);
                }
            }
            else
            {
                return await BrowseDomainsAsyncOriginal(options, callback, cancellationToken, netInterfacesToSendRequestOn);
            }
#endif
        }

        internal static async Task<ILookup<string, string>> BrowseDomainsAsyncOriginal(BrowseDomainsOptions options,
                                                                                Action<string, string> callback = null,
                                                                                CancellationToken cancellationToken = default(CancellationToken),
                                                                                System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
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


        /// <summary>
        /// Forces Xamarin.iOS running on iOS 14.5 or greater to use original Zeroconf BSD Sockets API
        /// 
        /// This would be set to true only when the app possesses the com.apple.developer.networking.multicast entitlement.
        /// Default value is false (which means use the NSNetServiceBrowser workaround when running on iOS 14.5 or greater)
        /// Has no effect on platforms other than Xamarin.iOS
        /// </summary>
        public static bool UseBSDSocketsZeroconfOniOS { get; set; } = false;

        /// <summary>
        /// Returns true when iOS version of app is running on iOS 14.5+ and workaround has not been
        /// suppressed with UseBSDSocketsZeroconfOniOS property. Returns false in all other cases
        /// </summary>
        public static bool IsiOSWorkaroundEnabled
        {
            get
            {
                var result = false;

#if __IOS__
                if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5) && !UseBSDSocketsZeroconfOniOS)
                {
                    result = true;
                }
#endif

                return result;
            }
        }

        /// <summary>
        /// Xamarin.iOS only: returns the list of NSBonjourServices from Info.plist
        /// </summary>
        /// <param name="domain">Optional domain (example: "local.") to append to each service; null = no domain appended; non-null must terminate with "."</param>
        /// <returns></returns>
        public static IReadOnlyList<string> GetiOSInfoPlistServices(string domain = null)
        {
            var serviceList = new List<string>();

#if __IOS__
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5) && !UseBSDSocketsZeroconfOniOS)
            {
                serviceList.AddRange(BonjourBrowser.GetNSBonjourServices(domain));
            }
#endif

            return serviceList;
        }

        /// <summary>
        /// Xamarin.iOS only: returns the list of NSBonjourServices from Info.plist
        /// </summary>
        /// <param name="scanTime">How long NSNetServiceBrowser will scan for mDNS domains (default is 2 seconds)</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static
#if __IOS__
            async 
#endif
            Task<IReadOnlyList<string>> GetiOSDomains(TimeSpan scanTime = default(TimeSpan),
                                                                        CancellationToken cancellationToken = default(CancellationToken))
        {
#if __IOS__
            var domainList = new List<string>();

            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 5) && !UseBSDSocketsZeroconfOniOS)
            {
                domainList.AddRange(await ZeroconfNetServiceBrowser.GetDomains((scanTime != default(TimeSpan)) ? scanTime : TimeSpan.FromSeconds(2), cancellationToken));
            }
            return domainList;
#else
            return Task.FromResult((IReadOnlyList<string>)new List<string>());
#endif

        }
    }
}
