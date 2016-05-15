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

        async void ResolveClick(object sender, RoutedEventArgs e)
        {

            try
            {
                var domains = await ZeroconfResolver.BrowseDomainsAsync();
                // var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

                // var responses = await ZeroconfResolver.ResolveAsync("_http._tcp.local.");


                //foreach (var resp in responses)
                //    Debug.WriteLine(resp);


                var sub = ZeroconfResolver.Resolve(domains.Select(g => g.Key));
                IDisposable disp = null;
                disp = sub.Subscribe(h => Debug.WriteLine(h), () =>
                                                                  {
                                                                      Debug.WriteLine("Completed");
                                                                      disp.Dispose(); ;
                                                                  });
            }
            catch (Exception)
            {
                Debug.WriteLine("Exception was thrown... most likely the port is already in use, unfortunatly WinRT does not allow re-use of ports.");
            }   
        }
    }
}
