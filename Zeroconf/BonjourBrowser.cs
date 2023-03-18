#if __IOS__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Network;


namespace Zeroconf
{
    class BonjourBrowser
    {
        NSNetServiceBrowser netServiceBrowser = new NSNetServiceBrowser();

        Dictionary<string, ZeroconfHost> zeroconfHostDict = new Dictionary<string, ZeroconfHost>();
        HashSet<string> domainHash = new HashSet<string>();

        double netServiceResolveTimeout;

        /// <summary>
        /// Implements iOS mDNS browse and resolve
        /// </summary>
        /// <param name="resolveTimeout">Time limit for NSNetService.Resolve() operation</param>
        public BonjourBrowser(TimeSpan resolveTimeout = default(TimeSpan))
        {
            netServiceBrowser.FoundDomain += Browser_FoundDomain;
            netServiceBrowser.DomainRemoved += Browser_DomainRemoved;

            netServiceBrowser.FoundService += Browser_FoundService;
            netServiceBrowser.ServiceRemoved += Browser_ServiceRemoved;

            netServiceBrowser.SearchStarted += Browser_SearchStarted;
            netServiceBrowser.NotSearched += Browser_NotSearched;
            netServiceBrowser.SearchStopped += Browser_SearchStopped;

            netServiceResolveTimeout = (resolveTimeout != default(TimeSpan)) ? resolveTimeout.TotalSeconds : 5d;
        }

