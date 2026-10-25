using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Levels;

namespace SpiralWar.Entities
{
    /// <summary>
    /// The spiral itself: a head on a parametric path, with every body ring following the
    /// head's history.
    ///
    /// The trail is indexed by ARC LENGTH, not by frame count. That matters more than it
    /// sounds: the head's tangential speed collapses as the radius shrinks, so a "N frames
    /// behind" rule makes the rings pile into each other near the core and stretch apart at the
    /// rim. Spacing the rings by distance keeps the snake looking like one solid object for the
    /// whole descent, and it also makes the collision boxes line up with what the player sees.
    ///
    /// <see cref="TimeScale"/> is how the stasis supply works. Scaling the time the spiral
    /// integrates - rather than scaling its speed constants - means the fail timer, the danger
    /// ring and the HUD countdown all stay consistent with each other for free.
    /// </summary>
    public sealed class Snake : GameObject
    {
        public SnakeSegment? Head { get; private set; }
        public IReadOnlyList<SnakeSegment> Segments => _segments;
        public LevelDefinition Level { get; }

        /// <summary>True once the head has arrived at the core - the player has lost.</summary>
        public bool HeadReachedCenter { get; private set; }

        /// <summary>0 at the rim, 1 at the core. Drives the warning ring and the HUD.</summary>
        public float Danger { get; private set; }

        /// <summary>
        /// Multiplier on how fast the spiral advances. 1 is normal; the stasis supply drops it
        /// to <see cref="GameConstants.StasisTimeScale"/> for a few seconds.
        /// </summary>
        public float TimeScale { get; set; } = 1f;

        public int TotalSegmentCount { get; }
        public int AliveSegmentCount { get; private set; }

        /// <summary>Raised when a body ring is destroyed. Carries the ring so drops can spawn.</summary>
        public event EventHandler<SnakeSegment>? SegmentKilled;

        /// <summary>Raised whenever acid eats hit points off a ring.</summary>
        public event EventHandler<SegmentDamageEventArgs>? SegmentAcidTicked;

        private readonly List<SnakeSegment> _segments = new List<SnakeSegment>();
        private readonly List<Vector2> _trail = new List<Vector2>();
        private readonly List<float> _step = new List<float>();

        private readonly Vector2 _center;
        private readonly float _startRadius;
        private readonly float _startAngle;
        private readonly float _segmentSpacing;
        private readonly float _cullMargin;
        private readonly Random _rng;

        private float _elapsed;
        private float _currentRadius;

        public Snake(LevelDefinition level, SpriteFont font, Texture2D pixel, int seed)
        {
            Level = level;
            _rng = new Random(seed);
            _center = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);

            _startRadius = LevelDesigner.ComputeSpawnRadius();
            _currentRadius = _startRadius;

            _startAngle = (float)(_rng.NextDouble() * Math.Tau);
            _segmentSpacing = level.SegmentSpacing;
            _cullMargin = level.SegmentWidth + 40f;

            TotalSegmentCount = level.SegmentCount;
            AliveSegmentCount = TotalSegmentCount;

            Build(level, font, pixel);
        }

        private void Build(LevelDefinition level, SpriteFont font, Texture2D pixel)
        {
            Head = new SnakeSegment(isHead: true, level, font, pixel, _rng);

            Vector2 spawn = _center + new Vector2(
                MathF.Cos(_startAngle) * _startRadius,
                MathF.Sin(_startAngle) * _startRadius);

            Head.MoveTo(spawn, 0f);
            Head.UpdateRotation(_startAngle + MathF.PI);

            _trail.Add(spawn);
            _step.Add(0f);

            for (int i = 0; i < TotalSegmentCount; i++)
            {
                var segment = new SnakeSegment(isHead: false, level, font, pixel, _rng);
                segment.MoveTo(spawn, 0f);
                segment.Died += OnSegmentDied;
                segment.AcidTicked += OnSegmentAcidTicked;
                _segments.Add(segment);
            }
        }

        private void OnSegmentDied(object? sender, EventArgs e)
        {
            if (sender is not SnakeSegment segment) return;

            AliveSegmentCount--;
            if (AliveSegmentCount < 0) AliveSegmentCount = 0;

            SegmentKilled?.Invoke(this, segment);
        }

