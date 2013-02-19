using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Zeroconf
{
    public static class ZeroconfResolver
    {
        public static IObservable<ZeroconfRecord> Resolve(string protocol)
        {
            return Observable.Defer(async () =>
                {
                    var socket = new DatagramSocket();
                    var o = Observable
                        .FromEventPattern
                        <TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs>,
                            DatagramSocketMessageReceivedEventArgs>(
                                x => socket.MessageReceived += x, _ => socket.Dispose())
                        .Replay();
                    var d = o.Connect();
                    await socket.BindServiceNameAsync("5353");
                    socket.JoinMulticastGroup(new HostName("224.0.0.251"));
                    var os = await socket.GetOutputStreamAsync(new HostName("224.0.0.251"), "5353");
                    var writer = new DataWriter(os);
                    WriteQueryMessage(protocol, writer);
                    writer.StoreAsync();
                    return o
                        .Select(ProcessMessage)
                        .Where(x => x != null)
                        .Finally(d.Dispose);
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

        private static DnsMessage ReadDnsMessage(IDataReader dataReader)
        {
            var stringReader = new DnsStringReader(dataReader, dataReader.UnconsumedBufferLength);
            var msg = new DnsMessage
                {
                    QueryIdentifier = dataReader.ReadUInt16(),
                    Flags = dataReader.ReadUInt16(),
                };

            var qCount = dataReader.ReadUInt16();
            var aCount = dataReader.ReadUInt16();
            var nsCount = dataReader.ReadUInt16();
            var arCount = dataReader.ReadUInt16();
            var nonQuestions = aCount + nsCount + arCount;

            for (var i = 0; i < qCount; i++)
                msg.Records.Add(new QuestionRecord
                              {
                                  ResourceName = stringReader.ReadString(),
                                  QuestionType = dataReader.ReadUInt16(),
                                  Class = dataReader.ReadUInt16()
                              });

            for (var i = 0; i < nonQuestions; i++)
            {
                var resName = stringReader.ReadString();
                var resType = dataReader.ReadUInt16();
                var resClass = dataReader.ReadUInt16();
                var ttl = dataReader.ReadUInt32();
                var resLength = dataReader.ReadUInt16();
                var remaining = dataReader.UnconsumedBufferLength;
                ResourceRecord rec;
                switch (resType)
                {
                    case 1:  // A,
                        rec = new HostAddressRecord
                        {
                            IPAddress = string.Format("{0}.{1}.{2}.{3}",
                                                                dataReader.ReadByte(), dataReader.ReadByte(),
                                                                dataReader.ReadByte(), dataReader.ReadByte())
                        };
                        break;
                    case 12:  // PTR
                        rec = new PtrRecord
                        {
                            DomainNamePointer = stringReader.ReadString(resLength)
                        };
                        break;
                    case 16:  // TXT
                        rec = new TxtRecord
                        {
                            TextRData = stringReader.ReadString(resLength)
                        };
                        break;
                    case 33:  // SRV
                        rec = new SrvRecord
                        {
                            Priority = dataReader.ReadUInt16(),
                            Weight = dataReader.ReadUInt16(),
                            Port = dataReader.ReadUInt16(),
                            Target = stringReader.ReadString(resLength)
                        };
                        break;
                    default:
                        rec = new UnknownDnsRecord
                        {
                            ResourceType = resType
                        };
                        break;
                }
                rec.ResourceName = resName;
                rec.Class = resClass;
                rec.Ttl = ttl;
                msg.Records.Add(rec);
                var remainingResourceBytes = (int)(resLength - (remaining - dataReader.UnconsumedBufferLength));
                if (remainingResourceBytes < 0)
                {
                    Debug.WriteLine("error reading resource - reached into next record");
                    return msg;
                }
                if (remainingResourceBytes > 0)
                    dataReader.ReadBuffer((uint)remainingResourceBytes);
            }

            return msg;
        }

        private static ZeroconfRecord DnsToZeroconf(DnsMessage message)
        {
            if (!message.IsResponse)
                return null;
            var zr = new ZeroconfRecord();
            var ptr = message.Records.OfType<PtrRecord>().FirstOrDefault();
            if (ptr != null)
                zr.Name = ptr.DomainNamePointer.Split('.')[0];
            var hst = message.Records.OfType<HostAddressRecord>().FirstOrDefault();
            if (hst != null)
            {
                zr.Host = hst.ResourceName.Split('.')[0];
                zr.IPAddress = hst.IPAddress;
            }
            var srv = message.Records.OfType<SrvRecord>().FirstOrDefault();
            if (srv != null)
                zr.Port = srv.Port.ToString();
            return zr;
        }

        private static ZeroconfRecord ProcessMessage(EventPattern<DatagramSocketMessageReceivedEventArgs> eventPattern)
        {
            var dr = eventPattern.EventArgs.GetDataReader();
            var byteCount = dr.UnconsumedBufferLength;
            Debug.WriteLine("IP: {0} Bytes:{1}", eventPattern.EventArgs.RemoteAddress.DisplayName, byteCount);
            var msg = ReadDnsMessage(dr);
            return DnsToZeroconf(msg);
        }
    }

    public class ZeroconfRecord
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public override string ToString()
        {
            return string.Format("Name:{0} IP:{1} Host:{2} Port:{3}", Name, IPAddress, Host, Port);
        }
    }

    internal class DnsStringReader
    {
        private readonly Dictionary<uint, string> _map = new Dictionary<uint, string>();
        private readonly IDataReader _dataReader;
        private readonly uint _totalBytes;

        internal DnsStringReader(IDataReader dataReader, uint totalBytes)
        {
            _dataReader = dataReader;
            _totalBytes = totalBytes;
        }

        public string ReadString(ushort resLength = 0)
        {
            var substringPositions = new List<uint>();
            var startingPosition = _totalBytes - _dataReader.UnconsumedBufferLength;
            Func<string> retVal = () => substringPositions.Count == 0 ? "" : _map[substringPositions[0]];
            Action<string, uint> appendToPositions = (str, position) =>
            {
                foreach (var pos in substringPositions)
                    _map[pos] = _map[pos] + '.' + str;
                _map[position] = str;
                substringPositions.Add(position);
            };
            while (true)
            {
                var position = _totalBytes - _dataReader.UnconsumedBufferLength;
                if (resLength > 0 && position - startingPosition >= resLength)
                    return retVal();
                var strLen = _dataReader.ReadByte();
                if (strLen == 0)
                    return retVal();
                string str;
                if (DnsMath.IsPointer(strLen))
                {
                    var ptr = DnsMath.TwoBytesToPointer(strLen, _dataReader.ReadByte());
                    if (_map.TryGetValue(ptr, out str))
                        appendToPositions(str, position);
                    return retVal();
                }
                str = _dataReader.ReadString(strLen);
                appendToPositions(str, position);
            }
        }
    }
}
