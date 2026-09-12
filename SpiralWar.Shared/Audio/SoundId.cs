namespace SpiralWar.Audio
{
    /// <summary>
    /// Every sound the game can make. Values are used as array indices by the
    /// <see cref="SoundBank"/>, so do not reorder them casually.
    /// </summary>
    public enum SoundId
    {
        Shoot = 0,
        ShootWide,
        SegmentHit,
        SegmentArmorHit,
        SegmentDestroy,
        SupplyDrop,
        SupplyPickup,
        HealthPickup,
        PlayerHurt,
        PlayerDown,
        LevelClear,
        BossWarning,
        MenuMove,
        MenuSelect,
        MenuBack,

        Count
    }
}
