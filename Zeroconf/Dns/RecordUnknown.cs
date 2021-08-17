using System;

namespace Heijden.DNS
{
    class RecordUnknown : Record
	{
		public byte[] RDATA;
		public RecordUnknown(RecordReader rr)
		{
			// re-read length
			var RDLENGTH = rr.ReadUInt16(-2);
			RDATA = rr.ReadBytes(RDLENGTH);
		}
	}
}
