
using System.Diagnostics;
using Xunit;


namespace Zeroconf.Maui.TestRunner.Tests
{

    /// <summary>
    /// So we know that the test runner is working
    /// </summary>
    public class Test_xUnit
    {

        [Fact]
        public void Test_HelloWorld()
        {
            Debug.WriteLine("Hello World");
            Assert.True(true);
        }

    }
}
