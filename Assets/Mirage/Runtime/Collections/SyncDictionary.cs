using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mirage.Serialization;

namespace Mirage.Collections
{
    public class SyncIDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISyncObject, IReadOnlyDictionary<TKey, TValue>
    {
        protected readonly IDictionary<TKey, TValue> objects;

        public int Count => this.objects.Count;
        public bool IsReadOnly { get; private set; }

        internal int ChangeCount => this.changes.Count;


        /// <summary>
        /// Raised when an element is added to the dictionary.
        /// Receives the key and value of the new item
        /// </summary>
        public event Action<TKey, TValue> OnInsert;

        /// <summary>
        /// Raised when the dictionary is cleared
        /// </summary>
        public event Action OnClear;

        /// <summary>
        /// Raised when an item is removed from the dictionary
        /// receives the key and value of the old item
        /// </summary>
        public event Action<TKey, TValue> OnRemove;

        /// <summary>
        /// Raised when an item is changed in a dictionary
        /// Receives key, the old value and the new value
        /// </summary>
        public event Action<TKey, TValue, TValue> OnSet;

        /// <summary>
        /// Raised after the dictionary has been updated
        /// Note that if there are multiple changes
        /// this event is only raised once.
        /// </summary>
        public event Action OnChange;

        private enum Operation : byte
        {
            OP_ADD,
            OP_CLEAR,
            OP_REMOVE,
            OP_SET
        }

        private struct Change
        {
            internal Operation operation;
            internal TKey key;
            internal TValue item;
        }

        private readonly List<Change> changes = new List<Change>();

        // how many changes we need to ignore
        // this is needed because when we initialize the list,
        // we might later receive changes that have already been applied
        // so we need to skip them
        private int changesAhead;

        public void Reset()
        {
            this.IsReadOnly = false;
            this.changes.Clear();
            this.changesAhead = 0;
            this.objects.Clear();
        }

        public bool IsDirty => this.changes.Count > 0;

        public ICollection<TKey> Keys => this.objects.Keys;

        public ICollection<TValue> Values => this.objects.Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.objects.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.objects.Values;

        // throw away all the changes
        // this should be called after a successfull sync
        public void Flush() => this.changes.Clear();

        public SyncIDictionary(IDictionary<TKey, TValue> objects)
        {
            this.objects = objects;
        }

        private void AddOperation(Operation op, TKey key, TValue item)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("SyncDictionaries can only be modified by the server");
            }

            var change = new Change
            {
                operation = op,
                key = key,
                item = item
            };

            this.changes.Add(change);

