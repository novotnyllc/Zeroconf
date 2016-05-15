Zeroconf
==========

# Bonjour/mDNS support for .NET 4.5, Windows Phone 8, Windows Store apps and Portable Class Libraries

The core logic is implemented as a PCL, but due to networking APIs being 
platform-specific, a platform-specific helper library is required. Just make
sure that you also install the NuGet to your main app and you'll be all set.

## Installation

The easiest way to get started is to use the NuGet package.

> Install-Package [Zeroconf](http://www.nuget.org/packages/Zeroconf)

Current Build Status:
[![Build status](https://ci.appveyor.com/api/projects/status/52nr1dgg9ftrxeh9/branch/master?svg=true)](https://ci.appveyor.com/project/onovotny/zeroconf/branch/master)

## Usage

There's are two methods with a few optional parameters:

    using Zeroconf;
    public async Task ProbeForNetworkPrinters()
    {
        IReadOnlyList<IZeroconfHost> results = await
            ZeroconfResolver.ResolveAsync("_printer._tcp.local.");
    }

    public async Task EnumerateAllServicesFromAllHosts()
    {
        ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();            
        var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));            
        foreach (var resp in responses)
            Console.WriteLine(resp);
    }

The `ResolveAsync` method has one required and several optional parameters. 
The method signature is as follows:

    Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(string protocol, TimeSpan scanTime = default(TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, Action<IZeroconfHost> callback = null, CancellationToken cancellationToken = default(CancellationToken));

The `BrowseDomainsAsync` method has the same set of optional parameters.
The method signature is:
   
    Task<ILookup<string, string>> BrowseDomainsAsync(TimeSpan scanTime = default (TimeSpan), int retryDelayMilliseconds = 2000, Action<string, string> callback = null, CancellationToken cancellationToken = default (CancellationToken))

What you get back from the Browse is a lookup, by service name, of a group that contains every host
offering that service. Thst most common use would be in the example above, passing in
all keys (services) to the Resolve method. Otherwise, you can also see what hosts are
offering which services as well.

### IObservable

Starting in v2.5, there are two additional methods that return `IObservable`'s instead of Tasks. These methods
are otherwise identical to the `*Async` versions but are more suitable for some usages. 

### Parameters

| Parameter Name | Default Value | Notes |
| -------------- | ------------- | ----- |
| protocol | | Service to query. Almost always must end with *.local.* |
| scanTime | 2 seconds | Amount of time to listen for responses |
| retries | 2 | Number of times to attempt to bind to the socket. Binding may fail if another app is currently using it. |
| retryDelayMilliseconds | 2000 | Delay between retries |
| callback | null | If provided, called per `IZeroconfigHost` as they are processed. This can be used to stream data back prior to call completion. |
| cancellationToken | CancellationToken.None | Optional use of task cancellation |


## Notes

The `ResolveAsync` method is thread-safe, however all calls to it are serialized as only
one can be in-progress at a time.

#### Xamarin.Android 4.x Linker bug
There is currently a [bug](https://bugzilla.xamarin.com/show_bug.cgi?id=21578) on Xamarin.Android 4.x that incorrectly strips out internal Socket methods. This has been [fixed](http://developer.xamarin.com/releases/android/xamarin.android_5/xamarin.android_5.0/) for the Xamarin.Android 5.0 series. As a workaround on 4.x, entering `System;` in to the `Ignore Assemblies` field in the `Project Options->Build->Android Build` page will fix the problem.

### Android
You must call the WifiManager.MulticastLock manager Aquire and Release before/after you call the Zeroconf methods.
Previous versions (prior to 2.7 did this internally, now it requires the caller to do it).

Something like thisl
```csharp

// Somewhere early
var wifi = (WifiManager)ApplicationContext.GetSystemService(Context.WifiService);
var mlock = wifi.CreateMulticastLock("Zeroconf lock");

---
// Later, before you call Zeroconf
try
{
  mlock.Acquire();

  // Call Zeroconf
  ZeroconfResolver....
}
finally
{
  mlock.Release();
}
```


## Credits

This library was made possible through the efforts of the following projects:

* [ZeroconfRT](https://github.com/saldoukhov/ZeroconfRT) by Sergey Aldoukhov
* [DNS.NET Resolver](http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C) by Alphons van der Heijden
