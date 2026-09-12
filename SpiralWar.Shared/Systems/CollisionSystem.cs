using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>Payload describing a bullet that connected with a ring.</summary>
    public sealed class BulletImpactEventArgs : EventArgs
    {
        public SnakeSegment Segment { get; }
        public Vector2 Point { get; }
        public Vector2 Direction { get; }
        public int DamageApplied { get; }
        public bool Destroyed { get; }

        public BulletImpactEventArgs(SnakeSegment segment, Vector2 point, Vector2 direction,
                                     int damageApplied, bool destroyed)
        {
            Segment = segment;
            Point = point;
            Direction = direction;
            DamageApplied = damageApplied;
            Destroyed = destroyed;
        }
    }

    /// <summary>
    /// Detects and resolves every contact in the game.
    ///
    /// Two things make this trustworthy where the previous axis-aligned version was not:
    ///
    ///  1. ORIENTED boxes. Rings are rotated along the spiral, so their real footprint is a
    ///     48x28 box at an arbitrary angle, not the 48x48 square its AABB would imply.
    ///  2. SWEPT tests. A bullet covers ~11 px per frame at 660 px/s and a ring is 28 px on its
    ///     short axis, so a point test misses grazing shots and, at higher frame times, passes
    ///     clean through. The bullet's own last-frame position gives us the segment to sweep.
    ///
    /// Broad-phase rejection by bounding radius runs first, so the expensive slab clipping only
    /// happens for pairs that could plausibly touch.
    /// </summary>
    public sealed class CollisionSystem
    {
        private readonly BulletSystem _bulletSystem;

        private readonly float _segmentRadius;
        private readonly float _playerRadius;

        /// <summary>Raised for every bullet that connects, after damage has been applied.</summary>
        public event EventHandler<BulletImpactEventArgs>? BulletImpact;

        /// <summary>Raised when a ring overlaps the ship and the damage gate allowed a tick.</summary>
        public event EventHandler<SnakeSegment>? PlayerContact;

        /// <summary>Raised once, when the head arrives at the core.</summary>
        public event EventHandler? CoreBreached;

        private bool _coreBreachReported;

        public CollisionSystem(BulletSystem bulletSystem)
        {
            _bulletSystem = bulletSystem;

            _segmentRadius = CollisionMath.BoundingRadius(
                GameConstants.SegmentWidth, GameConstants.SegmentHeight);

            // The ship's triangle is roughly a disc of this radius for contact purposes.
            _playerRadius = GameConstants.PlayerSize * 0.55f;
        }

        public void Update(Snake snake, Player player)
        {
            if (snake == null || player == null) return;

            ResolveBulletsVsSegments(snake);
            ResolvePlayerVsSegments(snake, player);
            ResolveCore(snake, player);
        }

        // ── Bullets vs rings ───────────────────────────────────────────────

        private void ResolveBulletsVsSegments(Snake snake)
        {
            IReadOnlyList<Bullet> bullets = _bulletSystem.Bullets;
            if (bullets.Count == 0) return;

            for (int b = 0; b < bullets.Count; b++)
            {
                Bullet bullet = bullets[b];
                if (!bullet.IsActive) continue;

                Vector2 from = bullet.PreviousPosition;
                Vector2 to = bullet.Position;

                Vector2 midpoint = (from + to) * 0.5f;
                float halfTravel = Vector2.Distance(from, to) * 0.5f;
                float broadRadius = halfTravel + _segmentRadius;
                float broadRadiusSq = broadRadius * broadRadius;

                SnakeSegment? closest = null;
                float closestTime = float.MaxValue;

                foreach (SnakeSegment segment in snake.GetAliveBodySegments())
                {
                    // Broad phase.
                    if (CollisionMath.DistanceSquared(midpoint, segment.Position) > broadRadiusSq)
                        continue;

                    // Narrow phase: swept segment against the oriented ring.
                    if (!CollisionMath.SegmentIntersectsOrientedBox(from, to,
                            segment.Position,
                            GameConstants.SegmentWidth, GameConstants.SegmentHeight,
                            segment.Rotation,
                            out float hitTime))
                        continue;

                    // A bullet crossing several rings in one frame must hit the FIRST one.
                    if (hitTime >= closestTime) continue;

                    closestTime = hitTime;
                    closest = segment;
                }

                if (closest == null) continue;

                Vector2 impactPoint = Vector2.Lerp(from, to, closestTime);
                Vector2 direction = to - from;
                if (direction.LengthSquared() > 0.0001f)
                    direction.Normalize();

                int applied = closest.TakeDamage(bullet.Damage);
                bool destroyed = !closest.IsAlive;

                bullet.Consume();

                if (applied > 0 || destroyed)
                    BulletImpact?.Invoke(this,
                        new BulletImpactEventArgs(closest, impactPoint, direction, applied, destroyed));
            }
        }

        // ── Ship vs rings ──────────────────────────────────────────────────

        private void ResolvePlayerVsSegments(Snake snake, Player player)
        {
            if (!player.IsAlive) return;

            float broadRadius = _playerRadius + _segmentRadius;
            float broadRadiusSq = broadRadius * broadRadius;

            foreach (SnakeSegment segment in snake.GetAliveBodySegments())
            {
                if (CollisionMath.DistanceSquared(player.Position, segment.Position) > broadRadiusSq)
                    continue;

                if (!CollisionMath.CircleIntersectsOrientedBox(player.Position, _playerRadius,
                        segment.Position,
                        GameConstants.SegmentWidth, GameConstants.SegmentHeight,
                        segment.Rotation))
                    continue;

                // The ship's own cooldown decides whether this actually costs hit points.
                if (player.TakeContactDamage() > 0)
                    PlayerContact?.Invoke(this, segment);

                // One contact per frame is enough — the gate would reject the rest anyway.
                return;
            }
        }

        // ── The core ───────────────────────────────────────────────────────

        private void ResolveCore(Snake snake, Player player)
        {
            if (!snake.HeadReachedCenter || _coreBreachReported) return;

            _coreBreachReported = true;
            player.Obliterate();
            CoreBreached?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Clears per-level latches. Call when a new level starts.</summary>
        public void Reset() => _coreBreachReported = false;
    }
}
