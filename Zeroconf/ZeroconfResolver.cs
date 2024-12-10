using System.Diagnostics;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Heijden.DNS;

namespace Zeroconf
{
    /// <summary>
    ///     Looks for ZeroConf devices
    /// </summary>
    public static partial class ZeroconfResolver
    {
        static readonly AsyncLock ResolverLock = new AsyncLock();

        static readonly INetworkInterface NetworkInterface = new NetworkInterface();

        static IEnumerable<string> BrowseResponseParser(Response response)
        {
            return response.RecordsPTR.Select(ptr => ptr.PTRDNAME.TrimEnd('.'));
        }

        static async Task<IDictionary<string, Response>> ResolveInternal(ZeroconfOptions options,
                                                                         Action<string, Response> callback,
                                                                         CancellationToken cancellationToken,
                                                                         System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            var requestBytes = GetRequestBytes(options);
            using (options.AllowOverlappedQueries ? Disposable.Empty : await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, Response>();

                void Converter(IPAddress address, byte[] buffer)
                {
                    var resp = new Response(buffer);
                    var firstPtr = resp.RecordsPTR.FirstOrDefault();
                    var name = firstPtr?.PTRDNAME.Split('.')[0] ?? string.Empty;
                    var addrString = address.ToString();

                    Debug.WriteLine($"IP: {addrString}, {(string.IsNullOrEmpty(name) ? string.Empty : $"Name: {name}, ")}Bytes: {buffer.Length}, IsResponse: {resp.header.QR}");

                    if (resp.header.QR)
                    {
                        var key = $"{addrString}{(string.IsNullOrEmpty(name) ? "" : $": {name}")}";
                        lock (dict)
                        {
                            dict[key] = resp;
                        }

                        callback?.Invoke(key, resp);
                    }
                }

                Debug.WriteLine($"Looking for {string.Join(", ", options.Protocols)} with scantime {options.ScanTime}");

                await NetworkInterface.NetworkRequestAsync(requestBytes,
                                                           options.ScanTime,
                                                           options.Retries,
                                                           (int)options.RetryDelay.TotalMilliseconds,
                                                           Converter,
                                                           cancellationToken,
                                                           netInterfacesToSendRequestOn)
                                      .ConfigureAwait(false);

                return dict;
            }
        }

        static byte[] GetRequestBytes(ZeroconfOptions options)
        {
            var req = new Request();
            var queryType = options.ScanQueryType == ScanQueryType.Ptr ? QType.PTR : QType.ANY;

            foreach (var protocol in options.Protocols)
            {
                var question = new Question($"{protocol}.", queryType, QClass.IN);

                req.AddQuestion(question);
            }

            return req.Data;
        }

        static ZeroconfHost ResponseToZeroconf(Response response, string remoteAddress, ResolveOptions options)
        {




            var host = new ZeroconfHost();
            var services = new Dictionary<string, Service>(StringComparer.OrdinalIgnoreCase);

            // 1. Process PTR records to discover service instances
            foreach (var answer in response.Answers)
            {
                if (answer.RECORD is RecordPTR ptr)
                {
                    // The PTR record gives us a full service instance name
                    var fullName = ptr.PTRDNAME.TrimEnd('.');
                    var labels = fullName.Split('.');
                    if (labels.Length < 3)
                        continue; // Not a valid service name

                    // The first label is the instance name
                    var svc = new Service
                    {
                        Name = labels[0],
                        ServiceName = fullName
                    };
                    services[fullName] = svc;
                }
            }
            // 2. Process SRV and TXT records to fill in details for each discovered service
            // Note: DNS-SD records for a given service may appear in both Answers and Additionals.
            var allRecords = response.Answers.Cast<RR>().Concat(response.Additionals.Cast<RR>()).ToList();

            foreach (var record in allRecords)
            {
                if (record.RECORD is RecordSRV srv)
                {
                    var fullName = record.NAME.TrimEnd('.');
                    if (!services.TryGetValue(fullName, out var svc))
                        continue; // No PTR record for this service

                    svc.Port = srv.PORT;
                    svc.Ttl = (int)srv.RR.TTL;
                    svc.Priority = srv.PRIORITY;
                    svc.Weight = srv.WEIGHT;

                    // Set the host's id if it hasn't already been set
                    if (string.IsNullOrEmpty(host.Id))
                    {
                        host.Id = srv.TARGET.TrimEnd('.');
                        host.DisplayName = srv.RR.NAME.Split('.')[0];
                    }
                }
                else if (record.RECORD is RecordTXT txt)
                {
                    var fullName = record.NAME.TrimEnd('.');
                    if (!services.TryGetValue(fullName, out var svc))
                        continue; // No PTR record for this service
                    var set = new Dictionary<string, string>();
                    foreach (var txtRecord in txt.TXT)
                    {
                        var split = txtRecord.Split(new[] { '=' }, 2);
                        if (split.Length == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(split[0]))
                                set[split[0]] = null;
                        }
                        else
                        {
                            set[split[0]] = split[1];
                        }
                    }
                    svc.AddPropertySet(set);
                }
            }
            // 3. Process A and AAAA records to resolve IP addresses of the host
            // The host's Id should have been set from an SRV record. Use that to match A/AAAA records.

            var ipAddresses = new List<string>();
            host.IPAddresses = ipAddresses;

            foreach (var record in allRecords)
            {
                if (!string.IsNullOrEmpty(host.Id))
                {
                    if (record.RECORD is RecordA a && record.NAME.TrimEnd('.') == host.Id)
                    {
                        ipAddresses.Add(a.Address.ToString());
                    }
                    else if (record.RECORD is RecordAAAA aaaa && record.NAME.TrimEnd('.') == host.Id)
                    {
                        ipAddresses.Add(aaaa.Address.ToString());
                    }
                }
            }

            // 4. Add all discovered services to the host
            foreach (var service in services.Values)
            {
                host.AddService(service);
            }

            return host;
        }


    }
}
