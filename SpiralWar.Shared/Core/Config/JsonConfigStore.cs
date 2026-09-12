using System;
using System.IO;
using System.Text.Json;

namespace SpiralWar.Core.Config
{
    /// <summary>
    /// File system backed <see cref="IConfigStore"/>. Documents are written as indented
    /// JSON under {LocalApplicationData}/ArarGames/SpiralWar so that a player can inspect
    /// or back them up.
    ///
    /// Writes go to a temporary file first and are then moved over the real one, so a crash
    /// mid-save can never leave a half written profile behind.
    /// </summary>
    public sealed class JsonConfigStore : IConfigStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _rootPath;

        public string RootPath => _rootPath;

        public JsonConfigStore(string? rootPath = null)
        {
            _rootPath = rootPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArarGames",
                "SpiralWar");
        }

        private string PathFor(string documentName) =>
            Path.Combine(_rootPath, documentName + ".json");

        public bool Exists(string documentName) => File.Exists(PathFor(documentName));

        public T? Load<T>(string documentName) where T : class
        {
            try
            {
                string path = PathFor(documentName);
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;

                return JsonSerializer.Deserialize<T>(json, Options);
            }
            catch (Exception ex)
            {
                // A corrupt document must never stop the game from starting.
                Console.WriteLine($"[Config] Load '{documentName}' failed: {ex.Message}");
                return null;
            }
        }

        public void Save<T>(string documentName, T value) where T : class
        {
            try
            {
                Directory.CreateDirectory(_rootPath);

                string path = PathFor(documentName);
                string tempPath = path + ".tmp";

                string json = JsonSerializer.Serialize(value, Options);
                File.WriteAllText(tempPath, json);

                // Atomic-ish replace.
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Save '{documentName}' failed: {ex.Message}");
            }
        }

        public void Delete(string documentName)
        {
            try
            {
                string path = PathFor(documentName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Delete '{documentName}' failed: {ex.Message}");
            }
        }
    }
}
