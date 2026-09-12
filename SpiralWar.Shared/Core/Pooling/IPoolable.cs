namespace SpiralWar.Core.Pooling
{
    /// <summary>
    /// Contract for objects that are recycled by an <see cref="ObjectPool{T}"/> instead of
    /// being allocated per use. Keeping this separate from GameObject means pooling can be
    /// applied to particles, bullets and floating text without a shared base class (ISP).
    /// </summary>
    public interface IPoolable
    {
        /// <summary>True while the object is in use by the game.</summary>
        bool IsActive { get; }

        /// <summary>Called when the pool hands the object back to the caller.</summary>
        void OnSpawned();

        /// <summary>Called when the object is returned to the pool. Must clear all state.</summary>
        void OnDespawned();
    }
}
