using Detekonai.Core;
using Detekonai.Networking.NetSync.Runtime.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Detekonai.Networking.NetSync.Runtime
{
    public class DefaultInterceptor : INetworkInterceptor
    {
        private readonly object owner;
        private readonly bool aotCompatMode;
        private readonly string objectId;
        private IMessageBus Bus { get; }

        private readonly object internalLock = new object();


        interface ISetInvoker
        {
            void Invoke(object owner, object value);
        }
        class SetInvoker<T, V> : ISetInvoker
        {
            private readonly Action<T, V> del;

            public SetInvoker(Delegate d)
            {
                del = (Action<T, V>)d;
            }

            public void Invoke(object owner, object value)
            {
                del.Invoke((T)owner, (V)value);
            }
        }

        private readonly Dictionary<(object, string), int> bingoTable = new Dictionary<(object, string), int>();
        private readonly Dictionary<string, ISetInvoker> setterMap = new Dictionary<string, ISetInvoker>();
        private readonly Dictionary<string, MethodInfo> methodAliasMap = new Dictionary<string, MethodInfo>();
        public DefaultInterceptor(IMessageBus bus, object owner, bool aotCompatMode = true)
        {
            Bus = bus;
            this.owner = owner;
            this.aotCompatMode = aotCompatMode;
            var attrib = owner.GetType().GetCustomAttribute<NetSyncAttribute>();
            if (attrib == null || string.IsNullOrEmpty(attrib.Name))
            {
                objectId = owner.GetType().Name;
            }
            else
            {
                objectId = attrib.Name;
            }
            if (!aotCompatMode)
            {
                BuildWriteCache();
            }
            BuildAliasMap();
            bus.Subscribe<NetSyncMessage>(OnPropertyMessage);
            bus.Subscribe<NetSyncRPCMessage>(OnRpcMessage);
        }

        private void BuildAliasMap()
        {
            foreach (var method in owner.GetType().GetMethods())
            {
                NetSyncAttribute attrib = method.GetCustomAttribute<NetSyncAttribute>();
                if (attrib != null)
                {
                    if (string.IsNullOrEmpty(attrib.Name))
                    {
                        methodAliasMap[method.Name] = method;
                    }
                    else
                    {
                        methodAliasMap[attrib.Name] = method;
                    }
                }
            }
        }

        private void BuildWriteCache()
        {
            foreach (var prop in owner.GetType().GetProperties())
            {
                if (prop.SetMethod == null)
                {
                    continue;
                }
                Type typ = typeof(Action<,>).MakeGenericType(owner.GetType(), prop.GetMethod.ReturnType);//prop.GetMethod.ReturnType);
                var ser = typeof(SetInvoker<,>).MakeGenericType(owner.GetType(), prop.GetMethod.ReturnType);
                setterMap[prop.Name] = (ISetInvoker)Activator.CreateInstance(ser, new object[] { prop.SetMethod.CreateDelegate(typ) });
            }
        }

        private void OnPropertyMessage(NetSyncMessage msg)
        {
            if (!msg.Local && msg.ObjectId == objectId)
            {
                AddBingo(msg.ObjectId, msg.PropertyId);
                if (aotCompatMode)
                {
                    owner.GetType().GetProperty(msg.PropertyId).SetValue(owner, msg.Value);
                }
                else
                {
                    setterMap[msg.PropertyId].Invoke(owner, msg.Value);
                }
            }
        }

        private void OnRpcMessage(NetSyncRPCMessage msg)
        {
            if (!msg.Local && msg.ObjectId == objectId)
            {
                AddBingo(msg.ObjectId, msg.MethodName);
                if (methodAliasMap.TryGetValue(msg.MethodName, out MethodInfo method))
                {
                    method.Invoke(owner, msg.Parameters);
                }
            }
        }

        private void AddBingo(string ob, string name)
        {
            var key = (ob, name);
            lock (internalLock)
            {
                bingoTable.TryGetValue(key, out int val);
                bingoTable[key] = val + 1;
            }
        }

        private bool BingoCheck(string ob, string name)
        {
            bool bingo = false;
            var key = (ob, name);
            lock (internalLock)
            {
                if (bingoTable.TryGetValue(key, out int val))
                {
                    if (val == 1)
                    {
                        bingoTable.Remove(key);
                    }
                    else
                    {
                        bingoTable[key] = val - 1;
                    }
                    bingo = true;
                }
            }
            return bingo;
        }

        public void WriteValue(string obName, string propName, object ob)
        {
            bool bingo = BingoCheck(obName, propName);
            if (!bingo)
            {
                Console.WriteLine($"Intercepted: {obName}.{propName} = {ob}");
                Bus.Trigger(new NetSyncMessage() { ObjectId = obName, PropertyId = propName, Value = ob, Local = true });
            }
        }

        public void CallFunction(string obName, string functionName, object[] parameters)
        {
            bool bingo = BingoCheck(obName, functionName);
            if (!bingo)
            {
                Console.WriteLine($"Intercepted: {obName}.{functionName}({string.Join(", ", parameters)})");
                Bus.Trigger(new NetSyncRPCMessage() { ObjectId = obName, MethodName = functionName, Parameters = parameters, Local = true });
            }
        }
    }
}
