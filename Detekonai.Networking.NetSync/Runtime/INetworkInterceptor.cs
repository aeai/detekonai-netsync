using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime
{
    public interface INetworkInterceptor
    {
        void WriteValue(string obName, string propName, object ob);
        void CallFunction(string obName, string functionName, object[] parameters);
    }
}
