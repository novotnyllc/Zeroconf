#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.UI.Xaml;
using Zeroconf;

namespace ZeroconfTest.RT
{
    public sealed partial class MainPage
    {
        public const string PDL_DATASTREAM_SERVICE_TYPE = "_pdl-datastream._tcp.local.";
        public const string IPP_TCP_SERVICE_TYPE = "_ipp._tcp.local.";
        public const string IPRINTER_TCP_SERVICE_TYPE = "_printer._tcp.local.";

        private CancellationTokenSource _cancelTokenSource;
        private int deviceCount;
        private int domainCount;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void Resolve_Click(object sender, RoutedEventArgs e)
        {
            //Action<IZeroconfRecord> onMessage = record => Console.WriteLine("On Message: {0}", record);
            //var domains = await ZeroconfResolver.BrowseDomainsAsync();
            deviceCount = 0;
            var protocols = new List<string> { PDL_DATASTREAM_SERVICE_TYPE, IPP_TCP_SERVICE_TYPE, IPRINTER_TCP_SERVICE_TYPE };

            try
            {
                _cancelTokenSource = new CancellationTokenSource();
                var responses = await ZeroconfResolver.ResolveAsync(protocols, new TimeSpan(0, 0, 10), 2, 2000, NotifyDeviceFound, false, _cancelTokenSource.Token);

                //var domains = await ZeroconfResolver.BrowseDomainsAsync();
                //var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key), new TimeSpan(0,0,10), 2, 2000, NotifyDeviceFound);

                //var responses = await ZeroconfResolver.ResolveAsync("_printer._tcp.local.", default(TimeSpan), 2, 2000, NotifyDeviceFound);

                //foreach (var resp in responses)
                //    Console.WriteLine(resp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                domainCount = 0;
                _cancelTokenSource = new CancellationTokenSource();
                var responses = await ZeroconfResolver.BrowseDomainsAsync(new TimeSpan(0, 0, 10), 2, 2000, DomainsFound, false, _cancelTokenSource.Token);

                //foreach (var service in responses)
                //{
                //    Console.WriteLine(service.Key);

                //    foreach (var host in service)
                //        Console.WriteLine("\tIP: " + host);

                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void NotifyDeviceFound(IZeroconfHost device)
        {
            if (device != null)
            {
                Debug.WriteLine((++deviceCount).ToString() + " - " + device.DisplayName + " : " + device.IPAddress);
                //Debug.WriteLine((++deviceCount).ToString() + " - " + device);
            }
            else
                Debug.WriteLine("Scan complete!!!!");
        }

        private void DomainsFound(string service, string address)
        {
            if (!string.IsNullOrEmpty(service) && !string.IsNullOrEmpty(address))
            {
                Debug.WriteLine((++domainCount).ToString() + " - " + service + " : " + address);
            }
            else
                Debug.WriteLine("Scan complete!!!!");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancelTokenSource.Cancel();
        }
    }
}
