using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Heijden.DNS;
using Type = Heijden.DNS.Type;

namespace Zeroconf
{
    public static class ZeroconfResolver
    {
        public static IObservable<ZeroconfRecord> Resolve(string protocol)
        {
            return Observable.Create<ZeroconfRecord>(async observer =>
                {
                    var socket = new DatagramSocket();
                    var s = Observable
                        .FromEventPattern
                        <TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs>,
                            DatagramSocketMessageReceivedEventArgs>(
                                x => socket.MessageReceived += x, _ => socket.Dispose())
                        .Select(ProcessMessage)
                        .Where(x => x != null)
                        .Subscribe(observer);
                    await socket.BindServiceNameAsync("5353");
                    socket.JoinMulticastGroup(new HostName("224.0.0.251"));
                    var os = await socket.GetOutputStreamAsync(new HostName("224.0.0.251"), "5353");
                    var writer = new DataWriter(os);
                    WriteQueryMessage(protocol, writer);
                    writer.StoreAsync();
                    return s;
                });
        }

        private static void WriteQueryMessage(string protocol, IDataWriter dataWriter)
        {
            // This writes a protocol question to mDNS frame (http://en.wikipedia.org/wiki/MDNS) 
            dataWriter.WriteUInt16(0);  // ID
            dataWriter.WriteInt16(0);   // Flags
            dataWriter.WriteInt16(1);   // QDCOUNT (questions count)
            dataWriter.WriteInt16(0);   // ANCOUNT (answers count)
            dataWriter.WriteInt16(0);   // NSCOUNT (name server count)
            dataWriter.WriteInt16(0);   // ARCOUNT (additional records count)
            WriteParts(protocol.Split('.'), dataWriter);
            dataWriter.WriteUInt16(255);  // Type=ALL
            dataWriter.WriteUInt16(255);  // Class=ALL
        }

        private static void WriteParts(IEnumerable<string> parts, IDataWriter dataWriter)
        {
            foreach (var part in parts.TakeWhile(part => part.Length != 0))
            {
                dataWriter.WriteByte((byte)part.Length);
                dataWriter.WriteString(part);
            }
            dataWriter.WriteByte(0);
        }

        private static Response ReadDnsResponse(IDataReader dataReader)
        {

            var resp = new Response(dataReader.ReadBuffer(dataReader.UnconsumedBufferLength).ToArray());

            return resp;
        }

        

        private static ZeroconfRecord ProcessMessage(EventPattern<DatagramSocketMessageReceivedEventArgs> eventPattern)
        {
            var dr = eventPattern.EventArgs.GetDataReader();
            var byteCount = dr.UnconsumedBufferLength;
            Debug.WriteLine("IP: {0} Bytes:{1}", eventPattern.EventArgs.RemoteAddress.DisplayName, byteCount);

            var r = ReadDnsResponse(dr);
            
           if(!r.header.QR)
               return null;
            
            
            return ResponseToZeroconf(r);}

        private static ZeroconfRecord ResponseToZeroconf(Response response)
        {
            // records by type
            var records = response.RecordsRR.ToLookup(record => record.Type);

            
            var z = new ZeroconfRecord();

            if (records.Contains(Type.PTR))
            {
                var ptr = (RecordPTR) records[Type.PTR].First().RECORD;
                z.Name = ptr.PTRDNAME.Split('.')[0];
            }

            if (records.Contains(Type.A))
            {
                var rr = records[Type.A].First();
                z.Host = rr.NAME.Split('.')[0];
                z.IPAddress = ((RecordA)rr.RECORD).Address;
            }

            if (records.Contains(Type.SRV))
            {
                var srv = (RecordSRV)records[Type.SRV].First().RECORD;
                z.Port = srv.PORT.ToString(CultureInfo.InvariantCulture);
            }

            if (records.Contains(Type.TXT))
            {
                foreach (var rr in records[Type.TXT])
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

    public class ZeroconfRecord
    {
        internal void AddProperty(string key, string value)
        {
            _properties[key] = value;
        }

        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();

        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Name:{0} IP:{1} Host:{2} Port:{3}", Name, IPAddress, Host, Port);

            if (_properties.Any())
            {
                sb.AppendLine();
                foreach (var kvp in _properties)
                {
                    sb.AppendFormat("{0} = {1}", kvp.Key, kvp.Value);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public IReadOnlyDictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }
    }
}
