namespace SpiralWar.Entities
{
    /// <summary>
    /// Base for hostile entities. Carries the two things every enemy in this game needs and
    /// nothing else: how fast it travels and which way it is facing.
    /// </summary>
    public abstract class Enemy : Entity
    {
        public float Speed { get; protected set; }
        public float Rotation { get; protected set; }

        protected Enemy(int health, float speed) : base(health)
        {
            Speed = speed;
        }

        protected void SetRotation(float rotation) => Rotation = rotation;
    }
}
