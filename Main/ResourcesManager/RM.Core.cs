using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;

namespace UnityInterface
{
    /// <summary>
    /// Manager assets from base game or loaded.
    /// </summary>
    public static partial class ResourcesManager
    {
        static Dictionary<Type, Dictionary<string, Object>> loadedAssets = new Dictionary<Type, Dictionary<string, Object>>();
        internal static Dictionary<Type, IAssetLoader<Object>> assetLoaders = new Dictionary<Type, IAssetLoader<Object>>();
        public static Transform prefabParent { get; internal set; }
        internal static void Log(LogLevel logLevel, string log) => UnityInterfacePlugin.assetLogger?.Log(logLevel, log);
        public static void LoadFromPath<T>(string path) where T : Object => LoadFromPath(path, typeof(T));
        internal static void LoadFromPath(string path, Type type)
        {
            if (!assetLoaders.ContainsKey(type) || assetLoaders[type] == null)
            {
                Debug.LogWarning($"Loader [{type.Name}] wasn't found! Please add it on AssetManager.AddLoader method during YOUR Awake Method!");
                return;
            }
            try
            {
                Object a = assetLoaders[type].Load(path);
                if (a)
                {
                    Add(a);
                }
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, $"[{type.Name}] {path} loaded Failed! Exception:{e}");
            }
        }
        public static void Add<T>(T asset) where T : Object
        {
            Type type = asset.GetType();
            if (!loadedAssets.ContainsKey(type))
            {
                loadedAssets.Add(type, new Dictionary<string, Object>());
            }
            if (!loadedAssets[type].ContainsKey(asset.name))
            {
                loadedAssets[type].Add(asset.name, asset);
                Log(LogLevel.Info, $"[{type.Name}] [{asset.GetInstanceID()}] {asset.name} was addend!");
            }
            else
            {
                Log(LogLevel.Warning, $"[{type.Name}] [{asset.GetInstanceID()}] {asset.name} Only supports unique names for each type! But you have copies!");
            }
        }
        internal static void AddLoader(this Type assetType, IAssetLoader<Object> loader)
        {
            if (!assetLoaders.ContainsKey(assetType))
            {
                assetLoaders.Add(assetType, loader);
            }
            else
            {
                assetLoaders[assetType] = loader;
            }
            Log(LogLevel.Info, $"[{assetType.Name}] Loader:{loader.GetType().Name} was addend into system!");
        }

        public static void SetAsPrefab(GameObject prefab) => prefab.transform.SetParent(prefabParent);

        public static Object[] Get(Type type)
        {
            List<Object> result = new List<Object>(Resources.FindObjectsOfTypeAll(type));
            if (loadedAssets.TryGetValue(type, out var val))
            {
                result.AddRange(val.Values);
            }
            if (typeof(Component).IsAssignableFrom(type))
            {
                result.AddRange(prefabParent.GetComponentsInChildren(type, true));
            }
            return result.ToArray().UniqueCheck();
        }
        public static T[] Get<T>() where T : Object => Get(typeof(T)).OfType<T>().ToArray();
        public static Object Get(Type type, string name) => Get(type).FirstOrDefault(a => a.name == name);
        public static T Get<T>(string name) where T : Object => (T)Get(typeof(T), name);
    }
}