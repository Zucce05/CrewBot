using Newtonsoft.Json;
using System;
using System.IO;

namespace CrewBot.Classes
{
    public interface IJsonFileStore
    {
        T LoadOrDefault<T>(string path, T defaultValue);
        void Save<T>(string path, T value);
    }

    public class JsonFileStore : IJsonFileStore
    {
        public T LoadOrDefault<T>(string path, T defaultValue)
        {
            EnsureDirectory(path);

            if (!File.Exists(path))
            {
                Save(path, defaultValue);
                return defaultValue;
            }

            try
            {
                string fileText = File.ReadAllText(path);
                T value = JsonConvert.DeserializeObject<T>(fileText);
                if (value == null)
                {
                    BackupUnreadableFile(path);
                    return defaultValue;
                }

                return value;
            }
            catch (Exception)
            {
                BackupUnreadableFile(path);
                return defaultValue;
            }
        }

        public void Save<T>(string path, T value)
        {
            EnsureDirectory(path);
            string tempPath = $"{path}.tmp";
            string backupPath = $"{path}.bak";

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            try
            {
                using (StreamWriter file = File.CreateText(tempPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, value);
                }

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, backupPath, true);
                    return;
                }

                File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static void BackupUnreadableFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            File.Copy(path, $"{path}.bak", true);
        }

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
