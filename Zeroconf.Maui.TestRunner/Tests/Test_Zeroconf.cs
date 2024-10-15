
#nullable enable

using System.Diagnostics;

using Xunit;


namespace Zeroconf.Maui.TestRunner.Tests
{

    /// <summary>
    /// Tests Zeroconf
    /// </summary>
    public class Test_Zeroconf
    {

        /// <summary>
        /// The services we will be looking for
        /// </summary>
        List<string> mDNS_Services = new()
        {
            "_printer._tcp.local.",
        };


        /// <summary>
        /// Our Expected devices
        /// </summary>
        List<mDNS_Device> mDNS_Devices = new()
        {
            new mDNS_Device { DisplayName = "Kyocera ECOSYS M6230cidn (2)", ID = "10.0.0.111" }
        };



        /// <summary>
        /// Defines an expected mDNS device
        /// </summary>
        class mDNS_Device
        {
            public required string DisplayName { get; set; }
            public required string ID { get; set; }
        }


        /// <summary>
        /// Tests the ResolveAsync method.
        /// Resolves a list of services and checks the results against a list of expected devices.
        /// Fails if any expected devices are not found or if any unexpected devices are found.
        /// </summary>
        [Fact]
        public async void Test_ResolveAsync_1()
        {

            // Invoke the test on the main thread to ensure success (especially for iOS)
            await RunTest_OnMainThread(async () =>
            {
                // Test parameters
                var aExpected_Services = this.mDNS_Services;
                var aExpected_Devices = this.mDNS_Devices;

                // Ask Zeroconf to resolve the services
                var aRes = await ZeroconfResolver.ResolveAsync(aExpected_Services);
                Debug.WriteLine($"mDNS scan complete. Found {aRes.Count()} devices.");

                // Check the results match the expected devices
                foreach (IZeroconfHost aResponse in aRes)
                {
                    Debug.WriteLine($"Found: {aResponse}");

                    // Check if the response is in the expected devices
                    mDNS_Device? aDevice = aExpected_Devices
                        .Where(x => x.DisplayName == aResponse.DisplayName
                                    && x.ID == aResponse.Id)
                        .FirstOrDefault();

                    // If the device is not in the expected devices then fail the test
                    if (aDevice == null)
                        Assert.True(false, $"Error: '{aResponse.DisplayName}' Device not found in expected devices!");
                }

                // Make sure all the expected devices were found
                if (aExpected_Devices.Count != aRes.Count())
                    Assert.True(false, "Error: Not all expected devices were found!");
            });

        }


        /// <summary>
        /// Tests the IsiOSWorkaroundEnabled method.
        /// </summary>
        [Fact]
        public void Test_iOS_IsiOSWorkaroundEnabled()
        {

            // Check if the iOS workaround is enabled
            bool iOSWorkaround = ZeroconfResolver.IsiOSWorkaroundEnabled;
            Debug.WriteLine($"ZeroConf iOS Workaround enabled={iOSWorkaround}");

            // Check the result
            Assert.True(iOSWorkaround, "iOS Workaround is not enabled.");

        }


        /// <summary>
        /// Tests the GetiOSInfoPlistServices method.
        /// Valid only on iOS
        /// </summary>
        [Fact]
        public void Test_iOS_GetInfoPlistServices()
        {

            // Test parameters
            var aExcectedServices = this.mDNS_Services;

            // Get a list of iOS services in the info.plist file
            var aAllowedServices = ZeroconfResolver.GetiOSInfoPlistServices();
            Debug.WriteLine($"Found {aAllowedServices.Count} services in iOS Info.plist");

            // Check the results match the expected services
            foreach (string aService in aAllowedServices)
            {
                Debug.WriteLine($"Found: {aService}");

                // Check if the service is in the expected services
                if (!aExcectedServices.Contains($"{aService}.local."))
                    Assert.True(false, $"Error: '{aService}' Service not found in expected services!");
            }

            // Make sure all the expected services were found
            if (aExcectedServices.Count != aAllowedServices.Count)
                Assert.True(false, "Error: Not all expected services were found!");

        }


        /// <summary>
        /// Runs a test asynchronously on the main thread.
        /// </summary>
        /// <param name="testFunc"></param>
        /// <returns></returns>
        private Task RunTest_OnMainThread(Func<Task> testFunc)
        {

            // use this to track test completion
            var aTcs = new TaskCompletionSource<bool>();

            // Use the .NET Maui main thread to run the test
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Run the test function
                    await testFunc();

                    // All done
                    aTcs.SetResult(true);
                }
                catch (Exception x)
                {
                    // We ran into a problem so pass this back to the calling thread
                    aTcs.SetException(x);
                }
            });

            // Wait for the test to finish running on the main thread
            return aTcs.Task;

        }

    }
}
