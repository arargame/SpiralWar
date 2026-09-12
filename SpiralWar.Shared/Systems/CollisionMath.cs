using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Geometry helpers for the collision system. Pure functions, no state, no allocations —
    /// which means they can be called tens of thousands of times a frame without consequence.
    ///
    /// Everything here works in the ORIENTED box's local space. The snake's rings are rotated
    /// to follow the spiral, so an axis-aligned test (what the first version used) reports hits
    /// in the empty corners of the bounding square and misses along the ring's thin axis. On a
    /// 48x28 ring rotated 45 degrees that is a ~35% error in the hit area, which the player
    /// feels immediately as "my shots don't register".
    /// </summary>
    public static class CollisionMath
    {
        /// <summary>Transforms a world point into a box's local, axis-aligned space.</summary>
        public static Vector2 ToLocal(Vector2 point, Vector2 boxCenter, float rotation)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            Vector2 d = point - boxCenter;

            return new Vector2(
                d.X * cos + d.Y * sin,
                -d.X * sin + d.Y * cos);
        }

        /// <summary>Point inside an oriented box.</summary>
        public static bool PointInOrientedBox(Vector2 point, Vector2 boxCenter,
            float width, float height, float rotation)
        {
            Vector2 local = ToLocal(point, boxCenter, rotation);
            return MathF.Abs(local.X) <= width * 0.5f &&
                   MathF.Abs(local.Y) <= height * 0.5f;
        }

        /// <summary>
        /// Swept test: does the segment from <paramref name="from"/> to <paramref name="to"/>
        /// touch the oriented box? This is what stops fast bullets from tunnelling.
        /// </summary>
        /// <param name="hitTime">0..1 position along the segment where contact starts.</param>
        public static bool SegmentIntersectsOrientedBox(Vector2 from, Vector2 to,
            Vector2 boxCenter, float width, float height, float rotation,
            out float hitTime)
        {
            hitTime = 0f;

            Vector2 a = ToLocal(from, boxCenter, rotation);
            Vector2 b = ToLocal(to, boxCenter, rotation);
            Vector2 d = b - a;

            float hw = width * 0.5f;
            float hh = height * 0.5f;

            float tMin = 0f;
            float tMax = 1f;

            if (!ClipAxis(a.X, d.X, hw, ref tMin, ref tMax)) return false;
            if (!ClipAxis(a.Y, d.Y, hh, ref tMin, ref tMax)) return false;

            hitTime = tMin < 0f ? 0f : tMin;
            return true;
        }

        /// <summary>Slab clip on one axis. Returns false as soon as the interval collapses.</summary>
        private static bool ClipAxis(float origin, float delta, float halfExtent,
            ref float tMin, ref float tMax)
        {
            const float Epsilon = 1e-6f;

            if (MathF.Abs(delta) < Epsilon)
            {
                // Parallel to this slab: either always inside it or never.
                return MathF.Abs(origin) <= halfExtent;
            }

            float inv = 1f / delta;
            float t1 = (-halfExtent - origin) * inv;
            float t2 = (halfExtent - origin) * inv;

            if (t1 > t2)
            {
                float swap = t1;
                t1 = t2;
                t2 = swap;
            }

            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;

            return tMin <= tMax;
        }

        /// <summary>Circle against an oriented box. Used for the ship's contact damage.</summary>
        public static bool CircleIntersectsOrientedBox(Vector2 circleCenter, float radius,
            Vector2 boxCenter, float width, float height, float rotation)
        {
            Vector2 local = ToLocal(circleCenter, boxCenter, rotation);

            float hw = width * 0.5f;
            float hh = height * 0.5f;

            // Closest point on the box to the circle, in local space.
            float cx = MathHelper.Clamp(local.X, -hw, hw);
            float cy = MathHelper.Clamp(local.Y, -hh, hh);

            float dx = local.X - cx;
            float dy = local.Y - cy;

            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>
        /// Conservative broad-phase radius of an oriented box: half its diagonal. Two objects
        /// further apart than the sum of their radii cannot possibly touch, so the expensive
        /// narrow-phase test can be skipped.
        /// </summary>
        public static float BoundingRadius(float width, float height)
            => MathF.Sqrt(width * width + height * height) * 0.5f;

        /// <summary>Squared distance — avoids a square root in broad-phase rejection.</summary>
        public static float DistanceSquared(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
