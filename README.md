Zeroconf
==========

# Bonjour/mDNS discovery support for .NET 6, .NET 8, .NET Maui, .NET v4.8, UWP, , Xamarin & .NET Standard 2.0

The core logic is implemented primarily .NET Standard 2.0. Due to networking APIs being platform-specific on earlier platforms, a platform-specific version is required. Just make sure that you also install the NuGet to your main app and you'll be all set.

## Installation

The easiest way to get started is to use the NuGet package.

> Install-Package [Zeroconf](http://www.nuget.org/packages/Zeroconf)

Current Build Status: [![Build Status](https://dev.azure.com/clairernovotny/GitBuilds/_apis/build/status/Zeroconf%20-%20CI?branchName=master)](https://dev.azure.com/clairernovotny/GitBuilds/_build/latest?definitionId=37)

## Migration from <= v3.5.11 to >=v3.6

The key of the dictionary `IZeroconfHost.Services` of type `IReadOnlyDictionary<string, IService>` that is returned by e.g. `ResolveAsync` changed.
Instead of `IService.Name` which is the PTR record name, now the name from the SRV record is used which is called `ServiceName` in `IService`.
This allows to return more than one service of the same type from the same host. If you used the key of this dictonary (e.g. `s.Key`) for iteration now use `s.Value.Name`.

## Usage 

There's are two methods with a few optional parameters:

```csharp
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
```

The `ResolveAsync` method has one required and several optional parameters. 
The method signature is as follows:

```csharp
Task<IReadOnlyList<IZeroconfHost>> ResolveAsync(string protocol, TimeSpan scanTime = default(TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, Action<IZeroconfHost> callback = null, CancellationToken cancellationToken = default(CancellationToken), System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null);
```

The `BrowseDomainsAsync` method has the same set of optional parameters.
The method signature is:

```csharp
Task<ILookup<string, string>> BrowseDomainsAsync(TimeSpan scanTime = default (TimeSpan), int retryDelayMilliseconds = 2000, Action<string, string> callback = null, CancellationToken cancellationToken = default (CancellationToken), System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
```

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
| netInterfacesToSendRequestOn | null | Specify a list of network adapters to use. If null is specified, all adapters are used |


## Notes

The `ResolveAsync` method is thread-safe, however all calls to it are serialized as only
one can be in-progress at a time.

#### Xamarin.Android 4.x Linker bug
There is currently a [bug](https://bugzilla.xamarin.com/show_bug.cgi?id=21578) on Xamarin.Android 4.x that incorrectly strips out internal Socket methods. This has been [fixed](http://developer.xamarin.com/releases/android/xamarin.android_5/xamarin.android_5.0/) for the Xamarin.Android 5.0 series. As a workaround on 4.x, entering `System;` in to the `Ignore Assemblies` field in the `Project Options->Build->Android Build` page will fix the problem.

### Android
You must call the WifiManager.MulticastLock manager Aquire and Release before/after you call the Zeroconf methods.
Previous versions (prior to 2.7 did this internally, now it requires the caller to do it).

Something like this:

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

You'll also need to specify the correct permsision like this:
```csharp
[assembly: UsesPermission(Android.Manifest.Permission.ChangeWifiMulticastState)]
```

### UWP
You'll need to have the following permissions on your manifest depending on what networks you're trying to scan:
 - Private Networks (Client & Server)
 - Internet (Client & Server)

## Xamarin.iOS on iOS 14.5+

iOS 14.5 and later introduced new restrictions on mDNS clients. Low-level socket-based clients (like Zeroconf) are blocked at the iOS library/system call level
unless the program has a special com.apple.developer.networking.multicast entitlement.

If your app must browse arbitrary/unknown-at-compile-time services, you must obtain the com.apple.developer.networking.multicast entitlement from Apple.
If you **do** obtain the entitlement, somewhere early in your app set the property ZeroconfResolver.UseBSDSocketsZeroconfOniOS to true; this will cause the Zeroconf
library to behave as it always has and no workaround code will be executed.

For all other apps that know which mDNS services they must interact with: one way to work around this restriction is to use the iOS NSNetServiceBrowser
and NSNetService objects. A workaround using this method is now built in to Zeroconf; on iOS version 14.5 and greater, when ZeroconfResolver.ResolveAsync() or
ZeroconfResolver.BrowseAsync() functions are called, this workaround code is executed.

### Setup

This following actions are required for this workaround:

1. In your Xamarin.iOS project, modify Info.plist to include something like the following:

```
  <key>NSLocalNetworkUsageDescription</key>
  <string>Looking for local mDNS/Bonjour services</string>
  <key>NSBonjourServices</key>
  <array>
    <string>_audioplayer-discovery._tcp</string>
    <string>_http._tcp</string>
    <string>_printer._tcp</string>
    <string>_apple-mobdev2._tcp</string>
  </array>
```

The effect of the above: the first time your app runs, iOS will prompt the user for permission to allow mDNS queries from your app; it will display the
<string> value of the key NSLocalNetworkUsageDescription to the user.

For the key NSBonjourServices, its array of <string> values is the list of mDNS services that your app needs in order to run properly;
iOS will only allow mDNS information from those listed services to reach your app. Note that the domain (usually ".local.") is not specified in Info.plist.

Both the NSLocalNetworkUsageDescription and NSBonjourServices key values should be changed to what is required for your application.

2. Possible modification of BrowseAsync() calling code, if applicable

Calling BrowseAsync() followed by ResolveAsync() is essentially doing the same work twice: BrowseAsync is simulated using ResolveAsync() with the list of
NSBonjourServices from Info.plist. When you can modify the code, calling only ResolveAsync() only will provide you all the information you need in half the time.

The list of services from Info.plist are obtainable from ZeroconfResolver.GetiOSInfoPlistServices(), and a platform-independent way to know if the workaround
is enabled is the property ZeroconfResolver.IsiOSWorkaroundEnabled.

If you have to deal with custom mDNS domains, ZeroconfResolver.GetiOSDomains() will search the network for domains and return their names. The chosen domain
may then be used as a parameter to ZeroconfResolver.GetiOSInfoPlistServices(domain). See
[Apple's documentation](https://developer.apple.com/library/archive/documentation/Networking/Conceptual/NSNetServiceProgGuide/Articles/BrowsingForServices.html)

Example browse and resolve code:

```csharp
IReadOnlyList<IZeroconfHost> responses = null;

IReadOnlyList<string> domains;
if (ZeroconfResolver.IsiOSWorkaroundEnabled)
{
    // Demonstrates how using ZeroconfResolver.GetiOSInfoPlistServices() is much faster than ZeroconfResolver.BrowseDomainsAsync()
    //
    // In real life, you'd only query the domains if you were planning on presenting the user with a choice of domains to browse,
    //  or the app knows in advance there will be a choice and what the domain names would be
    //
    // This code assumes there will only be one domain returned ("local.") In general, if you don't have a requirement to handle domains,
    //  just call GetiOSInfoPlistServices() with zero arguments

    var iosDomains = await ZeroconfResolver.GetiOSDomains();
    string selectedDomain = (iosDomains.Count > 0) ? iosDomains[0] : null;

    domains = ZeroconfResolver.GetiOSInfoPlistServices(selectedDomain);
}
else
{
    var browseDomains = await ZeroconfResolver.BrowseDomainsAsync();
    domains = browseDomains.Select(g => g.Key).ToList();
}

responses = await ZeroconfResolver.ResolveAsync(domains);
```

### Unimplemented features in the workaround

ListenForAnnouncementsAsync()

ResolverListener()

### Implementation Details

The callback functions are based on a simple-minded implementation: they will be called only after each ScanTime interval has expired for each distinct
protocol/mDNS service.

The more protocols/mDNS services you resolve, the longer it takes the library to return: minimumTotalDelayTime = (nServices * ScanTime).

### Known bugs

There is no propagation of errors (NetService_ResolveFailure, Browser_NotSearched) from the iOS API to ZeroconfSwitch. If any of these errors occur,
you simply get nothing and like it.

## Credits

This library was made possible through the efforts of the following projects:

* [ZeroconfRT](https://github.com/saldoukhov/ZeroconfRT) by Sergey Aldoukhov
* [DNS.NET Resolver](http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C) by Alphons van der Heijden
