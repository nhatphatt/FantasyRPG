using System;

namespace FantasyRPG.Core.Utilities;

/// <summary>
/// Zero-allocation object pool for reusable reference types (projectiles,
/// particles, VFX). Pre-allocates a fixed-size array. No resizing at runtime.
///
/// Usage:
///   var pool = new ObjectPool&lt;Projectile&gt;(() => new Projectile(), 256);
///   Projectile p = pool.Rent();     // zero-alloc during gameplay
///   pool.Return(p);                 // return to pool when done
/// </summary>
public sealed class ObjectPool<T> where T : class
{
    private readonly T[] _pool;
    private int _count;

    public int Available => _count;
    public int Capacity => _pool.Length;

    /// <summary>
    /// Creates a pool of the given capacity, immediately pre-allocating all objects
    /// using the provided factory. Call this during LoadContent or Initialize.
    /// </summary>
    public ObjectPool(Func<T> factory, int capacity)
    {
        _pool = new T[capacity];
        _count = capacity;

        for (int i = 0; i < capacity; i++)
            _pool[i] = factory();
    }

    /// <summary>
    /// Rents an object from the pool. Returns null if exhausted.
    /// Zero allocations — just pops from the internal stack.
    /// </summary>
    public T? Rent()
    {
        if (_count <= 0)
            return null;

        _count--;
        T item = _pool[_count];
        _pool[_count] = null!; // Clear reference in pool slot
        return item;
    }

    /// <summary>
    /// Returns an object to the pool. The caller MUST reset
    /// the object's state before or after returning it.
    /// </summary>
    public void Return(T item)
    {
        if (_count >= _pool.Length)
            return; // Pool is full — silently drop (better than crash in game loop)

        _pool[_count] = item;
        _count++;
    }
}
