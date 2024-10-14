using System.Diagnostics;

namespace Zeroconf.Maui.Test
{
    public partial class MainPage : ContentPage
    {

        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void Test1_Btn_Clicked(object sender, EventArgs e)
        {

            Debug.WriteLine($"MAUI Button Press Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            Debug.WriteLine($"MAUI Button Press Thread Name: {Thread.CurrentThread.Name}");
            Debug.WriteLine($"MAUI Button Press Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
            Debug.WriteLine($"MAUI Button Press Synchronization Context: {SynchronizationContext.Current?.GetType().Name ?? "null"}");


            // Look for all T1 devices
            Debug.WriteLine("Starting mDNS scan...");

            //// Get a list of iOS domains
            //var aIosDomains = await ZeroconfResolver.GetiOSDomains();
            //string aSelectedDomain = (aIosDomains.Count > 0) ? aIosDomains[0] : null;
            //Debug.WriteLine($"Found {aIosDomains.Count} domains. Using domain: {aSelectedDomain}");

            //// Get a list of iOS services
            //IReadOnlyList<string> aAllowedServices;
            //aAllowedServices = ZeroconfResolver.GetiOSInfoPlistServices();
            //Debug.WriteLine($"Found {aAllowedServices.Count} services in iOS Info.plist");

            //// Check if the iOS workaround is enabled
            //bool iOSWorkaround = ZeroconfResolver.IsiOSWorkaroundEnabled;
            //Debug.WriteLine($"ZeroConf iOS Workaround enabled={iOSWorkaround}");

            // create a list of the services we are looking for
            var aServices = new List<string> { "_printer._tcp.local." };
            //var aServices = new List<string> { "_fcmd._udp.local.", "_printer._tcp.local." };
            var aRes = await ZeroconfResolver.ResolveAsync(aServices);
            Debug.WriteLine($"mDNS scan complete. Found {aRes.Count()} devices.");
            foreach (IZeroconfHost aResponse in aRes)
            {
                // Create a T1 Device object from the response
                Debug.WriteLine($"Response: {aResponse}");

                // Get the FCmd service from the response. If there is no FCmd service, skip this response
                Zeroconf.IService? aService = aResponse.Services
                    .Where(x => aServices.Contains(x.Value.Name))
                    .FirstOrDefault().Value;
                if (aService == null)
                {
                    Debug.WriteLine("Error: No FCmd service found in mDNS response!");
                    continue;
                }
            }

        }
    }

}
