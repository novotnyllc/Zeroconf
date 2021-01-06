﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Heijden.DNS;
using Type = Heijden.DNS.Type;

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
            return response.RecordsPTR.Select(ptr => ptr.PTRDNAME);
        }

        static async Task<IDictionary<string, ValueTuple<Response, System.Net.NetworkInformation.NetworkInterface>>> ResolveInternal(ZeroconfOptions options,
                                                                         Action<string, Response, System.Net.NetworkInformation.NetworkInterface> callback,
                                                                         CancellationToken cancellationToken,
                                                                         System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            var requestBytes = GetRequestBytes(options);
            using (options.AllowOverlappedQueries ? Disposable.Empty : await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, ValueTuple<Response, System.Net.NetworkInformation.NetworkInterface>>();

                void Converter(IPAddress address, byte[] buffer, System.Net.NetworkInformation.NetworkInterface netInterface)
                {
                    var resp = new Response(buffer);
                    var firstPtr = resp.RecordsPTR.FirstOrDefault();
                    var name = firstPtr?.PTRDNAME.Split('.')[0] ?? string.Empty;
                    var addrString = address.ToString();

                    Debug.WriteLine($"IP: {addrString}, {(string.IsNullOrEmpty(name) ? string.Empty : $"Name: {name}, ")}Bytes: {buffer.Length}, IsResponse: {resp.header.QR}");

                    if (resp.header.QR)
                    {   var key = $"{addrString}{(string.IsNullOrEmpty(name) ? "" : $": {name}")}";
                        lock (dict)
                        {
                            dict[key] = (resp, netInterface);
                        }

                        callback?.Invoke(key, resp, netInterface);                        
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
                var question = new Question(protocol, queryType, QClass.IN);

                req.AddQuestion(question);
            }

            return req.Data;
        }

        static ZeroconfHost ResponseToZeroconf(Response response, string remoteAddress, ResolveOptions options, System.Net.NetworkInformation.NetworkInterface netInterface)
        {
            var z = new ZeroconfHost
            {
                IPAddresses = response.Answers
                                      .Select(r => r.RECORD)
                                      .OfType<RecordA>()
                                      .Concat(response.Additionals
                                                      .Select(r => r.RECORD)
                                                      .OfType<RecordA>())
                                      .Select(aRecord => aRecord.Address)
                                      .Distinct()
                                      .ToList(),
                NetworkInterface = netInterface
            };

            z.Id = z.IPAddresses.FirstOrDefault() ?? remoteAddress;
            
            var dispNameSet = false;
           
            foreach (var ptrRec in response.RecordsPTR)
            {
                // set the display name if needed
                if (!dispNameSet
                    && (options == null
                        || (options != null
                            && options.Protocols.Contains(ptrRec.RR.NAME))))
                {
                    z.DisplayName = ptrRec.PTRDNAME.Split('.')[0];
                    dispNameSet = true;
                }

                // Get the matching service records
                var responseRecords = response.RecordsRR
                                             .Where(r => r.NAME == ptrRec.PTRDNAME)
                                             .Select(r => r.RECORD)
                                             .ToList();

                var srvRec = responseRecords.OfType<RecordSRV>().FirstOrDefault();
                if (srvRec == null)
                    continue; // Missing the SRV record, not valid

                var svc = new Service
                {
                    Name = ptrRec.RR.NAME,
                    Port = srvRec.PORT,
                    Ttl = (int)srvRec.RR.TTL,

                };

                // There may be 0 or more text records - property sets
                foreach (var txtRec in responseRecords.OfType<RecordTXT>())
                {
                    var set = new Dictionary<string, string>();
                    foreach (var txt in txtRec.TXT)
                    {
                        var split = txt.Split(new[] {'='}, 2);
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

                z.AddService(svc);
            }

            return z;
        }


    }
}
