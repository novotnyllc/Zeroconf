using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Zeroconf
{

    internal class NetworkInterface : INetworkInterface
    {
        public async Task NetworkRequestAsync(byte[] requestBytes,
                                              TimeSpan scanTime,
                                              int retries,
                                              int retryDelayMilliseconds,
                                              Action<string, byte[]> onResponse,
                                              CancellationToken cancellationToken)
        {
            using (var socket = new DatagramSocket())
            {
                // setup delegate to detach from later
                TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> handler =
                    (sock, args) =>
                    {
                        var dr = args.GetDataReader();
                        var buffer = dr.ReadBuffer(dr.UnconsumedBufferLength).ToArray();

                        onResponse(args.RemoteAddress.CanonicalName.ToString(), buffer);
                    };

                socket.MessageReceived += handler;
                var socketBound = false;

                for (var i = 0; i < retries; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await BindToSocketAndWriteQuery(socket,
                                                        requestBytes,
                                                        cancellationToken).ConfigureAwait(false);
                        socketBound = true;
                    }
                    catch (Exception e)
                    {
                        socketBound = false;
                        Debug.WriteLine("Exception trying to Bind:\n{0}", e);

                        // Most likely a fatal error
                        if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.Unknown)
                            throw;

                        // If we're not connected on the last retry, rethrow the underlying exception
                        if (i + 1 >= retries)
                            throw;
                    }

                    if (socketBound)
                        break;

                    Debug.WriteLine("Retrying in {0} ms", retryDelayMilliseconds);
                    // Not found, wait to try again
                    await Task.Delay(retryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }

                if (socketBound)
                {
                    // wait for responses
                    await Task.Delay(scanTime, cancellationToken).ConfigureAwait(false);
                    Debug.WriteLine("Done Scanning");
                }
            }
        }

        private static async Task BindToSocketAndWriteQuery(DatagramSocket socket, byte[] bytes, CancellationToken cancellationToken)
        {
#if !WINDOWS_PHONE
            try
            {
               // Try to bind using port 5353 first
               await socket.BindServiceNameAsync("5353", NetworkInformation.GetInternetConnectionProfile().NetworkAdapter)
                           .AsTask(cancellationToken)
                           .ConfigureAwait(false);

            }
            catch (Exception)
            {
                // If it fails, use the default
                await socket.BindServiceNameAsync("", NetworkInformation.GetInternetConnectionProfile().NetworkAdapter)
                            .AsTask(cancellationToken)
                            .ConfigureAwait(false);

            }
#else
            await socket.BindServiceNameAsync("5353")
                        .AsTask(cancellationToken)
                        .ConfigureAwait(false);

#endif
                        

            socket.JoinMulticastGroup(new HostName("224.0.0.251"));
            var os = await socket.GetOutputStreamAsync(new HostName("224.0.0.251"), "5353")
                                 .AsTask(cancellationToken)
                                 .ConfigureAwait(false);

            using (var writer = new DataWriter(os))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync()
                            .AsTask(cancellationToken)
                            .ConfigureAwait(false);

                Debug.WriteLine("Sent mDNS query");

                writer.DetachStream();
            }
        }
    }
}
