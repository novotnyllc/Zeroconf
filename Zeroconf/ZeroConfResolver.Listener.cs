using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Zeroconf
{
    static partial class ZeroconfResolver
    {
		public static ResolverListener CreateListener(IEnumerable<string> protocols,
                                                         int queryInterval = 4000, 
                                                         int pingsUntilRemove = 2,
                                                         TimeSpan scanTime = default(TimeSpan),
                                                         int retries = 2,
                                                         int retryDelayMilliseconds = 2000)
		{
			return new ResolverListener(protocols, queryInterval, pingsUntilRemove, scanTime, retries, retryDelayMilliseconds);
		}

		public static ResolverListener CreateListener(string protocol,
                                                         int queryInterval = 4000,
                                                         int pingsUntilRemove = 2,
                                                         TimeSpan scanTime = default(TimeSpan),
                                                         int retries = 2,
                                                         int retryDelayMilliseconds = 2000)
		{
			return CreateListener(new[] { protocol }, queryInterval, pingsUntilRemove, scanTime, retries, retryDelayMilliseconds);
        }

		public class ResolverListener : IDisposable
		{
            private IEnumerable<string> protocols;
            private TimeSpan scanTime;
            private int retries;
            private int retryDelayMilliseconds;

#if (!NETSTANDARD1_0 && !WINDOWS_PHONE && !NETFX_CORE) || WINDOWS_UWP
            Timer timer;
#endif

            private int queryInterval;
            private int pingsUntilRemove;

            HashSet<Tuple<string, string>> discoveredHosts = new HashSet<Tuple<string, string>>();
            IDictionary<Tuple<string, string>, int> toRemove = new Dictionary<Tuple<string, string>, int>();
            IList<string> serviceTypes = new List<string>();

            internal ResolverListener(IEnumerable<string> protocols, int queryInterval, int pingsUntilRemove, TimeSpan scanTime, int retries, int retryDelayMilliseconds)
            {
                this.protocols = protocols;
                this.scanTime = scanTime;
                this.retries = retries;
                this.retryDelayMilliseconds = retryDelayMilliseconds;

                this.queryInterval = queryInterval;
                this.pingsUntilRemove = pingsUntilRemove;

#if (!NETSTANDARD1_0 && !WINDOWS_PHONE && !NETFX_CORE) || WINDOWS_UWP
                timer = new Timer(DiscoverHosts, this, 0, queryInterval);
#endif
            }

            public event EventHandler<IZeroconfHost> ServiceFound;

            public event EventHandler<IZeroconfHost> ServiceLost;

            public event EventHandler<Exception> Error;

            private async void DiscoverHosts(object state)
            {
                try
                {
                    var instance = state as ResolverListener;
                    var hosts = await ZeroconfResolver.ResolveAsync(protocols, scanTime, retries, retryDelayMilliseconds);
                    instance.OnResolved(hosts);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex);
                }
            }

            private void OnResolved(IReadOnlyList<IZeroconfHost> hosts)
            {
                lock (discoveredHosts)
                {
                    var newHosts = new HashSet<Tuple<string, string>>(discoveredHosts);
                    var remainingHosts = new HashSet<Tuple<string, string>>(discoveredHosts);

                    foreach (var host in hosts)
                    {
                        foreach (var service in host.Services)
                        {
                            var keyValue = new Tuple<string, string>(host.DisplayName, service.Key);
                            if (discoveredHosts.Contains(keyValue))
                            {
                                remainingHosts.Remove(keyValue);
                            }
                            else
                            {
                                ServiceFound?.Invoke(this, host);
                                newHosts.Add(keyValue);
                                if (toRemove.ContainsKey(keyValue)) toRemove.Remove(keyValue);
                            }
                        }
                    }

                    foreach (var service in remainingHosts)
                    {
                        if (toRemove.ContainsKey(service))
                        {
                            //zeroconf sometimes reports missing hosts incorrectly. 
                            //after pingsUntilRemove missing hosts reports, we'll remove the service from the list.
                            if (++toRemove[service] > pingsUntilRemove)
                            {
                                toRemove.Remove(service);
                                newHosts.Remove(service);
                                ServiceLost?.Invoke(this, new ZeroconfHost{ DisplayName = service.Item1, Id = service.Item2 });
                            }
                        }
                        else
                        {
                            toRemove.Add(service, 0);
                        }
                    }

                    discoveredHosts = newHosts;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
#if (!NETSTANDARD1_0 && !WINDOWS_PHONE && !NETFX_CORE) || WINDOWS_UWP
                    if (timer != null) timer.Dispose();
#endif
                }
            }
        }
    }
}
