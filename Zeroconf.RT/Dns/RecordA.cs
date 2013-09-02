using System;
/*
 3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
 * 
 */

namespace Heijden.DNS
{
    internal class RecordA : Record
	{
        public
#if NETFX_CORE
 Windows.Networking.HostName
#else
        System.Net.IPAddress 
#endif
            Address;

		public RecordA(RecordReader rr)
		{
            //Address = new System.Net.IPAddress(rr.ReadBytes(4));
            var str = string.Format("{0}.{1}.{2}.{3}",
                rr.ReadByte(),
                rr.ReadByte(),
                rr.ReadByte(),
                rr.ReadByte());

#if NETFX_CORE
            Address = new Windows.Networking.HostName(str);
#else
            System.Net.IPAddress.TryParse(str, out this.Address);
#endif
		}

		public override string ToString()
		{
			return Address.ToString();
		}

	}
}
