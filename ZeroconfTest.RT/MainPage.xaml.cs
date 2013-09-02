using System;
using System.Diagnostics;
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
            var responses = await ZeroconfResolver.ResolveAsync("_pdl-datastream._tcp.local.", TimeSpan.FromSeconds(5));

            foreach (var resp in responses)
                Debug.WriteLine(resp);
        }
    }
}
