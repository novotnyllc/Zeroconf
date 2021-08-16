#if __IOS__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zeroconf
{
    static class ZeroconfNetServiceBrowser
    {
        static internal async Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(ResolveOptions options,
                                                            Action<IZeroconfHost> callback = null,
                                                            CancellationToken cancellationToken = default(CancellationToken),
                                                            System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (netInterfacesToSendRequestOn != null)
            {
                throw new NotImplementedException($"iOS NSNetServiceBrowser/NSNetService does not support per-network interface requests");
            }

            List<IZeroconfHost> combinedResultList = new List<IZeroconfHost>();

            // Seems you must reuse the one BonjourBrowser (which is really an NSNetServiceBrowser)... multiple instances do not play well together

            BonjourBrowser bonjourBrowser = new BonjourBrowser(options.ScanTime);

            foreach (var protocol in options.Protocols)
            {
                bonjourBrowser.StartServiceSearch(protocol);

                await Task.Delay(options.ScanTime, cancellationToken).ConfigureAwait(false);

                bonjourBrowser.StopServiceSearch();

                // Simpleminded callback implementation
                var results = bonjourBrowser.ReturnZeroconfHostResults();
                foreach (var result in results)
                {
                    if (callback != null)
                    {
                        callback(result);
                    }
                }

                combinedResultList.AddRange(results);
            }

            return combinedResultList;
        }

        static internal async Task<ILookup<string, string>> BrowseDomainsAsync(BrowseDomainsOptions options,
                                                                     Action<string, string> callback = null,
                                                                     CancellationToken cancellationToken = default(CancellationToken),
                                                                     System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (netInterfacesToSendRequestOn != null)
            {
                throw new NotImplementedException($"iOS NSNetServiceBrowser/NSNetService does not support per-network interface requests");
            }

            var browseDomainProtocolList = BonjourBrowser.GetNSBonjourServices();

            ResolveOptions resolveOptions = new ResolveOptions(browseDomainProtocolList);
            var zeroconfResults = await ResolveAsync(resolveOptions, callback: null, cancellationToken, netInterfacesToSendRequestOn);

            List<IntermediateResult> resultsList = new List<IntermediateResult>();
            foreach (var host in zeroconfResults)
            {
                foreach (var service in host.Services)
                {
                    foreach (var ipAddr in host.IPAddresses)
                    {
                        IntermediateResult b = new IntermediateResult();
                        b.ServiceNameAndDomain = service.Key;
                        b.HostIPAndService = $"{ipAddr}: {BonjourBrowser.GetServiceType(service.Value.Name, includeTcpUdpDelimiter: false)}";

                        resultsList.Add(b);

                        // Simpleminded callback implementation
                        if (callback != null)
                        {
                            callback(service.Key, ipAddr);
                        }
                    }
                }
            }

            ILookup<string, string> results = resultsList.ToLookup(k => k.ServiceNameAndDomain, h => h.HostIPAndService);
            return results;
        }

        class IntermediateResult
        {
            public string ServiceNameAndDomain;
            public string HostIPAndService;
        }

        static internal async Task<IReadOnlyList<string>> GetDomains(TimeSpan scanTime, CancellationToken cancellationToken = default(CancellationToken))
        {
            BonjourBrowser bonjourBrowser = new BonjourBrowser(scanTime);

            bonjourBrowser.StartDomainSearch();

            await Task.Delay(scanTime, cancellationToken).ConfigureAwait(false);

            bonjourBrowser.StopDomainSearch();

            IReadOnlyList<string> domainList = bonjourBrowser.GetFoundDomainList();
            return domainList;
        }
    }
}
#endif