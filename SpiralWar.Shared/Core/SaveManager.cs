using System;
using System.IO;

namespace SpiralWar.Core
{
    /// <summary>
    /// Oyunun seviye bazlı gidişatını (progression) kalıcı hale getiren sistem.
    /// Kayıtlar LocalApplicationData altında "SpiralWar/save.dat" olarak tutulur.
    /// </summary>
    public static class SaveManager
    {
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpiralWar");
        private static readonly string FilePath = Path.Combine(FolderPath, "save.dat");

        /// <summary>
        /// Sadece mevcut (ulaşılan) Level'i kaydeder.
        /// </summary>
        public static void SaveLevel(int level)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);

                File.WriteAllText(FilePath, level.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Kayıtlı Level'ı yükler. Kayıt yoksa 1 döner.
        /// </summary>
        public static int LoadLevel()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string content = File.ReadAllText(FilePath);
                    if (int.TryParse(content, out int level) && level > 0)
                        return level;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load failed: {ex.Message}");
            }
            return 1;
        }

        /// <summary>
        /// Mevcut kaydı siler (New Game için).
        /// </summary>
        public static void ClearSave()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch
            {
                // Silme hatası tolere edilebilir.
            }
        }
    }
}
