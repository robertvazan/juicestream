using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobertsStreamUtils
{
    [ProtoContract]
    class MultiplexPacket
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public long BrookId { get; set; }
        [ProtoMember(2)]
        public byte[] Data { get; set; }
        [ProtoMember(3)]
        public bool EndOfStream { get; set; }
    }
}
