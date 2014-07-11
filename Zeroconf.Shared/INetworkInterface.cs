using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeroconf
{
    internal interface INetworkInterface
    {
        Task NetworkRequestAsync(byte[] requestBytes,
                                 TimeSpan scanTime,
                                 int retries,
                                 int retryDelayMilliseconds,
                                 Action<string, byte[]> onResponse,
                                 CancellationToken cancellationToken);
    }
}