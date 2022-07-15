using System;
using System.Collections;
using System.Collections.Generic;
using Mirage.Serialization;

namespace Mirage.Collections
{
    public class SyncList<T> : IList<T>, IReadOnlyList<T>, ISyncObject
    {
        private readonly IList<T> objects;
        private readonly IEqualityComparer<T> comparer;

        public int Count => this.objects.Count;
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Raised when an element is added to the list.
        /// Receives index and new item
        /// </summary>
        public event Action<int, T> OnInsert;

        /// <summary>
        /// Raised when the list is cleared
        /// </summary>
        public event Action OnClear;

        /// <summary>
        /// Raised when an item is removed from the list
        /// receives the index and the old item
        /// </summary>
        public event Action<int, T> OnRemove;

        /// <summary>
        /// Raised when an item is changed in a list
        /// Receives index, old item and new item
        /// </summary>
        public event Action<int, T, T> OnSet;

        /// <summary>
        /// Raised after the list has been updated
        /// Note that if there are multiple changes
        /// this event is only raised once.
        /// </summary>
        public event Action OnChange;

        private enum Operation : byte
        {
            OP_ADD,
            OP_CLEAR,
            OP_INSERT,
            OP_REMOVEAT,
            OP_SET
        }

        private struct Change
        {
            internal Operation operation;
            internal int index;
            internal T item;
        }

        private readonly List<Change> changes = new List<Change>();

        // how many changes we need to ignore
        // this is needed because when we initialize the list,
        // we might later receive changes that have already been applied
        // so we need to skip them
        private int changesAhead;

        internal int ChangeCount => this.changes.Count;

        public SyncList() : this(EqualityComparer<T>.Default)
        {
        }

        public SyncList(IEqualityComparer<T> comparer)
        {
            this.comparer = comparer ?? EqualityComparer<T>.Default;
            this.objects = new List<T>();
        }

        public SyncList(IList<T> objects, IEqualityComparer<T> comparer = null)
        {
            this.comparer = comparer ?? EqualityComparer<T>.Default;
            this.objects = objects;
        }

        public bool IsDirty => this.changes.Count > 0;

        // throw away all the changes
        // this should be called after a successfull sync
        public void Flush() => this.changes.Clear();

        public void Reset()
        {
            this.IsReadOnly = false;
            this.changes.Clear();
            this.changesAhead = 0;
            this.objects.Clear();
        }

        private void AddOperation(Operation op, int itemIndex, T newItem)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("Synclists can only be modified at the server");
            }

            var change = new Change
            {
                operation = op,
                index = itemIndex,
                item = newItem
            };

