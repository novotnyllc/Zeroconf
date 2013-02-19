using System.Collections.Generic;

namespace Zeroconf
{
    internal static class DnsMath
    {
        public static bool IsResponse(ushort flags) { return (flags & 0x8000) != 0; }

        public static bool IsPointer(byte value) { return (value & 0xC0) > 0; }

        public static ushort TwoBytesToPointer(byte first, byte second)
        {
            return (ushort) (((first ^ 0xC0) << 8) | second);
        }
    }

    internal class DnsMessage
    {
        private readonly List<DnsRecord> _records = new List<DnsRecord>();

        public ushort QueryIdentifier { get; set; }
        public ushort Flags { get; set; }
        public List<DnsRecord> Records
        {
            get { return _records; }
        }

        public bool IsResponse { get { return DnsMath.IsResponse(Flags); } }
    }

    internal class DnsRecord
    {
        public string ResourceName { get; set; }
        public ushort Class { get; set; }
    }

    internal class QuestionRecord : DnsRecord
    {
        public ushort QuestionType { get; set; }
    }

    internal class ResourceRecord : DnsRecord
    {
        public uint Ttl { get; set; }
    }

    internal class HostAddressRecord : ResourceRecord
    {
        public string IPAddress { get; set; }
    }

    internal class PtrRecord : ResourceRecord
    {
        public string DomainNamePointer { get; set; }
    }

    internal class TxtRecord : ResourceRecord
    {
        public string TextRData { get; set; }
    }

    internal class SrvRecord : ResourceRecord
    {
        public ushort Priority { get; set; }
        public ushort Weight { get; set; }
        public ushort Port { get; set; }
        public string Target { get; set; }
    }

    internal class UnknownDnsRecord : ResourceRecord
    {
        public ushort ResourceType { get; set; }
        public byte[] Data { get; set; }
    }
}
