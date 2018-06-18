using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Zeroconf
{
    interface INetworkInterface
    {
        Task NetworkRequestAsync(byte[] requestBytes,
                                 TimeSpan scanTime,
                                 int retries,
                                 int retryDelayMilliseconds,
                                 Action<IPAddress, byte[]> onResponse,
                                 CancellationToken cancellationToken);

        Task ListenForAnnouncementsAsync(Action<AdapterInformation, string, byte[]> callback, CancellationToken cancellationToken);
    }
}
