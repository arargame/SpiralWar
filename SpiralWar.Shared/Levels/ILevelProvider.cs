namespace SpiralWar.Levels
{
    /// <summary>
    /// Source of level definitions. Today it is a formula (<see cref="LevelDesigner"/>);
    /// tomorrow it could be a JSON campaign file or a daily-challenge server, and nothing
    /// in the gameplay layer would have to change.
    /// </summary>
    public interface ILevelProvider
    {
        /// <summary>Highest level this provider can serve.</summary>
        int MaxLevel { get; }

        /// <summary>Returns the definition for a 1-based level index, clamped to the valid range.</summary>
        LevelDefinition GetLevel(int index);
    }
}
