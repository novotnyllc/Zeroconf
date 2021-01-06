using System;
using System.Collections.Generic;
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
                                 Action<IPAddress, byte[], System.Net.NetworkInformation.NetworkInterface> onResponse,
                                 CancellationToken cancellationToken,
                                 IEnumerable<System.Net.NetworkInformation.NetworkInterface> netInterfacesToSendRequestOn);

        Task ListenForAnnouncementsAsync(Action<AdapterInformation, string, byte[]> callback, CancellationToken cancellationToken);
    }
}
