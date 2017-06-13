using System;
using System.Collections.Generic;
using System.Text;

namespace Heijden.DNS
{
    class Request
	{
		public Header header;
        readonly List<Question> questions;

        public Request()
		{
            header = new Header
            {
                OPCODE = OPCode.Query,
                QDCOUNT = 0
            };

            questions = new List<Question>();
		}

		public void AddQuestion(Question question)
		{
			questions.Add(question);
		}

		public byte[] Data
		{
			get
			{
				var data = new List<byte>();
				header.QDCOUNT = (ushort)questions.Count;
				data.AddRange(header.Data);
				foreach (var q in questions)
					data.AddRange(q.Data);
				return data.ToArray();
			}
		}
	}
}
