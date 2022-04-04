using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime.Messages
{
    [NetworkSerializable]
    public class NetSyncMessage : BaseMessage
    {
        [NetworkSerializableProperty]
        public string ObjectId { get; set; }
        [NetworkSerializableProperty]
        public string PropertyId { get; set; }
        [NetworkSerializableProperty]
        public object Value { get; set; }
        public bool Local { get; set; } = false;
    }
}
