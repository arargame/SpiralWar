using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpiralWar.Entities;
using SpiralWar.Levels;

namespace SpiralWar.Systems
{
    /// <summary>Payload describing one dynamite blast, for sound and particles.</summary>
    public sealed class DetonationEventArgs : EventArgs
    {
        public Vector2 Position { get; }
        public float Radius { get; }

        /// <summary>0 for a ring the player shot, 1+ for links in the chain it started.</summary>
        public int ChainDepth { get; }

        /// <summary>Rings that were destroyed by this single blast.</summary>
        public int Kills { get; }

        public DetonationEventArgs(Vector2 position, float radius, int chainDepth, int kills)
        {
            Position = position;
            Radius = radius;
            ChainDepth = chainDepth;
            Kills = kills;
        }
    }

    /// <summary>
    /// Resolves neon dynamite.
    ///
    /// A dynamite ring is the one ring on the snake worth going out of your way for: destroying
    /// it damages everything inside <see cref="GameConstants.DynamiteBlastRadius"/>, and any
    /// dynamite caught in that blast goes off too. On a tightly wound spiral a well-placed shot
    /// can unzip a quarter of the snake.
    ///
    /// Two things keep that from becoming a free win:
    ///   * damage falls off toward the edge of the blast, so a chain peters out
    ///   * the chain is capped at <see cref="GameConstants.DynamiteChainDepthLimit"/> generations
    ///
    /// The chain is resolved ITERATIVELY rather than through the kill event re-entering itself.
    /// Recursing through the event would mean a hundred-ring cascade unwinding on the call stack
    /// inside a collision loop, with every handler running at an unpredictable depth. Here the
    /// loop is explicit, the depth is a number, and the whole cascade lands in one frame.
    /// </summary>
    public sealed class DetonationSystem
    {
        private struct PendingBlast
        {
            public Vector2 Position;
            public int Depth;
        }

        /// <summary>Hard ceiling on blasts per frame, so a pathological chain cannot hang a frame.</summary>
        private const int MaxBlastsPerFrame = 64;

        private readonly Queue<PendingBlast> _pending = new Queue<PendingBlast>();
        private readonly List<SnakeSegment> _buffer = new List<SnakeSegment>(64);

        private bool _resolving;
        private int _currentDepth;

        /// <summary>Raised once per blast, after its damage has been applied.</summary>
        public event EventHandler<DetonationEventArgs>? Detonated;

        /// <summary>Rings destroyed by blasts this level. Statistics only.</summary>
        public int ChainKills { get; private set; }

        /// <summary>
        /// Registers a destroyed dynamite ring. Called from the kill handler; safe to call
        /// while <see cref="Resolve"/> is running, which is exactly how a chain propagates.
        /// </summary>
        public void Enqueue(SnakeSegment segment)
        {
            if (segment == null || !segment.IsDynamite) return;

            int depth = _resolving ? _currentDepth + 1 : 0;
            if (depth > GameConstants.DynamiteChainDepthLimit) return;

            _pending.Enqueue(new PendingBlast { Position = segment.Position, Depth = depth });
        }

        /// <summary>Works through the queue, including everything the queue grows while working.</summary>
        public void Resolve(Snake snake, LevelDefinition level)
        {
            if (snake == null || level == null || _pending.Count == 0) return;

            _resolving = true;
            int budget = MaxBlastsPerFrame;

            try
            {
                while (_pending.Count > 0 && budget-- > 0)
                {
                    PendingBlast blast = _pending.Dequeue();
                    _currentDepth = blast.Depth;

                    int kills = ApplyBlast(snake, level, blast.Position);
                    ChainKills += kills;

                    Detonated?.Invoke(this, new DetonationEventArgs(
                        blast.Position, GameConstants.DynamiteBlastRadius, blast.Depth, kills));
                }

                // Anything still queued when the budget ran out is dropped rather than carried
                // into the next frame: a blast that lands a frame later than its explosion reads
                // as a bug, not as a feature.
                _pending.Clear();
            }
            finally
            {
                _resolving = false;
                _currentDepth = 0;
            }
        }

        private int ApplyBlast(Snake snake, LevelDefinition level, Vector2 position)
        {
            float radius = GameConstants.DynamiteBlastRadius;
            snake.CollectSegmentsInRadius(position, radius, _buffer);

            int kills = 0;

            for (int i = 0; i < _buffer.Count; i++)
            {
                SnakeSegment segment = _buffer[i];
                if (!segment.IsAlive) continue;

                float distance = Vector2.Distance(segment.Position, position);
                float falloff = MathHelper.Lerp(1f, GameConstants.DynamiteEdgeFalloff,
                    MathHelper.Clamp(distance / radius, 0f, 1f));

                int damage = Math.Max(1, (int)MathF.Round(level.DynamiteBlastDamage * falloff));

                segment.TakeDamage(damage);

                // A ring that dies here raises SegmentKilled, whose handler calls Enqueue on
                // this same instance - which is how a chain gets added mid-resolve.
                if (!segment.IsAlive) kills++;
            }

            return kills;
        }

        public void Reset()
        {
            _pending.Clear();
            _buffer.Clear();
            ChainKills = 0;
            _resolving = false;
            _currentDepth = 0;
        }
    }
}
