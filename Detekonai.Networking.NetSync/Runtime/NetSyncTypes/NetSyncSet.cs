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
    class NetSyncSet<T> : ISet<T>, IReadOnlySet<T>
    {
        private readonly ISet<T> target;
        private readonly string objectId;
        private readonly string memberId;
        public IMessageBus Bus { get; set; }
        public int Count => target.Count;

        public bool IsReadOnly => target.IsReadOnly;

        public NetSyncSet(ISet<T> target, string objectId, string memberId)
        {
            this.target = target;
            this.objectId = objectId;
            this.memberId = memberId;
        }

        public NetSyncSet(string objectId, string memberId)
        {
            this.target = new HashSet<T>();
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
            if (!msg.Local && msg.ObjectId == objectId && msg.MemberName == memberId)
            {
                switch (msg.MethodName)
                {
                    case nameof(Add):
                        target.Add((T)msg.Parameters[0]);
                        break;
                    case "Add2":
                        ((ICollection<T>)target).Add((T)msg.Parameters[0]);
                        break;
                    case nameof(Remove):
                        target.Remove((T)msg.Parameters[0]);
                        break;
                    case nameof(Clear):
                        target.Clear();
                        break; 
                    default:
                        throw new InvalidOperationException($"Component {msg.ObjectId}.{msg.MemberName} don't have method named {msg.MethodName}");
                };
            }
        }

        public bool Add(T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Add), Parameters = new object[] { item }, Local = true });
            return target.Add(item);
        }

        public void Clear()
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Clear), Parameters = Array.Empty<object>(), Local = true });
            target.Clear();
        }
        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException($"Can't do {nameof(ExceptWith)} with a NetSync set!");
            //Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(ExceptWith), Parameters = new object[] { other }, Local = true });
            //target.ExceptWith(other);
        }
        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException($"Can't do {nameof(IntersectWith)} with a NetSync set!");
            //Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(IntersectWith), Parameters = new object[] { other }, Local = true });
            //target.IntersectWith(other);
        }

        public bool Remove(T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Remove), Parameters = new object[] { item }, Local = true });
            return target.Remove(item);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException($"Can't do {nameof(SymmetricExceptWith)} with a NetSync set!");
            //Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(SymmetricExceptWith), Parameters = new object[] { other }, Local = true });
            //target.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException($"Can't do {nameof(UnionWith)} with a NetSync set!");
            //Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(UnionWith), Parameters = new object[] { other }, Local = true });
            //target.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = "Add2", Parameters = new object[] { item }, Local = true });
            target.Add(item);
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


        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return target.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return target.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return target.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return target.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return target.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return target.SetEquals(other);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return target.GetEnumerator();
        }
    }
}
