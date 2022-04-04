using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime
{
    public class InterceptorAssistant : INetworkInterceptor
    {
        public INetworkInterceptor Interceptor { get; set; }

        public InterceptorAssistant()
        { 
        }

        public void WriteValue(string obName, string propName, object ob)
        {
            Interceptor?.WriteValue(obName, propName, ob);
        }

        public void CallFunction(string obName, string functionName, object[] parameters)
        {
            Interceptor?.CallFunction(obName, functionName, parameters);
        }
    }
}
