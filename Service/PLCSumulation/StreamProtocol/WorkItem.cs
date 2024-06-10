using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PLCSumulation.StreamProtocol
{
    public class WorkItem
    {
        public byte[] Data { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
    }
}
