using GameTools.Encryption;
using System.Diagnostics;
using System.IO;
using Unity.Serialization.Json;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameTools.SaveSystem
{
    public static class SaveManager
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Open PlayerData Folder")]
        public static void OpenPlayerDataFolder()
        {
            string playerDataPath = root;
            if (!Directory.Exists(playerDataPath)) Directory.CreateDirectory(playerDataPath);
            Process.Start("explorer.exe", playerDataPath.Replace("/", "\\"));
        }
#endif
 
        private static string root => Path.Combine(Application.persistentDataPath, "PlayerData");
        public static bool HasFile(string path)
        {
            path = Utilities.ValidAndNormalizePath(path);
            string fullPath = Path.Combine(root, path);
            return File.Exists(fullPath);
        }
        public static void SaveString(string path, string data, string password = null)
        {
            path = Utilities.ValidAndNormalizePath(path);
            if (password != null) data = AES.Encrypt(data, password);
            string fullPath = Path.Combine(root, path);
            string tempPath = Path.Combine(root, $"{path}.tmp");
            string saveDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(saveDir) && !Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
            File.WriteAllText(tempPath, data);
            if (File.Exists(fullPath)) File.Replace(tempPath, fullPath, null);
            else File.Move(tempPath, fullPath);
        }

        public static string LoadString(string path, string password = null,  string defaultValue = null)
        {
            path = Utilities.ValidAndNormalizePath(path);
            string fullPath = Path.Combine(root, path);
            if (!File.Exists(fullPath)) return defaultValue;
            string text = File.ReadAllText(fullPath);
            if (password != null) text = AES.Decrypt(text, password);
            return text;
        }

        public static void Save<T>(string path, T data, string password = null)
        {
            string jsonData = JsonSerialization.ToJson(data);
            SaveString(path, jsonData, password);
        }

        public static T Load<T>(string path, string password = null, T defaultValue = default)
        {
            string text = LoadString(path, password);
            return text == null ? defaultValue : JsonSerialization.FromJson<T>(text);
        }

        public static bool DeleteFile(string path)
        {
            path = Utilities.ValidAndNormalizePath(path);
            string fullPath = Path.Combine(root, path);
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }

        public static bool DeleteDirectory(string path)
        {
            path = Utilities.ValidAndNormalizePath(path);
            string fullPath = Path.Combine(root, path);
            if (!Directory.Exists(fullPath)) return false;
            Directory.Delete(fullPath, true);
            return true;
        }

        public static void DeleteAll()
        {
            if (!Directory.Exists(root)) return;
            Directory.Delete(root, true);
        }
    }
}