using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
    public class NetSyncIgnoreAttribute : Attribute
    {
    }
}
