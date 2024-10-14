
using Xunit;

namespace Zeroconf.Maui.TestRunner.Tests
{

    /// <summary>
    /// Tests the network interface works
    /// </summary>
    public class Test_Network
    {

        /// <summary>
        /// Test we can open the google home page
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Test_HttpResponse()
        {
            try
            {
                // Test basic network connectivity
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://google.com");
                    Assert.True(response.IsSuccessStatusCode, "Basic network test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex}");
                throw;
            }
        }

    }
}