        private void OnSegmentAcidTicked(object? sender, int amount)
        {
            if (sender is not SnakeSegment segment) return;
            SegmentAcidTicked?.Invoke(this, new SegmentDamageEventArgs(segment, amount));
        }

        public override void Update(GameTime gameTime)
        {
            if (Head == null) return;

            float rawDt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float dt = rawDt * (TimeScale <= 0f ? 0f : TimeScale);
            _elapsed += dt;

            AdvanceHead(gameTime, dt);
            RecordTrail();
            PlaceSegments(gameTime, rawDt);
        }

        // ── Head motion ────────────────────────────────────────────────────

        private void AdvanceHead(GameTime gameTime, float dt)
        {
            if (Head == null) return;

            float duration = MathF.Max(1f, Level.SpiralDuration);
            float u = MathF.Min(1f, _elapsed / duration);

            // Eased progress: starts at the authored pace and tightens toward the core.
            // At u = 1 this is exactly 1 regardless of the acceleration value.
            float accel = Level.InwardAcceleration;
            float progress = (u + accel * u * u) / (1f + accel);

            _currentRadius = _startRadius * (1f - progress);
            if (_currentRadius < 0f) _currentRadius = 0f;

            Danger = 1f - _currentRadius / _startRadius;

            float angle = _startAngle + Level.AngularSpeed * _elapsed;
            Vector2 target = _center + new Vector2(
                MathF.Cos(angle) * _currentRadius,
                MathF.Sin(angle) * _currentRadius);

            Vector2 move = target - Head.Position;
            if (move.LengthSquared() > 0.0001f)
                Head.UpdateRotation(MathF.Atan2(move.Y, move.X));

            Head.MoveTo(target, dt);
            Head.Update(gameTime);

            if (Vector2.Distance(Head.Position, _center) < GameConstants.HeadCenterRadius)
                HeadReachedCenter = true;
        }

        // ── Trail ──────────────────────────────────────────────────────────

        private void RecordTrail()
        {
            if (Head == null) return;

            Vector2 head = Head.Position;
            float moved = Vector2.Distance(head, _trail[0]);

            // Only record a new point once the head has actually travelled. Recording every
            // frame would fill the list with zero-length steps as the spiral slows down.
            if (moved < 0.5f)
            {
                _trail[0] = head;
                return;
            }

            _trail.Insert(0, head);
            _step.Insert(1, moved);
            _step[0] = 0f;

            TrimTrail();
        }

        private void TrimTrail()
        {
            float maxLength = (_segments.Count + 3) * _segmentSpacing;
            float total = 0f;

            for (int i = 1; i < _step.Count; i++)
            {
                total += _step[i];
                if (total <= maxLength) continue;

                // Everything past this index is further back than the tail can ever reach.
                int removeFrom = i + 1;
                if (removeFrom < _trail.Count)
                {
                    _trail.RemoveRange(removeFrom, _trail.Count - removeFrom);
                    _step.RemoveRange(removeFrom, _step.Count - removeFrom);
                }
                return;
            }
        }

        // ── Body placement ─────────────────────────────────────────────────

        private void PlaceSegments(GameTime gameTime, float rawDt)
        {
            int trailIndex = 1;
            float walked = 0f;

            for (int s = 0; s < _segments.Count; s++)
            {
                float desired = (s + 1) * _segmentSpacing;

                // Walk further down the trail until we have covered the desired distance.
                while (trailIndex < _trail.Count && walked + _step[trailIndex] < desired)
                {
                    walked += _step[trailIndex];
                    trailIndex++;
                }

                SnakeSegment segment = _segments[s];
                Vector2 position;
                float rotation;

                if (trailIndex >= _trail.Count)
                {
                    // The snake is not fully deployed yet: park the remaining rings on the
                    // oldest known point. They unfurl as the head travels.
                    position = _trail[_trail.Count - 1];
                    rotation = segment.Rotation;
                }
                else
                {
                    float span = _step[trailIndex];
                    float into = desired - walked;
                    float t = span > 0.0001f ? MathHelper.Clamp(into / span, 0f, 1f) : 0f;

                    Vector2 a = _trail[trailIndex - 1];
                    Vector2 b = _trail[trailIndex];
                    position = Vector2.Lerp(a, b, t);

                    Vector2 forward = a - b;
                    rotation = forward.LengthSquared() > 0.0001f
                        ? MathF.Atan2(forward.Y, forward.X)
                        : segment.Rotation;
                }

                if (segment.IsAlive)
                {
                    segment.MoveTo(position, rawDt);
                    segment.UpdateRotation(rotation);
                }

                // Dead rings still tick so their flash finishes, but they stop moving.
                segment.Update(gameTime);
            }
        }