            OnChange?.Invoke();
        }

        public void OnSerializeAll(NetworkWriter writer)
        {
            // if init,  write the full list content
            writer.WritePackedUInt32((uint)this.objects.Count);

            foreach (var syncItem in this.objects)
            {
                writer.Write(syncItem.Key);
                writer.Write(syncItem.Value);
            }

            // all changes have been applied already
            // thus the client will need to skip all the pending changes
            // or they would be applied again.
            // So we write how many changes are pending
            writer.WritePackedUInt32((uint)this.changes.Count);
        }

        public void OnSerializeDelta(NetworkWriter writer)
        {
            // write all the queued up changes
            writer.WritePackedUInt32((uint)this.changes.Count);

            for (var i = 0; i < this.changes.Count; i++)
            {
                var change = this.changes[i];
                writer.WriteByte((byte)change.operation);

                switch (change.operation)
                {
                    case Operation.OP_ADD:
                    case Operation.OP_REMOVE:
                    case Operation.OP_SET:
                        writer.Write(change.key);
                        writer.Write(change.item);
                        break;
                    case Operation.OP_CLEAR:
                        break;
                }
            }
        }

        public void OnDeserializeAll(NetworkReader reader)
        {
            // This list can now only be modified by synchronization
            this.IsReadOnly = true;

            // if init,  write the full list content
            var count = (int)reader.ReadPackedUInt32();

            this.objects.Clear();
            this.changes.Clear();
            OnClear?.Invoke();

            for (var i = 0; i < count; i++)
            {
                var key = reader.Read<TKey>();
                var obj = reader.Read<TValue>();
                this.objects.Add(key, obj);
                OnInsert?.Invoke(key, obj);
            }

            // We will need to skip all these changes
            // the next time the list is synchronized
            // because they have already been applied
            this.changesAhead = (int)reader.ReadPackedUInt32();
            OnChange?.Invoke();
        }

        public void OnDeserializeDelta(NetworkReader reader)
        {
            // This list can now only be modified by synchronization
            this.IsReadOnly = true;
            var raiseOnChange = false;

            var changesCount = (int)reader.ReadPackedUInt32();

            for (var i = 0; i < changesCount; i++)
            {
                var operation = (Operation)reader.ReadByte();

                // apply the operation only if it is a new change
                // that we have not applied yet
                var apply = this.changesAhead == 0;

                switch (operation)
                {
                    case Operation.OP_ADD:
                        this.DeserializeAdd(reader, apply);
                        break;

                    case Operation.OP_SET:
                        this.DeserializeSet(reader, apply);
                        break;

                    case Operation.OP_CLEAR:
                        this.DeserializeClear(apply);
                        break;

                    case Operation.OP_REMOVE:
                        this.DeserializeRemove(reader, apply);
                        break;
                }

                if (apply)
                {
                    raiseOnChange = true;
                }
                // we just skipped this change
                else
                {
                    this.changesAhead--;
                }
            }

            if (raiseOnChange)
            {
                OnChange?.Invoke();
            }
        }

        private void DeserializeAdd(NetworkReader reader, bool apply)
        {
            var key = reader.Read<TKey>();
            var item = reader.Read<TValue>();
            if (apply)
            {
                this.objects[key] = item;
                OnInsert?.Invoke(key, item);
            }
        }

        private void DeserializeSet(NetworkReader reader, bool apply)
        {
            var key = reader.Read<TKey>();
            var item = reader.Read<TValue>();
            if (apply)
            {
                var oldItem = this.objects[key];
                this.objects[key] = item;
                OnSet?.Invoke(key, oldItem, item);
            }
        }

        private void DeserializeClear(bool apply)
        {
            if (apply)
            {
                this.objects.Clear();
                OnClear?.Invoke();
            }
        }

        private void DeserializeRemove(NetworkReader reader, bool apply)
        {
            var key = reader.Read<TKey>();
            var item = reader.Read<TValue>();
            if (apply)
            {
                this.objects.Remove(key);
                OnRemove?.Invoke(key, item);
            }
        }

        public void Clear()
        {
            this.objects.Clear();
            OnClear?.Invoke();
            this.AddOperation(Operation.OP_CLEAR, default, default);
        }

        public bool ContainsKey(TKey key) => this.objects.ContainsKey(key);

        public bool Remove(TKey key)
        {
            if (this.objects.TryGetValue(key, out var item) && this.objects.Remove(key))
            {
                OnRemove?.Invoke(key, item);
                this.AddOperation(Operation.OP_REMOVE, key, item);
                return true;
            }
            return false;
        }

        public TValue this[TKey i]
        {
            get => this.objects[i];
            set
            {
                if (this.ContainsKey(i))
                {
                    var oldItem = this.objects[i];
                    this.objects[i] = value;
                    OnSet?.Invoke(i, oldItem, value);
                    this.AddOperation(Operation.OP_SET, i, value);
                }
                else
                {
                    this.objects[i] = value;
                    OnInsert?.Invoke(i, value);
                    this.AddOperation(Operation.OP_ADD, i, value);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value) => this.objects.TryGetValue(key, out value);

        public void Add(TKey key, TValue value)
        {
            this.objects.Add(key, value);
            OnInsert?.Invoke(key, value);
            this.AddOperation(Operation.OP_ADD, key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => this.Add(item.Key, item.Value);

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.TryGetValue(item.Key, out var val) && EqualityComparer<TValue>.Default.Equals(val, item.Value);
        }

        public void CopyTo([NotNull] KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array Index Out of Range");
            }
            if (array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentException("The number of items in the SyncDictionary is greater than the available space from " + nameof(arrayIndex) + " to the end of the destination array");
            }

            var i = arrayIndex;
            foreach (var item in this.objects)
            {
                array[i] = item;
                i++;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = this.objects.Remove(item.Key);
            if (result)
            {
                OnRemove?.Invoke(item.Key, item.Value);
                this.AddOperation(Operation.OP_REMOVE, item.Key, item.Value);
            }
            return result;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.objects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.objects.GetEnumerator();
    }

    public class SyncDictionary<TKey, TValue> : SyncIDictionary<TKey, TValue>
    {
        public SyncDictionary() : base(new Dictionary<TKey, TValue>())
        {
        }

        public SyncDictionary(IEqualityComparer<TKey> eq) : base(new Dictionary<TKey, TValue>(eq))
        {
        }

        public new Dictionary<TKey, TValue>.ValueCollection Values => ((Dictionary<TKey, TValue>)this.objects).Values;

        public new Dictionary<TKey, TValue>.KeyCollection Keys => ((Dictionary<TKey, TValue>)this.objects).Keys;

        public new Dictionary<TKey, TValue>.Enumerator GetEnumerator() => ((Dictionary<TKey, TValue>)this.objects).GetEnumerator();

    }
}
