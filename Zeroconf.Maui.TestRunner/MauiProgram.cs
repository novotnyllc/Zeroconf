using Microsoft.Extensions.Logging;
using Xunit.Runners.Maui;

namespace Zeroconf.Maui.TestRunner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp() => MauiApp
            .CreateBuilder()
            .ConfigureTests(new TestOptions
            {
                Assemblies =
                {
            typeof(MauiProgram).Assembly
                }
            })
            .UseVisualRunner()
            .Build();
    }
}