        // ── Queries ────────────────────────────────────────────────────────

        /// <summary>Live body rings, for collision and HUD counts.</summary>
        public IEnumerable<SnakeSegment> GetAliveBodySegments()
        {
            for (int i = 0; i < _segments.Count; i++)
                if (_segments[i].IsAlive) yield return _segments[i];
        }

        /// <summary>
        /// Fills <paramref name="results"/> with every live ring whose centre is inside
        /// <paramref name="radius"/> of <paramref name="center"/>. Takes a caller-owned list so
        /// a chain of dynamite blasts does not allocate once per detonation.
        /// </summary>
        public void CollectSegmentsInRadius(Vector2 center, float radius, List<SnakeSegment> results)
        {
            if (results == null) return;
            results.Clear();

            float radiusSquared = radius * radius;

            for (int i = 0; i < _segments.Count; i++)
            {
                SnakeSegment segment = _segments[i];
                if (!segment.IsAlive) continue;

                float dx = segment.Position.X - center.X;
                float dy = segment.Position.Y - center.Y;
                if (dx * dx + dy * dy <= radiusSquared)
                    results.Add(segment);
            }
        }

        /// <summary>Nearest live ring to a point, or null when the snake is cleared.</summary>
        public SnakeSegment? FindNearestSegment(Vector2 point)
        {
            SnakeSegment? best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _segments.Count; i++)
            {
                SnakeSegment segment = _segments[i];
                if (!segment.IsAlive) continue;

                float dx = segment.Position.X - point.X;
                float dy = segment.Position.Y - point.Y;
                float distance = dx * dx + dy * dy;

                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = segment;
            }

            return best;
        }

        public bool IsCleared => AliveSegmentCount <= 0;

        public Vector2 Center => _center;

        public float HeadDistanceToCenter => Head == null
            ? float.MaxValue
            : Vector2.Distance(Head.Position, _center);

        /// <summary>Seconds left before the head arrives, at the current time scale.</summary>
        public float SecondsRemaining => MathF.Max(0f, Level.SpiralDuration - _elapsed);

        // ── Draw ───────────────────────────────────────────────────────────

        private bool IsOnScreen(Vector2 position)
            => position.X > -_cullMargin &&
               position.X < GameConstants.ScreenWidth + _cullMargin &&
               position.Y > -_cullMargin &&
               position.Y < GameConstants.ScreenHeight + _cullMargin;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Tail first so the head ends up on top.
            //
            // A boss spiral is well over a hundred rings and most of them are still off-stage
            // for the first half of the level. Each ring costs roughly fifty sprite draws once
            // its glow layers are counted, so skipping the ones nobody can see is the single
            // cheapest frame-time win available here.
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                SnakeSegment segment = _segments[i];
                if (!segment.IsAlive) continue;
                if (!IsOnScreen(segment.Position)) continue;

                segment.Draw(spriteBatch, gameTime);
            }

            if (Head != null && IsOnScreen(Head.Position))
                Head.Draw(spriteBatch, gameTime);
        }
    }

    /// <summary>Payload for ring damage events that are not tied to a projectile.</summary>
    public sealed class SegmentDamageEventArgs : EventArgs
    {
        public SnakeSegment Segment { get; }
        public int Amount { get; }

        public SegmentDamageEventArgs(SnakeSegment segment, int amount)
        {
            Segment = segment;
            Amount = amount;
        }
    }
}
