using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Düşman entity'ler için soyut temel; Speed ve Rotation taşır.
    /// </summary>
    public abstract class Enemy : Entity
    {
        public float Speed    { get; protected set; }
        public float Rotation { get; protected set; }

        protected Enemy(int health, float speed) : base(health)
        {
            Speed = speed;
        }

        protected void SetRotation(float rotation) => Rotation = rotation;
    }
}
