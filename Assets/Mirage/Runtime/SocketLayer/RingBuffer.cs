using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mirage.SocketLayer
{
    public class RingBuffer<T>
    {
        public readonly Sequencer Sequencer;
        private readonly IEqualityComparer<T> comparer;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDefault(T value)
        {
            return this.comparer.Equals(value, default(T));
        }

        private readonly T[] buffer;

        /// <summary>oldtest item</summary>
        private uint read;

        /// <summary>newest item</summary>
        private uint write;

        /// <summary>manually keep track of number of items queued/inserted, this will be different from read to write range if removing/inserting not in order</summary>
        private int count;

        public uint Read => this.read;
        public uint Write => this.write;

        /// <summary>
        /// Number of non-null items in buffer
        /// <para>NOTE: this is not distance from read to write</para>
        /// </summary>
        public int Count => this.count;

        public T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.buffer[index];
        }
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.buffer[index];
        }

        public RingBuffer(int bitCount) : this(bitCount, EqualityComparer<T>.Default) { }
        public RingBuffer(int bitCount, IEqualityComparer<T> comparer)
        {
            this.Sequencer = new Sequencer(bitCount);
            this.buffer = new T[1 << bitCount];
            this.comparer = comparer;
        }

        public bool IsFull => this.Sequencer.Distance(this.write, this.read) == -1;
        public long DistanceToRead(uint from)
        {
            return this.Sequencer.Distance(from, this.read);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>sequance of written item</returns>
        public uint Enqueue(T item)
        {
            var dist = this.Sequencer.Distance(this.write, this.read);
            if (dist == -1) { throw new InvalidOperationException($"Buffer is full, write:{this.write} read:{this.read}"); }

            this.buffer[this.write] = item;
            var sequence = this.write;
            this.write = (uint)this.Sequencer.NextAfter(this.write);
            this.count++;
            return sequence;
        }

        /// <summary>
        /// Tries to read the item at read index
        /// <para>same as <see cref="TryDequeue"/> but does not remove the item after reading it</para>
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if item exists, or false if it is missing</returns>
        public bool TryPeak(out T item)
        {
            item = this.buffer[this.read];
            return !this.IsDefault(item);
        }

        /// <summary>
        /// Does item exist at index
        /// <para>Index will be moved into bounds</para>
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if item exists, or false if it is missing</returns>
        public bool Exists(uint index)
        {
            var inBounds = (uint)this.Sequencer.MoveInBounds(index);
            return !this.IsDefault(this.buffer[inBounds]);
        }

        /// <summary>
        /// Removes the item at read index and increments read index
        /// <para>can be used after <see cref="TryPeak"/> to do the same as <see cref="TryDequeue"/></para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveNext()
        {
            this.buffer[this.read] = default;
            this.read = (uint)this.Sequencer.NextAfter(this.read);
            this.count--;
        }

        /// <summary>
        /// Removes next item and increments read index
        /// <para>Assumes next items exists, best to use this with <see cref="Exists"/></para>
        /// </summary>
        public T Dequeue()
        {
            var item = this.buffer[this.read];
            this.RemoveNext();
            return item;
        }

        /// <summary>
        /// Tries to remove the item at read index
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if item exists, or false if it is missing</returns>
        public bool TryDequeue(out T item)
        {
            item = this.buffer[this.read];
            if (!this.IsDefault(item))
            {
                this.RemoveNext();

                return true;
            }
            else
            {
                return false;
            }
        }


        public void InsertAt(uint index, T item)
        {
            this.count++;
            this.buffer[index] = item;
        }
        public void RemoveAt(uint index)
        {
            this.count--;
            this.buffer[index] = default;
        }


        /// <summary>
        /// Moves read index to next non empty position
        /// <para>this is useful when removing items from buffer in random order.</para>
        /// <para>Will stop when write == read, or when next buffer item is not empty</para>
        /// </summary>
        public void MoveReadToNextNonEmpty()
        {
            // if read == write, buffer is empty, dont move it
            // if buffer[read] is empty then read to next item
            while (this.write != this.read && this.IsDefault(this.buffer[this.read]))
            {
                this.read = (uint)this.Sequencer.NextAfter(this.read);
            }
        }

        /// <summary>
        /// Moves read 1 index
        /// </summary>
        public void MoveReadOne()
        {
            this.read = (uint)this.Sequencer.NextAfter(this.read);
        }
    }
}