        private void Browser_FoundDomain(object sender, NSNetDomainEventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_FoundDomain)}: Domain {e.Domain} MoreComing {e.MoreComing.ToString()}");
            lock (domainHash)
            {
                domainHash.Add(e.Domain);
            }
        }

        private void Browser_DomainRemoved(object sender, NSNetDomainEventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_FoundDomain)}: Domain {e.Domain} MoreComing {e.MoreComing.ToString()}");
            lock (domainHash)
            {
                domainHash.Remove(e.Domain);
            }
        }

        private void Browser_FoundService(object sender, NSNetServiceEventArgs e)
        {
            if (e.Service is NSNetService netService)
            {
                netService.AddressResolved += NetService_AddressResolved;
                netService.Stopped += NetService_Stopped;
                netService.ResolveFailure += NetService_ResolveFailure;

                Debug.WriteLine($"{nameof(Browser_FoundService)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                    $"HostName {netService?.HostName} Port {netService?.Port} MoreComing {e.MoreComing.ToString()}");

                Debug.WriteLine($"{nameof(Browser_FoundService)}: {nameof(netService)}.Resolve({netServiceResolveTimeout.ToString()})");
                netService.Resolve(netServiceResolveTimeout);
            }
        }

        private void NetService_AddressResolved(object sender, EventArgs e)
        {
            if (sender is NSNetService netService)
            {
                Debug.WriteLine($"{nameof(NetService_AddressResolved)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                    $"HostName {netService?.HostName} Port {netService?.Port} Addresses {GetZeroconfHostKey(netService)}");

                RefreshZeroconfHostDict(netService);
            }
        }

        private void NetService_ResolveFailure(object sender, NSNetServiceErrorEventArgs e)
        {
            if (sender is NSNetService netService)
            {
                netService = (NSNetService)sender;

                Debug.WriteLine($"{nameof(NetService_ResolveFailure)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                    $"HostName {netService?.HostName} Port {netService?.Port}");

                NSDictionary errors = e.Errors;

                Debug.WriteLine($"{nameof(NetService_ResolveFailure)}: Errors {errors?.ToString()}");

                if (errors != null)
                {
                    if (errors.Count > 0)
                    {
                        foreach (var key in errors.Keys)
                        {
                            Debug.WriteLine($"{nameof(NetService_ResolveFailure)}: Key {key} Value {errors[key].ToString()}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(NetService_ResolveFailure)}: errors has 0 entries");
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(NetService_ResolveFailure)}: errors is null");
                }
            }
        }

        private void NetService_Stopped(object sender, EventArgs e)
        {
            if (sender is NSNetService netService)
            {
                netService = (NSNetService)sender;

                Debug.WriteLine($"{nameof(NetService_Stopped)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                    $"HostName {netService?.HostName} Port {netService?.Port}");
            }
        }

        private void Browser_ServiceRemoved(object sender, NSNetServiceEventArgs e)
        {
            NSNetService service = e.Service;

            Debug.WriteLine($"{nameof(Browser_ServiceRemoved)}: Name {service.Name} Type {service.Type} Domain {service.Domain} " +
                $"HostName {service.HostName} Port {service.Port} MoreComing {e.MoreComing.ToString()}");

            string hostKey = GetZeroconfHostKey(service);
            string serviceKey = GetNsNetServiceName(service);
            lock (zeroconfHostDict)
            {
                if (zeroconfHostDict.TryGetValue(hostKey, out var zeroconfHost))
                {
                    zeroconfHost.RemoveService(serviceKey);
                }
            }
        }

        private void Browser_SearchStarted(object sender, EventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_SearchStarted)}");
        }

        private void Browser_NotSearched(object sender, NSNetServiceErrorEventArgs e)
        {
            NSDictionary errors = e.Errors;

            Debug.WriteLine($"{nameof(Browser_NotSearched)}: Errors {errors?.ToString()}");

            if (errors != null)
            {
                if (errors.Count > 0)
                {
                    foreach (var key in errors.Keys)
                    {
                        Debug.WriteLine($"{nameof(Browser_NotSearched)}: Key {key} Value {errors[key].ToString()}");
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(Browser_NotSearched)}: errors has 0 entries");
                }
            }
            else
            {
                Debug.WriteLine($"{nameof(Browser_NotSearched)}: errors is null");
            }
        }

        private void Browser_SearchStopped(object sender, EventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_SearchStopped)}");
        }

        //
        // Start/Stop search NSNetService search
        //

        public void StartDomainSearch()
        {
            Debug.WriteLine($"{nameof(StartDomainSearch)}: {nameof(netServiceBrowser)}.SearchForBrowsableDomains()");

            lock (domainHash)
            {
                domainHash.Clear();
            }

            netServiceBrowser.SearchForBrowsableDomains();
        }

        public void StopDomainSearch()
        {
            Debug.WriteLine($"{nameof(StopDomainSearch)}: {nameof(netServiceBrowser)}.Stop()");
            netServiceBrowser.Stop();
        }

        public List<string> GetFoundDomainList()
        {
            List<string> results = new List<string>();

            lock (domainHash)
            {
                results.AddRange(domainHash.ToList());
            }

            return results;
        }

        public void StartServiceSearch(string protocol)
        {
            const string localDomainForParse = ".local.";
            const string localDomain = "local.";
            int localDomainLength = localDomain.Length;

            // All previous service discovery results are discarded

            string serviceType = string.Empty;
            string domain = string.Empty;

            if (protocol.ToLower().EndsWith(localDomainForParse))
            {
                serviceType = protocol.Substring(0, protocol.Length - localDomainLength);
                domain = protocol.Substring(serviceType.Length);
            }
            else
            {
                serviceType = BonjourBrowser.GetServiceType(protocol);
                if (serviceType != null)
                {
                    if (protocol.Length > serviceType.Length)
                    {
                        domain = protocol.Substring(serviceType.Length);

                        //           6 = delim.Length
                        //          /----\ 
                        // _foo._bar._tcp. example.com.
                        // 012345678901234 567890123456 index = [0, 26]
                        // 123456789012345 678901234567 length = 27
                        //   serviceType      domain
                    }
                    else
                    {
                        domain = string.Empty;
                    }
                }
                else
                {
                    serviceType = protocol;
                    domain = string.Empty;
                }
            }

            Debug.WriteLine($"{nameof(StartServiceSearch)}: {nameof(netServiceBrowser)}.SearchForServices(Type {serviceType} Domain {domain})");

            netServiceBrowser.SearchForServices(serviceType, domain);
        }


        public void StopServiceSearch()
        {
            Debug.WriteLine($"{nameof(StopServiceSearch)}: {nameof(netServiceBrowser)}.Stop()");
            netServiceBrowser.Stop();
        }

        public static string GetServiceType(string protocol, bool includeTcpUdpDelimiter = true)
        {
            string serviceType = null;
            string[] delimArray = { "._tcp.", "._udp." };

            foreach (string delim in delimArray)
            {
                if (protocol.Contains(delim))
                {
                    if (includeTcpUdpDelimiter)
                    {
                        serviceType = protocol.Substring(0, protocol.IndexOf(delim) + delim.Length);
                    }
                    else
                    {
                        serviceType = protocol.Substring(0, protocol.IndexOf(delim));
                    }
                    break;
                }
            }

            return serviceType;
        }

        //
        // ZeroconfHost results
        //

        public IReadOnlyList<IZeroconfHost> ReturnZeroconfHostResults()
        {
            lock (zeroconfHostDict)
            {
                return zeroconfHostDict.Values.OfType<IZeroconfHost>().ToList();
            }
        }

        void RefreshZeroconfHostDict(NSNetService nsNetService)
        {
            Debug.WriteLine($"{nameof(RefreshZeroconfHostDict)}: Name {nsNetService.Name} Type {nsNetService.Type} Domain {nsNetService.Domain} " +
                    $"HostName {nsNetService.HostName} Port {nsNetService.Port}");

            // Obtain or create ZeroconfHost

            ZeroconfHost host = GetOrCreateZeroconfHost(nsNetService);

            // Add service to ZeroconfHost record

            Service svc = new Service();
            svc.Name = GetNsNetServiceName(nsNetService);
            svc.Port = (int)nsNetService.Port;
            // svc.Ttl = is not available

            NSData txtRecordData = nsNetService.GetTxtRecordData();
            if (txtRecordData is not null)
            {
                NSDictionary txtDict = NSNetService.DictionaryFromTxtRecord(txtRecordData);
                if (txtDict?.Any() is true)
                {
                    Debug.WriteLine($"{nameof(RefreshZeroconfHostDict)}: {string.Join(Environment.NewLine, txtDict.Select(r => $"Key {r.Key} Value {r.Value}"))}");

                    Dictionary<string, string> propertyDict = txtDict.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
                    svc.AddPropertySet(propertyDict);
                }
            }

            lock (zeroconfHostDict)
            {
                host.AddService(svc);
            }
        }

        ZeroconfHost GetOrCreateZeroconfHost(NSNetService service)
        {
            var hostKey = GetZeroconfHostKey(service);

            lock (zeroconfHostDict)
            {
                if (!zeroconfHostDict.TryGetValue(hostKey, out var host))
                {
                    host = new()
                    {
                        DisplayName = service.Name,
                        IPAddresses = service.Addresses?.Select(a => $@"{Sockaddr.CreateIPAddress(Sockaddr.CreateSockaddr(a.Bytes))}").ToList() ?? new(),
                    };
                    host.Id = host.IPAddress;
                    zeroconfHostDict[hostKey] = host;
                }
                return host;
            }
        }

        //
        // NsNetService utils
        //

        string GetNsNetServiceKey(NSNetService service)
        {
            string hostKey = GetZeroconfHostKey(service);
            return $"{hostKey}:{service.Type}{service.Domain}";
        }

        string GetNsNetServiceName(NSNetService service)
        {
            return $"{service.Type}{service.Domain}";
        }

        string GetZeroconfHostKey(NSNetService service)
        {
            if (service.Addresses is null)
            {
                return string.Empty;
            }

            return string.Join(';', service.Addresses.Select(a => Sockaddr.CreateIPAddress(Sockaddr.CreateSockaddr(a.Bytes))));
        }

        public static List<string> GetNSBonjourServices(string domain = null)
        {
            List<string> browseServiceList = new List<string>();

            var bundle = CFBundle.GetMain();
            var infoPlistNsDict = bundle?.InfoDictionary;

            const string infoPlistKey = "NSBonjourServices";
            var nsBonjourServices = infoPlistNsDict?[infoPlistKey];
            if (nsBonjourServices != null)
            {
                Console.WriteLine($"Found Info.plist key {infoPlistKey}");

                if (nsBonjourServices is NSArray)
                {
                    var nsBonjourServiceArr = NSArray.ArrayFromHandle<NSString>(nsBonjourServices.Handle);
                    foreach (var serviceItem in nsBonjourServiceArr)
                    {
                        var serviceItemStr = serviceItem.ToString();
                        var browseService = (domain != null) ? $"{serviceItemStr}.{domain}" : serviceItemStr;

                        Console.WriteLine($"  {infoPlistKey} PlistItem={serviceItemStr} BrowseService={browseService}");
                        browseServiceList.Add(browseService);
                    }
                }
                else
                {
                    throw new Exception($"Info.plist contains {infoPlistKey} but the value is not an array");
                }
            }
            else
            {
                throw new ArgumentNullException($"Info.plist does not contain {infoPlistKey} array");
            }

            return browseServiceList;
        }
    }
}
#endif