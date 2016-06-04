using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Zeroconf;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace ZeroconfTest.RT.WP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        async void ResolveClick(object sender, RoutedEventArgs e)
        {

            var domains = await ZeroconfResolver.BrowseDomainsAsync();

            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            foreach (var resp in responses)
                WriteLogLine(resp.ToString());
        }

        private async void BrowseClick(object sender, RoutedEventArgs e)
        {
            var responses = await ZeroconfResolver.BrowseDomainsAsync();

            foreach (var service in responses)
            {
                WriteLogLine(service.Key);

                foreach (var host in service)
                    WriteLogLine("\tIP: " + host);

            }
        }

        private void OnAnnouncement(ServiceAnnouncement sa)
        {
            WriteLogLine("---- Announced on {0} ({1}) ----", sa.AdapterInformation.Name, sa.AdapterInformation.Address);
            WriteLogLine(sa.Host.ToString());
        }

        IDisposable listenSubscription;
        IObservable<ServiceAnnouncement> subscription;

        private void ListenClick(object sender, RoutedEventArgs e)
        {
            try
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
            finally { }
        }

        private async void WriteLogLine(string text, params object[] args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Log.Text += string.Format(text, args) + "\r\n";
                scollViewer.ChangeView(0, scollViewer.ScrollableHeight, 1);
            });
        }


    }
}
