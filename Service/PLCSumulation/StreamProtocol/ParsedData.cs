using System;
using System.Collections.Generic;
using System.Text;

namespace PLCSumulation.StreamProtocol
{
    public class ParsedData
    {
        public int MessageType { get; set; }
        public byte[] Payload { get; set; }
    }
}
