using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace UnityInterface
{
    public static class UnityManager
    {
        public static List<BaseUnityPlugin> Plugins => plugins;
        private static List<BaseUnityPlugin> plugins = new List<BaseUnityPlugin>();
        public static void SetupPlugin(BaseUnityPlugin plugin, PluginConfig cfg = default)
        {
            plugins.Add(plugin);
            string s = AssetManager.GetProjectFolder(plugin);
            if (cfg.autoBuild)
            {
                if (!Directory.Exists(s))
                {
                    Directory.CreateDirectory(s);
                }
                string s1 = Path.Combine(s, typeof(AudioClip).Name);
                string[] pl = null;
                BuildDir(s1, out pl);
                for (int i = 0; i < pl.Length; i++)
                {
                    AssetManager.AddExtraAsset(AssetManager.GetAudioClipFromPath(pl[i]));
                }
                s1 = Path.Combine(s, typeof(Texture2D).Name);
                BuildDir(s1, out pl);
                for (int i = 0; i < pl.Length; i++)
                {
                    AssetManager.AddExtraAsset(AssetManager.GetTexture2DFromPath(pl[i]));
                }
                s1 = Path.Combine(s, typeof(Sprite).Name);
                BuildDir(s1, out pl);
                for (int i = 0; i < pl.Length; i++)
                {
                    AssetManager.AddExtraAsset(AssetManager.Texture2DToSprite(AssetManager.GetTexture2DFromPath(pl[i])));
                }
                s1 = Path.Combine(s, typeof(Mesh).Name);
                BuildDir(s1, out pl);
                for (int i = 0; i < pl.Length; i++)
                {
                    AssetManager.AddExtraAsset(AssetManager.GetMeshFromPath(pl[i]));
                }
                s1 = Path.Combine(s, typeof(AssetBundle).Name);
                BuildDir(s1, out pl);
                for (int i = 0; i < pl.Length; i++)
                {
                    AssetManager.AddExtraAsset(AssetBundle.LoadFromFile(pl[i]));
                }
            }
        }
        internal static void BuildDir(string path, out string[] array)
        {
            array = Directory.GetFiles(path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public struct PluginConfig
        {
            public bool autoBuild;
            public bool autoLoad;
        }
    }
    public static class AssetManager
    {
        public static Dictionary<Type, Dictionary<string, Object>> assets = new Dictionary<Type, Dictionary<string, Object>>();
        public static string GetProjectFolder(BaseUnityPlugin plugin)
        {
            return Path.Combine(Application.streamingAssetsPath, "Projects", "Project_" + plugin.Info.Metadata.GUID);
        }
        public static void ReloadBuiltIn()
        {
            Object[] obj = Resources.FindObjectsOfTypeAll<Object>();
            foreach (var item in obj)
            {
                AddAsset(item);
            }
        }
        public static T Load<T>(string name) where T : Object
        {
            if (assets.ContainsKey(typeof(T)))
            {
                if (assets[typeof(T)].ContainsKey(name))
                {
                    return assets[typeof(T)][name] as T;
                }
            }
            return null;
        }
        public static void AddExtraAsset(Object o)
        {
            AddAsset(o);
        }
        private static void AddAsset(Object obj)
        {
            Type type = obj.GetType();
            if (!assets.ContainsKey(type))
            {
                assets.Add(type, new Dictionary<string, Object>());
            }
            if (!assets[type].ContainsKey(obj.name))
            {
                assets[type].Add(obj.name, obj);
            }
            else
            {
                assets[type][obj.name] = obj;
            }
        }
        public static void ForeachAllObjects<T>(OnSthOutputInSingle happened, params string[] Exclude) where T : Object
        {
            Object[] os = GetGameAssetsFromType<T>();
            for (int i = 0; i < os.Length; i++)
            {
                if (!Exclude.Contains(os[i].name))
                {
                    happened.Invoke(os[i]);
                }
            }
            Debug.LogWarning("No global value found! Return null!");
        }
        public static List<object> GetAllObjectWithoutExclude<T>(params string[] Exclude) where T : Object
        {
            List<object> ret = null;
            ForeachAllObjects<T>((object obj) =>
            {
                try
                {
                    if (!Exclude.Contains(((Object)obj).name))
                    {
                        ret.Add(obj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("GetAllObjectWithoutExclude failed! Log: " + ex);
                }
            }, Exclude);
            return ret;
        }
        public static object GetGlobalValue<T>(string name, params string[] Exclude) where T : Object
        {
            object ret = null;
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       ret = obj.GetValue(name);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("GetGlobalValue failed! Log: " + ex);
                   }
               }, Exclude);
            return ret;
        }
        public static void SetGlobalValue<T>(string name, object newValue, params string[] Exclude) where T : Object
        {
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       obj.SetValue(name, newValue);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("SetGlobalValue failed! Log: " + ex);
                   }
               }, Exclude);
        }
        public static void AddGlobalValuesToList<T, O>(string name, List<O> valuesToAdd, params string[] Exclude) where T : Object
        {
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       (obj.GetValue(name) as List<O>).AddRange(valuesToAdd);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("AddGlobalValuesToList failed! Log: " + ex);
                   }
               }, Exclude);
        }
        public static void AddGlobalValueToList<T, O>(string name, O valueToAdd, params string[] Exclude) where T : Object
        {
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       (obj.GetValue(name) as List<O>).Add(valueToAdd);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("AddGlobalValueToList failed! Log: " + ex);
                   }
               }, Exclude);
        }

        public static void AddGlobalValuesToArray<T, O>(string name, List<O> valuesToAdd, params string[] Exclude) where T : Object
        {
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       O[] ary = obj.GetValue(name) as O[];
                       Collections.Add(ref ary, valuesToAdd.ToArray());
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("AddGlobalValuesToArray failed! Log: " + ex);
                   }
               }, Exclude);
        }
        public static void AddGlobalValueToArray<T, O>(string name, O valueToAdd, params string[] Exclude) where T : Object
        {
            ForeachAllObjects<T>((object obj) =>
               {
                   try
                   {
                       O[] ary = obj.GetValue(name) as O[];
                       Collections.Add(ref ary, valueToAdd);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning("AddGlobalValueToArray failed! Log: " + ex);
                   }
               }, Exclude);
        }
        public static T GetGameAssetFromName<T>(string name) where T : Object
        {
            foreach (var item in GetGameAssetsFromType<T>())
            {
                if (item.name == name)
                {
                    return (T)item;
                }
            }
            return default;
        }
        public static T[] GetGameAssetsFromType<T>() where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>();
        }
        public static T GetGameAssetFromType<T>() where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>()[0];
        }
        public static Texture2D GetTexture2DFromPath(string path)
        {
            Texture2D t = new Texture2D(256, 256);
            if (t.LoadImage(File.ReadAllBytes(path)))
            {
                return t;
            }
            Debug.LogWarning("Could not Get texture from path");
            return null;
        }
        public static Sprite Texture2DToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        public static AudioClip GetAudioClipFromPath(string path)
        {
            IEnumerator ie = Interal_GetAudioClipFromPath(path);
            while (ie.MoveNext())
            {
            }
            return clip;
        }
        private static AudioClip clip;
        private static IEnumerator Interal_GetAudioClipFromPath(string path)
        {
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
            yield return request.SendWebRequest();
            clip = DownloadHandlerAudioClip.GetContent(request);
        }
        public static Mesh GetMeshFromPath(string path)
        {
            return JsonConvert.DeserializeObject<Mesh>(File.ReadAllText(path));
        }
    }
}
