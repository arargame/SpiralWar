using System;
using System.Collections.Generic;

namespace SpiralWar.Core.Pooling
{
    /// <summary>
    /// Fixed capacity, allocation free object pool.
    /// All instances are created once at construction time; Rent/Return only move
    /// references between two lists, so no garbage is produced during play.
    /// </summary>
    /// <typeparam name="T">A poolable reference type.</typeparam>
    public sealed class ObjectPool<T> where T : class, IPoolable
    {
        private readonly List<T> _active;
        private readonly Stack<T> _free;
        private readonly int _capacity;

        /// <summary>Objects currently handed out. Safe to iterate; do not mutate.</summary>
        public IReadOnlyList<T> Active => _active;

        public int Capacity => _capacity;
        public int ActiveCount => _active.Count;
        public int FreeCount => _free.Count;

        /// <param name="capacity">Hard upper bound of live instances.</param>
        /// <param name="factory">Creates one instance. Called exactly <paramref name="capacity"/> times.</param>
        public ObjectPool(int capacity, Func<T> factory)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _capacity = capacity;
            _active = new List<T>(capacity);
            _free = new Stack<T>(capacity);

            for (int i = 0; i < capacity; i++)
                _free.Push(factory());
        }

        /// <summary>
        /// Takes a free instance out of the pool, or returns null when the pool is exhausted.
        /// Callers must handle null: dropping an effect is always better than allocating
        /// during a frame.
        /// </summary>
        public T? Rent()
        {
            if (_free.Count == 0) return null;

            T item = _free.Pop();
            item.OnSpawned();
            _active.Add(item);
            return item;
        }

        /// <summary>
        /// Moves every instance whose IsActive turned false back into the free list.
        /// Call once per frame after updating the active set.
        /// </summary>
        public void ReclaimInactive()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                T item = _active[i];
                if (item.IsActive) continue;

                item.OnDespawned();
                _active.RemoveAt(i);
                _free.Push(item);
            }
        }

        /// <summary>Returns every active instance to the pool (level restart, screen exit).</summary>
        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].OnDespawned();
                _free.Push(_active[i]);
            }
            _active.Clear();
        }
    }
}
