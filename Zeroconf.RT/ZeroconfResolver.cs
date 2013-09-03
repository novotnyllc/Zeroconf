using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Heijden.DNS;
using DnsType = Heijden.DNS.Type;

namespace Zeroconf
{
    /// <summary>
    /// Looks for ZeroConf devices
    /// </summary>
    public static class ZeroconfResolver
    {
        private static readonly AsyncLock ResolverLock = new AsyncLock();
        /// <summary>
        /// Resolves available ZeroConf services
        /// </summary>
        /// <param name="scanTime">Default is 2 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <param name="protocol"></param>
        /// <param name="retries">If the socket is busy, the number of times the resolver should retry</param>
        /// <param name="retryDelayMilliseconds">The delay time between retries</param>
        /// <param name="callback">Called per record returned as they come in.</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IZeroconfRecord>> ResolveAsync(string protocol, TimeSpan scanTime = default (TimeSpan), int retries = 2, int retryDelayMilliseconds = 2000, Action<IZeroconfRecord> callback = null, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(protocol))
                throw new ArgumentNullException("protocol");

            if (scanTime == default(TimeSpan))
                scanTime = TimeSpan.FromSeconds(2);

            using (await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine("Looking for {0} with scantime {1}", protocol, scanTime);

                using (var socket = new DatagramSocket())
                {
                    var list = new List<ZeroconfRecord>();

                    // setup delegate to detach from later
                    TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> handler = (sock, args) =>
                    {
                        var dr = args.GetDataReader();
                        var byteCount = dr.UnconsumedBufferLength;
                        var resp = new Response(dr.ReadBuffer(dr.UnconsumedBufferLength).ToArray());

                        Debug.WriteLine("IP: {0}, Bytes: {1}, IsResponse: {2}", args.RemoteAddress.DisplayName, byteCount, resp.header.QR);

                        if (resp.header.QR)
                        {
                            var item = ResponseToZeroconf(resp);

                            lock (list)
                            {
                                list.Add(item);
                            }
                            
                            if (callback != null)
                                callback(item);
                        }
                    };

                    socket.MessageReceived += handler;
                    var socketBound = false;

                    for (var i = 0; i < retries; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            await BindToSocketAndWriteQuery(socket, protocol, cancellationToken).ConfigureAwait(false);
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

                    return list;
                }
            }
        }

        private static byte[] GetRequestBytes(string protocol)
        {
            var req = new Request();

            var question = new Question(protocol, QType.ANY, QClass.ANY);

            req.AddQuestion(question);

            return req.Data;
        }

        private async static Task BindToSocketAndWriteQuery(DatagramSocket socket, string protocol, CancellationToken cancellationToken)
        {
            await socket.BindServiceNameAsync("5353").AsTask(cancellationToken).ConfigureAwait(false);
            socket.JoinMulticastGroup(new HostName("224.0.0.251"));
            var os = await socket.GetOutputStreamAsync(new HostName("224.0.0.251"), "5353").AsTask(cancellationToken).ConfigureAwait(false);
            using (var writer = new DataWriter(os))
            {
                var bytes = GetRequestBytes(protocol);
                writer.WriteBytes(bytes);

                await writer.StoreAsync().AsTask(cancellationToken).ConfigureAwait(false);
                Debug.WriteLine("Sent mDNS query");

                writer.DetachStream();
            }
        }

        private static ZeroconfRecord ResponseToZeroconf(Response response)
        {
            // records by type
            var records = response.RecordsRR.ToLookup(record => record.Type);

            
            var z = new ZeroconfRecord();

            if (records.Contains(DnsType.PTR))
            {
                var ptr = (RecordPTR)records[DnsType.PTR].First().RECORD;
                z.Name = ptr.PTRDNAME.Split('.')[0];
            }

            if (records.Contains(DnsType.A))
            {
                var rr = records[DnsType.A].First();
                z.Host = rr.NAME.Split('.')[0];
                z.IPAddress = ((RecordA)rr.RECORD).Address;
            }

            if (records.Contains(DnsType.SRV))
            {
                var srv = (RecordSRV)records[DnsType.SRV].First().RECORD;
                z.Port = srv.PORT;
            }

            if (records.Contains(DnsType.TXT))
            {
                foreach (var rr in records[DnsType.TXT])
                {
                    var txtRecord = (RecordTXT)rr.RECORD;
                    foreach (var txt in txtRecord.TXT)
                    {
                        var split = txt.Split(new[] { '=' }, 2);
                        if (split.Length == 1)
                        {
                            z.AddProperty(split[0], null);
                        }
                        else
                        {
                            z.AddProperty(split[0], split[1]);
                        }
                    }
                }
            }

            return z;
        }
    }
}
