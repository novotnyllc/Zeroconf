#define DEBUG

using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Zeroconf;

namespace ZeroconfTest.RT
{
    public sealed partial class MainPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ResolveClick(object sender, RoutedEventArgs e)
        {
            
            var domains = await ZeroconfResolver.BrowseDomainsAsync();
            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            // var responses = await ZeroconfResolver.ResolveAsync("_http._tcp.local.");


            foreach (var resp in responses)
                Debug.WriteLine(resp);
        }
    }
}
