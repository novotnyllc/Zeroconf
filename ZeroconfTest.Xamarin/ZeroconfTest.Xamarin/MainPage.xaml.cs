using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Zeroconf;

namespace ZeroconfTest.Xam
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        void Browse_Pressed(object sender, EventArgs e)
        {
            _ = BrowseDomainsAsync();
        }

        void Resolve_Pressed(object sender, EventArgs e)
        {
            var protocol = Protocol.Text ?? string.Empty;
            IReadOnlyList<string> protocols = protocol.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            _ = ResolveProtocol(protocols);
        }

        void Clear_Pressed(object sender, EventArgs e)
        {
            Output.Text = string.Empty;
        }

        private async Task BrowseDomainsAsync()
        {
            ILookup<string, string> responses = null;

            responses = await ZeroconfResolver.BrowseDomainsAsync();

            Output.Text += Environment.NewLine;
            foreach (var service in responses)
            {
                Output.Text += $"{service.Key}{Environment.NewLine}";

                foreach (var host in service)
                {
                    Output.Text += $"\tIP: {host}{Environment.NewLine}";
                }
            }
        }

        private async Task ResolveProtocol(IReadOnlyList<string> domains)
        {
            IReadOnlyList<IZeroconfHost> responses = null;

            if (!domains.Any())
            {
                if (ZeroconfResolver.IsiOSWorkaroundEnabled)
                {
                    // Xamarin.iOS only, running on iOS 14.5+
                    //
                    // Demonstrates how using ZeroconfResolver.GetiOSInfoPlistServices() is much faster than ZeroconfResolver.BrowseDomainsAsync()
                    //
                    // In real life, you'd only query the domains if you were planning on presenting the user with a choice of domains to browse,
                    //  or the app knows in advance there will be a choice and what the domain names would be
                    //
                    // This code assumes there will only be one domain returned ("local.") In general, if you don't have a requirement to handle domains,
                    //  just call GetiOSInfoPlistServices() with zero arguments

                    var iosDomains = await ZeroconfResolver.GetiOSDomains();
                    var selectedDomain = iosDomains.FirstOrDefault();
                    domains = ZeroconfResolver.GetiOSInfoPlistServices(selectedDomain);
                }
                else
                {
                    var browseDomains = await ZeroconfResolver.BrowseDomainsAsync();
                    domains = browseDomains.Select(g => g.Key).ToList();
                }
            }

            responses = await ZeroconfResolver.ResolveAsync(domains);

            Output.Text += Environment.NewLine;

            foreach (var resp in responses)
            {
                Output.Text += $"{resp}{Environment.NewLine}";
            }
        }

    }
}

