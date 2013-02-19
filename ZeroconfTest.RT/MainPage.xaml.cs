using System;
using System.Reactive.Linq;
using System.Diagnostics;
using Windows.UI.Xaml;
using Zeroconf;

namespace ZeroconfTest.RT
{
    public sealed partial class MainPage
    {
        private IDisposable _d;

        public MainPage()
        {
            InitializeComponent();
        }

        private void ResolveClick(object sender, RoutedEventArgs e)
        {
            //ZeroconfResolver.Resolve("_airplay._tcp.local.").Subscribe();
            if (_d != null)
                _d.Dispose();
            _d = ZeroconfResolver
                .Resolve("_p2pchat._udp.local.")
                .Timeout(TimeSpan.FromSeconds(5))
                .Subscribe(x => Debug.WriteLine(x));
        }
    }
}
