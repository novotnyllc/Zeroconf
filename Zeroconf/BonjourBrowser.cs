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

using Foundation;
using UIKit;
using Network;

using ObjCRuntime;

namespace Zeroconf
{
    class BonjourBrowser
    {
        NSNetServiceBrowser netServiceBrowser = new NSNetServiceBrowser();

        Dictionary<string, NSNetService> discoveredServiceDict = new Dictionary<string, NSNetService>();
        Dictionary<string, ZeroconfHost> zeroconfHostDict = new Dictionary<string, ZeroconfHost>();

        ResolveOptions _resolveOptions;
        Action<IZeroconfHost> _resolveCallback = null;
        CancellationToken _resolveCancellationToken;
        System.Net.NetworkInformation.NetworkInterface[] _resolveNetInterfacesToSendRequestOn;

        const double netServiceResolveTimeout = 5D;

        public BonjourBrowser()
        {
            netServiceBrowser.FoundDomain += Browser_FoundDomain;
            netServiceBrowser.DomainRemoved += Browser_DomainRemoved;

            netServiceBrowser.FoundService += Browser_FoundService;
            netServiceBrowser.ServiceRemoved += Browser_ServiceRemoved;

            netServiceBrowser.SearchStarted += Browser_SearchStarted;
            netServiceBrowser.NotSearched += Browser_NotSearched;
            netServiceBrowser.SearchStopped += Browser_SearchStopped;
        }

        public void SetResolveOptions(ResolveOptions resolveOptions,
                                        Action<IZeroconfHost> callback = null,
                                        CancellationToken cancellationToken = default(CancellationToken),
                                        System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            _resolveOptions = resolveOptions;
            _resolveCallback = callback;
            _resolveCancellationToken = cancellationToken;
            _resolveNetInterfacesToSendRequestOn = netInterfacesToSendRequestOn;
        }

