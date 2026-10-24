using System;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace GameTools.SaveSystem
{
    public partial class DataContainer
    {
        private static readonly Dictionary<string, DataContainer> containers = new();
        public static readonly DataContainer defaultContainer = Get("default");

        /// <summary> 获取或者创建container，并且进行密码一致性检查</summary>
        public static DataContainer Get(string path, string password = null)
        {
            path = Utilities.ValidAndNormalizePath(path);
            if (containers.TryGetValue(path, out var container))
            {
                if (container.password == password) return container;
                throw new InvalidOperationException($"The password for container: '{path}' is not consistent.");
            }
            container = new DataContainer(path, password);
            containers[path] = container;
            return container;
        }
        
        /// <summary> 在不使用密码的情况下获取已存在的container </summary>
        public static DataContainer GetExist(string path)
        { 
            path = Utilities.ValidAndNormalizePath(path);
            return containers.GetValueOrDefault(path);
        }

        public static void SaveAll()
        {
            foreach (var item in containers.Values)
            {
                try
                {
                    item.Save();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        static DataContainer()
        {
            Application.focusChanged += (hasFocus) =>
            {
                if (!hasFocus) SaveAll();
            };
            Application.quitting += SaveAll;
        }
    }

    public partial class DataContainer
    {
        private Dictionary<(Type, string), object> createdBindableSource = new();
        private Dictionary<string, string> _cache;
        private Dictionary<string, string> cache
        {
            get
            {
                LoadToCache();
                return _cache;
            }
        }
        public bool isLoaded => _cache != null;
        public readonly string path;
        public readonly string password;
        public bool isDirty { get; private set; } = false;
        public ICollection<string> Keys => cache.Keys;
        public int Count => cache.Count;
        
        private DataContainer(string path, string password = null)
        {
            this.password = password;
            this.path = path;
        }
     
        public void LoadToCache()
        {
            if (_cache != null) return;
            _cache = SaveManager.Load<Dictionary<string, string>>(path, password);
            if (_cache == null) _cache = new(); 
        }

        public void Save(bool releaseCache = false)
        {
            if (isDirty && _cache != null)
            {
                SaveManager.Save(path, _cache, password);
                isDirty = false;
            }
            if (releaseCache) _cache = null;
        }

        


        public T Get<T>(string key, T defaultValue = default)
        {
            if (cache.TryGetValue(key, out string data))
            {
                return JsonSerialization.FromJson<T>(data);
            }
            return defaultValue;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (cache.TryGetValue(key, out string data))
            {
                value = JsonSerialization.FromJson<T>(data);
                return true;
            }
            value = default;
            return false;
        }
        
        
        public void Set<T>(string key, T data)
        {
            string jsonData = JsonSerialization.ToJson(data);
            if (cache.TryGetValue(key, out var oldData))
            {
                if (oldData == jsonData) { return; }
            }
            cache[key] = jsonData;
            isDirty = true;
        }
        
        public bool ContainsKey(string key) => cache.ContainsKey(key);
        public bool Remove(string key)
        {
            bool isRemoved = cache.Remove(key);
            if (isRemoved) { isDirty = true; }
            return isRemoved;
        }
        public void Clear()
        {
            if (cache.Count == 0) { return; }
            cache.Clear();
            isDirty = true;
        }
    }
}

/*public partial class DataContainer
    {
        private struct Data
        {
            public Data(string value, string type)
            {
                this.value = value;
                this.type = type;
            }
            public string value;
            public string type;
        }


        private Dictionary<string, Dictionary<string, string>> _cache = null;
        private Dictionary<string, Dictionary<string, string>> cache
        {
            get
            {
                LoadToCache();
                return _cache;
            }
        }
        public bool isLoaded => _cache != null;
        public readonly string containerName;
        public readonly bool encryptFile;
        private bool isDirty = false;

        private DataContainer(string name, bool encryptFile)
        {
            this.encryptFile = encryptFile;
            containerName = name;
        }

        public void LoadToCache()
        {
            if (_cache != null)
            {
                return;
            }
            _cache = SaveManager.Load<Dictionary<string, Dictionary<string, string>>>(containerName, encryptFile, null);
            if (_cache == null)
            {
                _cache = new();
            }
        }
        public void Save(bool releaseCache = false)
        {
            if (isDirty)
            {
                SaveManager.Save(containerName, _cache, encryptFile);
                isDirty = false;
            }
            if (releaseCache)
            {
                _cache = null;
            }
        }

        private Dictionary<string, string> GetDict<T>(bool createNew)
        {
            var key = typeof(T).FullName;
            Dictionary<string, string> dict;
            if (!cache.TryGetValue(key, out dict) || dict == null)
            {
                if (!createNew)
                {
                    return null;
                }
                dict = new Dictionary<string, string>();
                cache[key] = dict;
            }
            return dict;
        }

        private bool RemoveDict<T>()
        {
            var key = typeof(T).FullName;
            return cache.Remove(key);
        }

        public void Set<T>(string key, T data)
        {
            string jsonData = JsonSerialization.ToJson(data);
            var dict = GetDict<T>(true);

            if (dict.TryGetValue(key, out var oldValue) && oldValue == jsonData)
                return;

            dict[key] = jsonData;
            isDirty = true;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            var dict = GetDict<T>(false);
            if (dict != null && dict.TryGetValue(key, out string jsonData))
            {
                return JsonSerialization.FromJson<T>(jsonData);
            }
            return defaultValue;
        }

        public bool HasKey<T>(string key)
        {
            var dict = GetDict<T>(false);
            if (dict == null) { return false; }
            return dict.ContainsKey(key);
        }

        public bool Remove<T>(string key)
        {
            var dict = GetDict<T>(false);
            if (dict == null) { return false; }
            bool isRemoved = dict.Remove(key);
            if (isRemoved && dict.Count == 0)
            {
                RemoveDict<T>();
            }
             if (isRemoved) { 
                isDirty = true;
            }
            return isRemoved;
        }

        public void Clear()
        {
            cache.Clear();
            isDirty = true;
        }


    }*/
/*using SimpleEncryption;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace GameTools.SaveSystem
{
    public class DataContainer
    {
        private static List<DataContainer> containers = new();
        public static DataContainer Get(string name, bool encryptFile = true)
        {
            SaveManager.ValidName(name);
            DataContainer container = containers.Find(x => x.containerName == name && x.encryptFile == encryptFile);
            if (container == null)
            {
                container = new DataContainer(name, encryptFile);
                containers.Add(container);
            }
            return container;
        }

        private Dictionary<string, Dictionary<string, string>> _cache = null;
        private Dictionary<string, Dictionary<string, string>> cache
        {
            get
            {
                LoadToCache();
                return _cache;
            }
        }
        public bool isLoaded => _cache != null;
        public readonly string containerName;
        public readonly bool encryptFile;
        public bool encryptRAM { get; private set; } = false;
        private readonly string password; 

        private bool _isDirty = false;
        private bool isDirty
        {
            get { return _isDirty; }
            set
            {
                if (_isDirty != value)
                {
                    if (value)
                    {
                        Application.focusChanged += OnFocusChanged;
                        Application.quitting += OnApplicationQuit;
                    }
                    else
                    {
                        Application.focusChanged -= OnFocusChanged;
                        Application.quitting -= OnApplicationQuit;
                    }
                    _isDirty = value;
                }
            }
        }

        public void EnableRAMEncrypt()
        {
            if (encryptRAM) { return; }
            encryptRAM = true;
            if (isLoaded)
            {
                _cache = EncryptData(_cache, password);
            }
        }

        private DataContainer(string name, bool encryptFile)
        {
            this.encryptFile = encryptFile;
            containerName = name;
            password = System.Guid.NewGuid().ToString("N");
        }


        private static Dictionary<string, Dictionary<string, string>> EncryptData(Dictionary<string, Dictionary<string, string>> data, string password)
        {
            var encryptedData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in data)
            {
                if (item.Value == null) { continue; }
                var dict = new Dictionary<string, string>();
                foreach (var kv in item.Value)
                {
                    dict[B64X.Encrypt(kv.Key, password)] = B64X.Encrypt(kv.Value, password);
                }
                encryptedData[item.Key] = dict;
            }
            return encryptedData;
        }
        private static Dictionary<string, Dictionary<string, string>> DecryptData(Dictionary<string, Dictionary<string, string>> data, string password)
        {
            var decryptedData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in data)
            {
                if (item.Value == null) { continue; }
                var dict = new Dictionary<string, string>();
                foreach (var kv in item.Value)
                {
                    dict[B64X.Decrypt(kv.Key, password)] = B64X.Decrypt(kv.Value, password);
                }
                decryptedData[item.Key] = dict;
            }
            return decryptedData;
        }

        public void LoadToCache()
        {
            if (_cache != null)
            {
                return;
            }
            var tempCache = SaveManager.Load<Dictionary<string, Dictionary<string, string>>>(containerName, encryptFile, null);
            if (tempCache == null)
            {
                tempCache = new();
            }
            if (encryptRAM)
            {

                _cache = EncryptData(tempCache, password);
            }
            else
            {
                _cache = tempCache;
            }

        }
        public void Save(bool releaseCache = false)
        {
            if (isDirty)
            {
                var readyToSaveCache = encryptRAM ? DecryptData(cache, password) : cache;
                SaveManager.Save(containerName, readyToSaveCache, encryptFile);
                isDirty = false;
            }
            if (releaseCache)
            {
                _cache = null;
            }
        }

        private Dictionary<string, string> GetDict<T>(bool createNew)
        {
            var key = typeof(T).FullName;
            Dictionary<string, string> dict;
            if (!cache.TryGetValue(key, out dict) || dict == null)
            {
                if (!createNew)
                {
                    return null;
                }
                dict = new Dictionary<string, string>();
                cache[key] = dict;
            }
            return dict;
        }


        private void SetRaw<T>(string key, string value)
        {
            if (encryptRAM)
            {
                GetDict<T>(true)[B64X.Encrypt(key, password)] = B64X.Encrypt(value, password);
            }
            else
            {
                GetDict<T>(true)[key] = value;
            }
            isDirty = true;
        }
        private bool GetRaw<T>(string key, out string value)
        {
            var dict = GetDict<T>(false);
            if (dict == null)
            {
                value = null;
                return false;
            }

            if (encryptRAM)
            {
                key = B64X.Encrypt(key, password);
                if (dict.TryGetValue(key, out string xorData))
                {
                    value = B64X.Decrypt(xorData, password);
                    return true;
                }
            }
            else
            {
                return dict.TryGetValue(key, out value);
            }
            value = null;
            return false;
        }

        public void Set<T>(string key, T data)
        {
            string jsonData = JsonSerialization.ToJson(data);
            SetRaw<T>(key, jsonData);
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (GetRaw<T>(key, out string jsonData))
            {
                return JsonSerialization.FromJson<T>(jsonData);
            }
            return defaultValue;
        }

        public bool HasKey<T>(string key)
        {
            var dict = GetDict<T>(false);
            if (dict == null) { return false; }
            if (encryptRAM)
            {
                key = B64X.Encrypt(key, password);
            }
            return dict.ContainsKey(key);
        }

        public bool Remove<T>(string key)
        {
            var dict = GetDict<T>(false);
            if (dict == null) { return false; }

            if (encryptRAM)
            {
                key = B64X.Encrypt(key, password);
            }
            bool isRemoved = dict.Remove(key);

            if (isRemoved) { 
                isDirty = true;
            }
            return isRemoved;
        }

        public void Clear()
        {
            cache.Clear();
            isDirty = true;
        }



        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus) { Save(); }
        }

    }
}

*/