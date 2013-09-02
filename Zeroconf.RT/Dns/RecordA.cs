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
		//public System.Net.IPAddress Address;
        public string Address;

		public RecordA(RecordReader rr)
		{
			//Address = new System.Net.IPAddress(rr.ReadBytes(4));
			//System.Net.IPAddress.TryParse(string.Format("{0}.{1}.{2}.{3}",
			//	rr.ReadByte(),
			//	rr.ReadByte(),
			//	rr.ReadByte(),
			//	rr.ReadByte()), out this.Address);

            Address = string.Format("{0}.{1}.{2}.{3}",
                rr.ReadByte(),
                rr.ReadByte(),
                rr.ReadByte(),
                rr.ReadByte());
		}

		public override string ToString()
		{
			return Address.ToString();
		}

	}
}
