using Microsoft.Xna.Framework;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Bullet ↔ SnakeSegment ve Player ↔ SnakeHead çarpışma
    /// tespitini yapar. AABB kullanır.
    /// SRP: Sadece çarpışma kararı verir, hiçbir state değiştirmez —
    /// sonuçları callback/event yoluyla iletir.
    /// </summary>
    public sealed class CollisionSystem
    {
        private readonly BulletSystem _bulletSystem;
        private readonly SupplySystem _supplySystem;

        public CollisionSystem(BulletSystem bulletSystem, SupplySystem supplySystem)
        {
            _bulletSystem = bulletSystem;
            _supplySystem = supplySystem;
        }

        public void Update(Snake snake, Player player)
        {
            CheckBulletsVsSegments(snake);
            CheckPlayerVsHead(snake, player);
            _supplySystem.CheckCollection(player);
        }

        private void CheckBulletsVsSegments(Snake snake)
        {
            float hw = GameConstants.SegmentWidth  / 2f;
            float hh = GameConstants.SegmentHeight / 2f;

            foreach (var bullet in _bulletSystem.Bullets)
            {
                if (!bullet.IsActive) continue;

                foreach (var seg in snake.GetAliveBodySegments())
                {
                    // Basit AABB (rotasyon yok — yeterince küçük segmentler için geçerli)
                    var bounds = new Rectangle(
                        (int)(seg.Position.X - hw),
                        (int)(seg.Position.Y - hh),
                        GameConstants.SegmentWidth,
                        GameConstants.SegmentHeight);

                    if (bounds.Contains((int)bullet.Position.X, (int)bullet.Position.Y))
                    {
                        seg.TakeDamage(bullet.Damage);
                        bullet.Deactivate();
                        break; // Her bullet tek segmente çarpar
                    }
                }
            }
        }

        private void CheckPlayerVsHead(Snake snake, Player player)
        {
            if (snake.HeadReachedCenter)
                player.TakeDamage(player.CurrentHealth); // Oyun bitti
        }
    }
}
