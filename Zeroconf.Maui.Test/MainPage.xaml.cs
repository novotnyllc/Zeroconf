
using System.Diagnostics;


namespace Zeroconf.Maui.Test
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Very basic test to look for printers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Test1_Btn_Clicked(object sender, EventArgs e)
        {

            // Look for all T1 devices
            Debug.WriteLine("Starting mDNS scan...");

            // create a list of the services we are looking for
            var aServices = new List<string> { "_printer._tcp.local." };
            var aRes = await ZeroconfResolver.ResolveAsync(aServices);
            Debug.WriteLine($"mDNS scan complete. Found {aRes.Count()} devices.");
            foreach (IZeroconfHost aResponse in aRes)
            {
                // Debug the response
                Debug.WriteLine($"Response: {aResponse}");
            }

        }
    }

}
