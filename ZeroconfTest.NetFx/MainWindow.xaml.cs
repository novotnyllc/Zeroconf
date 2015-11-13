using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Zeroconf;

namespace ZeroconfTest.NetFx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancelTokenSource;
        private int deviceCount;
        private int domainCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Resolve_Click(object sender, RoutedEventArgs e)
        {
            //Action<IZeroconfRecord> onMessage = record => Console.WriteLine("On Message: {0}", record);
            deviceCount = 0;

            try
            {
                _cancelTokenSource = new CancellationTokenSource();

                var domains = await ZeroconfResolver.BrowseDomainsAsync();
                var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key), new TimeSpan(0,0,10), 2, 2000, NotifyDeviceFound, false, _cancelTokenSource.Token);

                //var responses = await ZeroconfResolver.ResolveAsync("_printer._tcp.local.", default(TimeSpan), 2, 2000, NotifyDeviceFound);

                //foreach (var resp in responses)
                //    Console.WriteLine(resp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                Console.WriteLine(ex.Message);
            }
        }

        private void NotifyDeviceFound(IZeroconfHost device)
        {
            if (device != null)
            {
                Console.WriteLine((++deviceCount).ToString() + " - " + device.DisplayName + " : " + device.IPAddress);
                //Console.WriteLine((++deviceCount).ToString() + " - " + device);
            }
            else
                Console.WriteLine("Scan complete!!!!");
        }

        private void DomainsFound(string service, string address)
        {
            if (!string.IsNullOrEmpty(service) && !string.IsNullOrEmpty(address))
            {
                Console.WriteLine((++domainCount).ToString() + " - " + service + " : " + address);
            }
            else
                Console.WriteLine("Scan complete!!!!");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancelTokenSource.Cancel();
        }
    }
}
