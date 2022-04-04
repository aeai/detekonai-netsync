using Detekonai.Core;
using Detekonai.Networking.NetSync.Runtime.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime.NetSyncTypes
{
    public class NetSyncList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly IList<T> target;
        private readonly string objectId;
        private readonly string memberId;
        public IMessageBus Bus { get; set; }

        public T this[int index]
        {
            get
            {
                return target[index];
            }
            set
            {
                Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = "[]", Parameters = new object[] {index, value }, Local = true });
                target[index] = value;
            }
        }

        public int Count => target.Count;

        public bool IsReadOnly => target.IsReadOnly;

        public NetSyncList(IList<T> target, string objectId, string memberId)
        {
            this.target = target;
            this.objectId = objectId;
            this.memberId = memberId;
        }

        public NetSyncList(string objectId, string memberId)
        {
            this.target = new List<T>();
            this.objectId = objectId;
            this.memberId = memberId;
        }

        public void Activate() 
        {
            Bus.Subscribe<NetSyncMemberRPCMessage>(OnMessage);
        }

        public void Deactivate()
        {
            Bus?.Unsubscribe<NetSyncMemberRPCMessage>(OnMessage);
        }

        private void OnMessage(NetSyncMemberRPCMessage msg)
        {
            if(!msg.Local && msg.ObjectId == objectId && msg.MemberName == memberId)
            {
                switch (msg.MethodName)
                {
                    case nameof(Add):
                        target.Add((T)msg.Parameters[0]);
                        break;
                    case nameof(Remove):
                        target.Remove((T)msg.Parameters[0]);
                        break;
                    case nameof(RemoveAt):
                        target.RemoveAt((int)msg.Parameters[0]);
                        break;
                    case nameof(Clear):
                        target.Clear();
                        break;
                    case nameof(Insert):
                        target.Insert((int)msg.Parameters[0], (T)msg.Parameters[1]);
                        break;
                    case "[]":
                        target[(int)msg.Parameters[0]] = (T)msg.Parameters[1];
                        break;
                    default:
                        throw new InvalidOperationException($"Component {msg.ObjectId}.{msg.MemberName} don't have method named {msg.MethodName}");
                };
            }
        }

        public void Add(T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Add), Parameters = new object[] { item}, Local = true });
            target.Add(item);
        }

        public void Clear()
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Clear), Parameters = Array.Empty<object>(), Local = true });
            target.Clear();
        }

        public bool Contains(T item)
        {
            return target.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            target.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return target.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return target.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Insert), Parameters = new object[] { index, item }, Local = true });
            target.Insert(index, item);
        }

        public bool Remove(T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Remove), Parameters = new object[] { item }, Local = true });
            return target.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(RemoveAt), Parameters = new object[] { index }, Local = true });
            target.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return target.GetEnumerator();
        }
    }
}
