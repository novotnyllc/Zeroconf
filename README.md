Zeroconf
==========

Bonjour/mDNS support for .NET 4.5, Windows Phone 8 and Windows Store apps
-
Due to networking APIs being platform-specific, Zeroconf is implemented 
a platform-specific library for each of the supported platforms. That said, 
The implementation between Win8 and WP8 are identical. The external API is
same for all platforms.

Installation
-
The easiest way to get started is to use the NuGet package.

Install-Package [Zeroconf](http://www.nuget.org/packages/Zeroconf)

Usage
-
There's a single method with a few optional parameters:

    using Zeroconf;
    public async Task ProbeForNetworkPrinters()
    {
        IReadOnlyList<IZeroconfRecord> results = await
            ZeroconfResolver.ResolveAsync("_printer._tcp.local.");
    }

The _ResolveAsync_ method has one required and several optional parameters. 
The method signature is as follows:
    
	Task<IReadOnlyList<IZeroconfRecord>> ResolveAsync(string protocol, TimeSpan scanTime = default(TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, CancellationToken cancellationToken = default(CancellationToken));


### Parameters

<table>
	<tr>
		<th>Parameter Name</th>
		<th>Default Value</th>
		<th>Notes</th>
	</tr>
	<tr>
		<td>protocol</td>
		<td></td>
		<td>Service to query. Almost always must end with <em>.local.</em></td>
	</tr>
	<tr>
		<td>scanTime<td>
		<td>2 seconds</td>
		<td>Amount of time to listen for responses</td>
	</tr>
	<tr>
		<td>retries</td>
		<td>2</td>
		<td>Number of times to attempt to bind to the socket. Binding may fail if 
		another app is currently using it.</td>
	</tr>
	<tr>
		<td>retryDelayMilliseconds</td>
		<td>2000</td>
		<td>Delay between retries</td>
	</tr>
	<tr>
		<td>cancellationToken</td>
		<td>CancellationToken.None</td>
		<td>Optional use of task cancellation</td>
	</tr>
</table>

### Notes

The _ResolveAsync_ method is thread-safe, however all calls to it are serialized as only
one can be in-progress at a time.

### Credits

This library was made possible through the efforts of the following projects:

* [ZeroconfRT](https://github.com/saldoukhov/ZeroconfRT) by Sergey Aldoukhov
* [DNS.NET Resolver](http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C) by Alphons van der Heijden