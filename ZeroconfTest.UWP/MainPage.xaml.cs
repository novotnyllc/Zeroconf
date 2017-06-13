#define DEBUG

using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Zeroconf;

namespace ZeroconfTest.UWP
{
    public sealed partial class MainPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        async void ResolveClick(object sender, RoutedEventArgs e)
        {
            
            var domains = await ZeroconfResolver.BrowseDomainsAsync();

            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            foreach (var resp in responses)
                WriteLogLine(resp.ToString());
        }

        async void BrowseClick(object sender, RoutedEventArgs e)
        {
            var responses = await ZeroconfResolver.BrowseDomainsAsync();

            foreach (var service in responses)
            {
                WriteLogLine(service.Key);

                foreach (var host in service)
                    WriteLogLine("\tIP: " + host);

            }
        }

        void OnAnnouncement(ServiceAnnouncement sa)
        {
            WriteLogLine("---- Announced on {0} ({1}) ----", sa.AdapterInformation.Name, sa.AdapterInformation.Address);
            WriteLogLine(sa.Host.ToString());
        }

        IDisposable listenSubscription;
        IObservable<ServiceAnnouncement> subscription;

        void ListenClick(object sender, RoutedEventArgs e)
        {
            if (listenSubscription != null)
            {
                listenSubscription.Dispose();
                listenSubscription = null;
            }
            else
            {
                subscription = ZeroconfResolver.ListenForAnnouncementsAsync();
                listenSubscription = subscription.Subscribe(OnAnnouncement);
            }
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        async void WriteLogLine(string text, params object[] args)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Log.Text += string.Format(text, args) + "\r\n";
                scollViewer.ChangeView(0, scollViewer.ScrollableHeight, 1);
            });
        }


    }
}
