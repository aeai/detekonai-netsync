using System;

namespace Detekonai.Networking.NetSync.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
    public class NetSyncAttribute : Attribute
    {
        public string Name { get; set; } = "";
    }
}
