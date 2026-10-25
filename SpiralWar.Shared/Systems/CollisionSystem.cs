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
        public bool WasCritical { get; }
        public bool AppliedAcid { get; }

        public BulletImpactEventArgs(SnakeSegment segment, Vector2 point, Vector2 direction,
                                     int damageApplied, bool destroyed,
                                     bool wasCritical, bool appliedAcid)
        {
            Segment = segment;
            Point = point;
            Direction = direction;
            DamageApplied = damageApplied;
            Destroyed = destroyed;
            WasCritical = wasCritical;
            AppliedAcid = appliedAcid;
        }
    }

    /// <summary>
    /// Detects and resolves every contact in the game.
    ///
    /// Two things make this trustworthy where an axis-aligned version would not:
    ///
    ///  1. ORIENTED boxes. Rings are rotated along the spiral, so their real footprint is a
    ///     rotated box, not the square its AABB would imply. On a 48x28 ring at 45 degrees that
    ///     is a ~35% error in hit area, which the player feels as "my shots don't register".
    ///  2. SWEPT tests. A bullet covers ~11 px per frame at 660 px/s and a ring is 28 px on its
    ///     short axis, so a point test misses grazing shots and, at higher frame times, passes
    ///     clean through. The bullet's own last-frame position gives us the segment to sweep.
    ///
    /// Ring size is read from the SEGMENT, not from GameConstants, because the level's snake
    /// form scales it: a Bulwark ring is half again as large as a Swarm ring and its hit box
    /// has to agree with what is drawn.
    /// </summary>
    public sealed class CollisionSystem
    {
        private readonly BulletSystem _bulletSystem;
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

            float segmentRadius = CollisionMath.BoundingRadius(
                snake.Level.SegmentWidth, snake.Level.SegmentHeight);

            for (int b = 0; b < bullets.Count; b++)
            {
                Bullet bullet = bullets[b];
                if (!bullet.IsActive) continue;

                Vector2 from = bullet.PreviousPosition;
                Vector2 to = bullet.Position;

                Vector2 midpoint = (from + to) * 0.5f;
                float halfTravel = Vector2.Distance(from, to) * 0.5f;
                float broadRadius = halfTravel + segmentRadius;
                float broadRadiusSq = broadRadius * broadRadius;

                SnakeSegment? closest = null;
                float closestTime = float.MaxValue;

                foreach (SnakeSegment segment in snake.GetAliveBodySegments())
                {
                    // A piercing round is still physically inside the ring it just punched.
                    if (ReferenceEquals(segment, bullet.IgnoredSegment)) continue;

                    // Broad phase.
                    if (CollisionMath.DistanceSquared(midpoint, segment.Position) > broadRadiusSq)
                        continue;

                    // Narrow phase: swept segment against the oriented ring.
                    if (!CollisionMath.SegmentIntersectsOrientedBox(from, to,
                            segment.Position,
                            segment.Width, segment.Height,
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

                bool appliedAcid = false;
                if (bullet.AcidStacks > 0 && closest.IsAlive)
                {
                    closest.ApplyAcid(bullet.AcidStacks);
                    appliedAcid = true;
                }

                bool destroyed = !closest.IsAlive;
                bool wasCritical = bullet.IsCritical;

                bullet.ConsumeHit(closest);

                if (applied > 0 || destroyed)
                    BulletImpact?.Invoke(this, new BulletImpactEventArgs(
                        closest, impactPoint, direction, applied, destroyed,
                        wasCritical, appliedAcid));
            }
        }

        // ── Ship vs rings ──────────────────────────────────────────────────

        private void ResolvePlayerVsSegments(Snake snake, Player player)
        {
            if (!player.IsAlive) return;

            float segmentRadius = CollisionMath.BoundingRadius(
                snake.Level.SegmentWidth, snake.Level.SegmentHeight);

            float broadRadius = _playerRadius + segmentRadius;
            float broadRadiusSq = broadRadius * broadRadius;

            foreach (SnakeSegment segment in snake.GetAliveBodySegments())
            {
                if (CollisionMath.DistanceSquared(player.Position, segment.Position) > broadRadiusSq)
                    continue;

                if (!CollisionMath.CircleIntersectsOrientedBox(player.Position, _playerRadius,
                        segment.Position,
                        segment.Width, segment.Height,
                        segment.Rotation))
                    continue;

                // The ship's own cooldown decides whether this actually costs hit points.
                if (player.TakeContactDamage() > 0)
                    PlayerContact?.Invoke(this, segment);

                // One contact per frame is enough - the gate would reject the rest anyway.
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
