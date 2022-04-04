using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime.Messages
{
    [NetworkSerializable]
    public class NetSyncMemberRPCMessage : BaseMessage
    {
        [NetworkSerializableProperty]
        public string ObjectId { get; set; }
        [NetworkSerializableProperty]
        public string MemberName { get; set; }
        [NetworkSerializableProperty]
        public string MethodName { get; set; }
        [NetworkSerializableProperty]
        public object[] Parameters { get; set; }
        public bool Local { get; set; } = false;
    }
}