            this.changes.Add(change);
            OnChange?.Invoke();
        }

        public void OnSerializeAll(NetworkWriter writer)
        {
            // if init,  write the full list content
            writer.WritePackedUInt32((uint)this.objects.Count);

            for (var i = 0; i < this.objects.Count; i++)
            {
                var obj = this.objects[i];
                writer.Write(obj);
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
                        writer.Write(change.item);
                        break;

                    case Operation.OP_CLEAR:
                        break;

                    case Operation.OP_REMOVEAT:
                        writer.WritePackedUInt32((uint)change.index);
                        break;

                    case Operation.OP_INSERT:
                    case Operation.OP_SET:
                        writer.WritePackedUInt32((uint)change.index);
                        writer.Write(change.item);
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
            OnClear?.Invoke();
            this.changes.Clear();

            for (var i = 0; i < count; i++)
            {
                var obj = reader.Read<T>();
                this.objects.Add(obj);
                OnInsert?.Invoke(i, obj);
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

                    case Operation.OP_CLEAR:
                        this.DeserializeClear(apply);
                        break;

                    case Operation.OP_INSERT:
                        this.DeserializeInsert(reader, apply);
                        break;

                    case Operation.OP_REMOVEAT:
                        this.DeserializeRemoveAt(reader, apply);
                        break;

                    case Operation.OP_SET:
                        this.DeserializeSet(reader, apply);
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
                OnChange?.Invoke();
        }

        private void DeserializeAdd(NetworkReader reader, bool apply)
        {
            var newItem = reader.Read<T>();
            if (apply)
            {
                this.objects.Add(newItem);
                OnInsert?.Invoke(this.objects.Count - 1, newItem);
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

        private void DeserializeInsert(NetworkReader reader, bool apply)
        {
            var index = (int)reader.ReadPackedUInt32();
            var newItem = reader.Read<T>();
            if (apply)
            {
                this.objects.Insert(index, newItem);
                OnInsert?.Invoke(index, newItem);
            }
        }

        private void DeserializeRemoveAt(NetworkReader reader, bool apply)
        {
            var index = (int)reader.ReadPackedUInt32();
            if (apply)
            {
                var oldItem = this.objects[index];
                this.objects.RemoveAt(index);
                OnRemove?.Invoke(index, oldItem);
            }
        }

        private void DeserializeSet(NetworkReader reader, bool apply)
        {
            var index = (int)reader.ReadPackedUInt32();
            var newItem = reader.Read<T>();
            if (apply)
            {
                var oldItem = this.objects[index];
                this.objects[index] = newItem;
                OnSet?.Invoke(index, oldItem, newItem);
            }
        }

        public void Add(T item)
        {
            this.objects.Add(item);
            OnInsert?.Invoke(this.objects.Count - 1, item);
            this.AddOperation(Operation.OP_ADD, this.objects.Count - 1, item);
        }

        public void AddRange(IEnumerable<T> range)
        {
            foreach (var entry in range)
            {
                this.Add(entry);
            }
        }

        public void Clear()
        {
            this.objects.Clear();
            OnClear?.Invoke();
            this.AddOperation(Operation.OP_CLEAR, 0, default);
        }

        public bool Contains(T item) => this.IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex) => this.objects.CopyTo(array, arrayIndex);

        public int IndexOf(T item)
        {
            for (var i = 0; i < this.objects.Count; ++i)
                if (this.comparer.Equals(item, this.objects[i]))
                    return i;
            return -1;
        }

        public int FindIndex(Predicate<T> match)
        {
            for (var i = 0; i < this.objects.Count; ++i)
                if (match(this.objects[i]))
                    return i;
            return -1;
        }

        public T Find(Predicate<T> match)
        {
            var i = this.FindIndex(match);
            return (i != -1) ? this.objects[i] : default;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            var results = new List<T>();
            for (var i = 0; i < this.objects.Count; ++i)
                if (match(this.objects[i]))
                    results.Add(this.objects[i]);
            return results;
        }

        public void Insert(int index, T item)
        {
            this.objects.Insert(index, item);
            OnInsert?.Invoke(index, item);
            this.AddOperation(Operation.OP_INSERT, index, item);
        }

        public void InsertRange(int index, IEnumerable<T> range)
        {
            foreach (var entry in range)
            {
                this.Insert(index, entry);
                index++;
            }
        }

        public bool Remove(T item)
        {
            var index = this.IndexOf(item);
            var result = index >= 0;
            if (result)
            {
                this.RemoveAt(index);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            var oldItem = this.objects[index];
            this.objects.RemoveAt(index);
            OnRemove?.Invoke(index, oldItem);
            this.AddOperation(Operation.OP_REMOVEAT, index, default);
        }

        public int RemoveAll(Predicate<T> match)
        {
            var toRemove = new List<T>();
            for (var i = 0; i < this.objects.Count; ++i)
                if (match(this.objects[i]))
                    toRemove.Add(this.objects[i]);

            foreach (var entry in toRemove)
            {
                this.Remove(entry);
            }

            return toRemove.Count;
        }

        public T this[int i]
        {
            get => this.objects[i];
            set
            {
                if (!this.comparer.Equals(this.objects[i], value))
                {
                    var oldItem = this.objects[i];
                    this.objects[i] = value;
                    OnSet?.Invoke(i, oldItem, value);
                    this.AddOperation(Operation.OP_SET, i, value);
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // default Enumerator allocates. we need a custom struct Enumerator to
        // not allocate on the heap.
        // (System.Collections.Generic.List<T> source code does the same)
        //
        // benchmark:
        //   uMMORPG with 800 monsters, Skills.GetHealthBonus() which runs a
        //   foreach on skills SyncList:
        //      before: 81.2KB GC per frame
        //      after:     0KB GC per frame
        // => this is extremely important for MMO scale networking
        public struct Enumerator : IEnumerator<T>
        {
            private readonly SyncList<T> list;
            private int index;
            public T Current { get; private set; }

            public Enumerator(SyncList<T> list)
            {
                this.list = list;
                this.index = -1;
                this.Current = default;
            }

            public bool MoveNext()
            {
                if (++this.index >= this.list.Count)
                {
                    return false;
                }
                this.Current = this.list[this.index];
                return true;
            }

            public void Reset() => this.index = -1;
            object IEnumerator.Current => this.Current;
            public void Dispose()
            {
                // nothing to dispose
            }
        }
    }
}
