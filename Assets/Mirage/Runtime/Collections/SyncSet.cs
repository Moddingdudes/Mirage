using System;
using System.Collections;
using System.Collections.Generic;
using Mirage.Serialization;

namespace Mirage.Collections
{
    public class SyncSet<T> : ISet<T>, ISyncObject
    {
        protected readonly ISet<T> objects;

        public int Count => this.objects.Count;
        public bool IsReadOnly { get; private set; }

        internal int ChangeCount => this.changes.Count;

        /// <summary>
        /// Raised when an element is added to the list.
        /// Receives the new item
        /// </summary>
        public event Action<T> OnAdd;

        /// <summary>
        /// Raised when the set is cleared
        /// </summary>
        public event Action OnClear;

        /// <summary>
        /// Raised when an item is removed from the set
        /// receives the old item
        /// </summary>
        public event Action<T> OnRemove;

        /// <summary>
        /// Raised after the set has been updated
        /// Note that if there are multiple changes
        /// this event is only raised once.
        /// </summary>
        public event Action OnChange;

        private enum Operation : byte
        {
            OP_ADD,
            OP_CLEAR,
            OP_REMOVE
        }

        private struct Change
        {
            internal Operation operation;
            internal T item;
        }

        private readonly List<Change> changes = new List<Change>();

        // how many changes we need to ignore
        // this is needed because when we initialize the list,
        // we might later receive changes that have already been applied
        // so we need to skip them
        private int changesAhead;

        public SyncSet(ISet<T> objects)
        {
            this.objects = objects;
        }

        public void Reset()
        {
            this.IsReadOnly = false;
            this.changes.Clear();
            this.changesAhead = 0;
            this.objects.Clear();
        }

        public bool IsDirty => this.changes.Count > 0;

        // throw away all the changes
        // this should be called after a successfull sync
        public void Flush() => this.changes.Clear();

        private void AddOperation(Operation op) => this.AddOperation(op, default);

        private void AddOperation(Operation op, T item)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("SyncSets can only be modified at the server");
            }

            var change = new Change
            {
                operation = op,
                item = item
            };

            this.changes.Add(change);
            OnChange?.Invoke();
        }

        public void OnSerializeAll(NetworkWriter writer)
        {
            // if init,  write the full list content
            writer.WritePackedUInt32((uint)this.objects.Count);

            foreach (var obj in this.objects)
            {
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

                    case Operation.OP_REMOVE:
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
            this.changes.Clear();
            OnClear?.Invoke();

            for (var i = 0; i < count; i++)
            {
                var obj = reader.Read<T>();
                this.objects.Add(obj);
                OnAdd?.Invoke(obj);
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
            var item = reader.Read<T>();
            if (apply)
            {
                this.objects.Add(item);
                OnAdd?.Invoke(item);
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
            var item = reader.Read<T>();
            if (apply)
            {
                this.objects.Remove(item);
                OnRemove?.Invoke(item);
            }
        }

        public bool Add(T item)
        {
            if (this.objects.Add(item))
            {
                OnAdd?.Invoke(item);
                this.AddOperation(Operation.OP_ADD, item);
                return true;
            }
            return false;
        }

        void ICollection<T>.Add(T item) => _ = this.Add(item);

        public void Clear()
        {
            this.objects.Clear();
            OnClear?.Invoke();
            this.AddOperation(Operation.OP_CLEAR);
        }

        public bool Contains(T item) => this.objects.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => this.objects.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (this.objects.Remove(item))
            {
                OnRemove?.Invoke(item);
                this.AddOperation(Operation.OP_REMOVE, item);
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator() => this.objects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == this)
            {
                this.Clear();
                return;
            }

            // remove every element in other from this
            foreach (var element in other)
            {
                this.Remove(element);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other is ISet<T> otherSet)
            {
                this.IntersectWithSet(otherSet);
            }
            else
            {
                var otherAsSet = new HashSet<T>(other);
                this.IntersectWithSet(otherAsSet);
            }
        }

        private void IntersectWithSet(ISet<T> otherSet)
        {
            var elements = new List<T>(this.objects);

            foreach (var element in elements)
            {
                if (!otherSet.Contains(element))
                {
                    this.Remove(element);
                }
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) => this.objects.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => this.objects.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => this.objects.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => this.objects.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => this.objects.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => this.objects.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == this)
            {
                this.Clear();
            }
            else
            {
                foreach (var element in other)
                {
                    if (!this.Remove(element))
                    {
                        this.Add(element);
                    }
                }
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other != this)
            {
                foreach (var element in other)
                {
                    this.Add(element);
                }
            }
        }
    }

    public class SyncHashSet<T> : SyncSet<T>
    {
        public SyncHashSet() : this(EqualityComparer<T>.Default) { }

        public SyncHashSet(IEqualityComparer<T> comparer) : base(new HashSet<T>(comparer ?? EqualityComparer<T>.Default)) { }

        // allocation free enumerator
        public new HashSet<T>.Enumerator GetEnumerator() => ((HashSet<T>)this.objects).GetEnumerator();
    }

    public class SyncSortedSet<T> : SyncSet<T>
    {
        public SyncSortedSet() : this(Comparer<T>.Default) { }

        public SyncSortedSet(IComparer<T> comparer) : base(new SortedSet<T>(comparer ?? Comparer<T>.Default)) { }

        // allocation free enumerator
        public new SortedSet<T>.Enumerator GetEnumerator() => ((SortedSet<T>)this.objects).GetEnumerator();
    }
}
