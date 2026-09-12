namespace SpiralWar.Core.Config
{
    /// <summary>
    /// Abstraction over "where does a document live". The game only ever talks to this
    /// interface, so the desktop file system can be swapped for isolated storage on
    /// mobile or a cloud backend later without touching gameplay code (DIP).
    /// </summary>
    public interface IConfigStore
    {
        /// <summary>Reads a document, or returns null when it does not exist / is corrupt.</summary>
        T? Load<T>(string documentName) where T : class;

        /// <summary>Writes a document. Implementations must never throw on IO failure.</summary>
        void Save<T>(string documentName, T value) where T : class;

        /// <summary>Removes a document if it exists.</summary>
        void Delete(string documentName);

        /// <summary>True when the document exists.</summary>
        bool Exists(string documentName);
    }
}