        private void Browser_FoundDomain(object sender, NSNetDomainEventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_FoundDomain)}: Domain {e.Domain} MoreComing {e.MoreComing.ToString()}");
        }

        private void Browser_DomainRemoved(object sender, NSNetDomainEventArgs e)
        {
            Debug.WriteLine($"{nameof(Browser_FoundDomain)}: Domain {e.Domain} MoreComing {e.MoreComing.ToString()}");
        }

        private void Browser_FoundService(object sender, NSNetServiceEventArgs e)
        {
            NSNetService netService = e.Service;

            netService.AddressResolved += NetService_AddressResolved;
            netService.Stopped += NetService_Stopped;
            netService.ResolveFailure += NetService_ResolveFailure;

            Debug.WriteLine($"{nameof(Browser_FoundService)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                $"HostName {netService?.HostName} Port {netService?.Port} MoreComing {e.MoreComing.ToString()}");

            if (netService != null)
            {
                Debug.WriteLine($"{nameof(Browser_FoundService)}: {nameof(netService)}.Resolve({netServiceResolveTimeout.ToString()})");
                netService.Resolve(netServiceResolveTimeout);
            }
            else
            {
                Debug.WriteLine($"{nameof(Browser_FoundService)}: service is null");
            }
        }

        private void NetService_AddressResolved(object sender, EventArgs e)
        {
            NSNetService netService = null;

            if (sender is NSNetService)
            {
                netService = (NSNetService)sender;

                Debug.WriteLine($"{nameof(NetService_AddressResolved)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                    $"HostName {netService?.HostName} Port {netService?.Port} Addresses {GetZeroconfHostKey(netService)}");

                if (netService.TxtRecordData != null)
                {
                    NSDictionary dict = NSNetService.DictionaryFromTxtRecord(netService.TxtRecordData);
                    if (dict != null)
                    {
                        if (dict.Count > 0)
                        {
                            foreach (var key in dict.Keys)
                            {
                                Debug.WriteLine($"{nameof(Browser_FoundService)}: Key {key} Value {dict[key].ToString()}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"{nameof(Browser_FoundService)}: Service.DictionaryFromTxtRecord has 0 entries");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(Browser_FoundService)}: Service.DictionaryFromTxtRecord returned null");
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(Browser_FoundService)}: TxtRecordData is null");
                }

                string serviceKey = GetNsNetServiceKey(netService);
                lock (discoveredServiceDict)
                {
                    discoveredServiceDict[serviceKey] = netService;
                }
            }
        }

        private void NetService_ResolveFailure(object sender, NSNetServiceErrorEventArgs e)
        {
            NSNetService netService = null;

            if (sender is NSNetService)
            {
                netService = (NSNetService)sender;
            }

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

        private void NetService_Stopped(object sender, EventArgs e)
        {
            NSNetService netService = null;

            if (sender is NSNetService)
            {
                netService = (NSNetService)sender;
            }

            Debug.WriteLine($"{nameof(NetService_Stopped)}: Name {netService?.Name} Type {netService?.Type} Domain {netService?.Domain} " +
                $"HostName {netService?.HostName} Port {netService?.Port}");
        }

        private void Browser_ServiceRemoved(object sender, NSNetServiceEventArgs e)
        {
            NSNetService service = e.Service;

            Debug.WriteLine($"{nameof(Browser_ServiceRemoved)}: Name {service.Name} Type {service.Type} Domain {service.Domain} " + 
                $"HostName {service.HostName} Port {service.Port} MoreComing {e.MoreComing.ToString()}");

            string serviceKey = GetNsNetServiceKey(service);
            lock (discoveredServiceDict)
            {
                discoveredServiceDict.Remove(serviceKey);
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

        public void StartServiceSearch()
        {
            const string localDomainForParse = ".local.";
            const string localDomain = "local.";
            int localDomainLength = localDomain.Length;

            // All previous service discovery results are discarded

            lock (discoveredServiceDict)
            {
                discoveredServiceDict.Clear();
            }

            lock (zeroconfHostDict)
            {
                zeroconfHostDict.Clear();
            }

            foreach (var protocol in _resolveOptions.Protocols)
            {
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
            Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}");

            lock (zeroconfHostDict)
            {
                zeroconfHostDict.Clear();
            }

            RefreshZeroconfHostDict();

            List<IZeroconfHost> hostList = new List<IZeroconfHost>();

            lock (zeroconfHostDict)
            {
                hostList.AddRange(zeroconfHostDict.Values);
            }

            Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}: Returning hostList.Count {hostList.Count}");

            return hostList;
        }

        void RefreshZeroconfHostDict()
        {
            // Do not walk discoveredServiceDict[] directly
            // If a NSNetService is in discoveredServiceDict[], it was resolved successfully before it was added

            List<NSNetService> nsNetServiceList = new List<NSNetService>();
            lock (discoveredServiceDict)
            {
                nsNetServiceList.AddRange(discoveredServiceDict.Values);
            }

            // For each NSNetService, create a ZeroconfHost record

            foreach (var nsNetService in nsNetServiceList)
            {
                Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}: Name {nsNetService.Name} Type {nsNetService.Type} Domain {nsNetService.Domain} " +
                    $"HostName {nsNetService.HostName} Port {nsNetService.Port}");

                // Obtain or create ZeroconfHost

                ZeroconfHost host = GetOrCreateZeroconfHost(nsNetService);

                // Add service to ZeroconfHost record

                Service svc = new Service();
                svc.Name = GetNsNetServiceName(nsNetService);
                svc.Port = (int)nsNetService.Port;
                // svc.Ttl = is not available

                NSData txtRecordData = nsNetService.GetTxtRecordData();
                if (txtRecordData != null)
                {
                    NSDictionary txtDict = NSNetService.DictionaryFromTxtRecord(txtRecordData);
                    if (txtDict != null)
                    {
                        if (txtDict.Count > 0)
                        {
                            foreach (var key in txtDict.Keys)
                            {
                                Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}: Key {key} Value {txtDict[key].ToString()}");
                            }

                            Dictionary<string, string> propertyDict = new Dictionary<string, string>();

                            foreach (var key in txtDict.Keys)
                            {
                                propertyDict[key.ToString()] = txtDict[key].ToString();
                            }
                            svc.AddPropertySet(propertyDict);
                        }
                        else
                        {
                            Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}: Service.DictionaryFromTxtRecord has 0 entries");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(ReturnZeroconfHostResults)}: Service.DictionaryFromTxtRecord returned null");
                    }
                }

                host.AddService(svc);
            }
        }

        ZeroconfHost GetOrCreateZeroconfHost(NSNetService service)
        {
            ZeroconfHost host;
            string hostKey = GetZeroconfHostKey(service);

            lock (zeroconfHostDict)
            {
                if (!zeroconfHostDict.TryGetValue(hostKey, out host))
                {
                    host = new ZeroconfHost();
                    host.DisplayName = service.Name;

                    List<string> ipAddrList = new List<string>();
                    foreach (NSData address in service.Addresses)
                    {
                        Sockaddr saddr = Sockaddr.CreateSockaddr(address.Bytes);
                        IPAddress ipAddr = Sockaddr.CreateIPAddress(saddr);
                        if (ipAddr != null)
                        {
                            ipAddrList.Add(ipAddr.ToString());
                        }
                    }
                    host.IPAddresses = ipAddrList;

                    host.Id = host.IPAddress;

                    zeroconfHostDict[hostKey] = host;
                }
            }

            return host;
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
            StringBuilder sb = new StringBuilder();

            foreach (NSData address in service.Addresses)
            {
                Sockaddr saddr = Sockaddr.CreateSockaddr(address.Bytes);
                IPAddress ipAddr = Sockaddr.CreateIPAddress(saddr);
                if (ipAddr != null)
                {
                    sb.Append((sb.Length == 0 ? ipAddr.ToString() : $";{ipAddr.ToString()}"));
                }
            }

            return sb.ToString();
        }
    }
}
#endif