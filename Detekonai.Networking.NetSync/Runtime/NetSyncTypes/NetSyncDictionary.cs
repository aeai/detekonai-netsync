using Detekonai.Core;
using Detekonai.Networking.NetSync.Runtime.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetSync.Runtime.NetSyncTypes
{
    class NetSyncDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {

        private readonly IDictionary<TKey, TValue> target;
        private readonly string objectId;
        private readonly string memberId;
        public IMessageBus Bus { get; set; }
        public TValue this[TKey key] {
            get
            {
                return target[key];
            }
            set
            {
                Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = "[]", Parameters = new object[] { key, value }, Local = true });
                target[key] = value;
            }
        }

        public ICollection<TKey> Keys => target.Keys;

        public ICollection<TValue> Values => target.Values;

        public int Count => target.Count;

        public bool IsReadOnly => target.IsReadOnly;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => target.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => target.Values;

        public NetSyncDictionary(string objectId, string memberId) 
        {
            this.objectId = objectId;
            this.memberId = memberId;
            target = new Dictionary<TKey, TValue>();
        }

        public NetSyncDictionary(string objectId, string memberId, IDictionary<TKey, TValue> target)
        {
            this.objectId = objectId;
            this.memberId = memberId;
            this.target = target;
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
                        target.Add((TKey)msg.Parameters[0], (TValue)msg.Parameters[1]);
                        break;
                    case "AddItem":
                        target.Add((KeyValuePair<TKey, TValue>)msg.Parameters[0]);
                        break;
                    case nameof(Remove):
                        target.Remove((TKey)msg.Parameters[0]);
                        break;
                    case "RemoveItem":
                        target.Remove((KeyValuePair<TKey, TValue>)msg.Parameters[0]);
                        break;
                    case nameof(Clear):
                        target.Clear();
                        break;
                    case "[]":
                        target[(TKey)msg.Parameters[0]] = (TValue)msg.Parameters[1];
                        break;
                    default:
                        throw new InvalidOperationException($"Component {msg.ObjectId}.{msg.MemberName} don't have method named {msg.MethodName}");
                };
            }
        }

        public void Add(TKey key, TValue value)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Add), Parameters = new object[] { key, value }, Local = true });
            target.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = "AddItem", Parameters = new object[] { item }, Local = true });
            target.Add(item);
        }

        public void Clear()
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Clear), Parameters = Array.Empty<object>(), Local = true });
            target.Clear();
        }
        public bool Remove(TKey key)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = nameof(Remove), Parameters = new object[] { key }, Local = true });
            return target.Remove(key);
        }
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            Bus?.Trigger(new NetSyncMemberRPCMessage() { ObjectId = objectId, MemberName = memberId, MethodName = "RemoveItem", Parameters = new object[] { item }, Local = true });
            return target.Remove(item);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return target.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return target.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            target.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return target.GetEnumerator();
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return target.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return target.GetEnumerator();
        }
    }
}
