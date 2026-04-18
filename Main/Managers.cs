using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityInterface
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SkipScanning : Attribute
    {

    }
    public static class PluginManager
    {
        #region"Done"
        private static bool CheckDirectory(string path, BaseUnityPlugin requester) => CheckDirectory(path, queueToGenerate.Contains(requester));
        internal static List<Type> types = new List<Type>();
        static List<Type> foundedScriptableObjectTypes = new List<Type>();
        /// <summary>
        ///Shortcut I built! To Bind setting quickly. Nothing special.
        /// </summary>
        /// <typeparam name="T">settings var "T"ype</typeparam>
        /// <param name="plugin">Config file's owner</param>
        /// <param name="name"></param>
        /// <param name="defualtVal"></param>
        /// <param name="summaries"></param>
        /// <returns></returns>
        public static T QuickOption<T>(this BaseUnityPlugin plugin, string name, T defualtVal, string summaries = "") => plugin.Config.Bind(new ConfigDefinition(summaries, name), defualtVal).Value;
        /// <summary>
        /// Return the folder: Project_{GUID} full path.
        /// </summary>
        /// <param name="GUID">Customized GUID</param>
        /// <returns></returns>
        public static string GetProjectFolder(string GUID) => Path.Combine(Application.streamingAssetsPath, "Projects", $"Project_{GUID}");
        /// <summary>
        /// Return the folder: Project_{plugin.Info.Metadata.GUID} full path.
        /// </summary>
        /// <param name="plugin">Folder's owner</param>
        /// <returns></returns>
        public static string GetProjectFolder(BaseUnityPlugin plugin) => GetProjectFolder(plugin.Info.Metadata.GUID);

        internal static List<BaseUnityPlugin> queueToLoad = new List<BaseUnityPlugin>(), queueToGenerate = new List<BaseUnityPlugin>(), queueRequiredReference = new List<BaseUnityPlugin>();
        public static void AddToLoad(this BaseUnityPlugin plugin)
        {
            if (!queueToLoad.Contains(plugin))
            {
                queueToLoad.Add(plugin);
            }
        }
        public static void GenerateAssetFolders(this BaseUnityPlugin plugin)
        {
            if (queueToLoad.Contains(plugin) && !queueToGenerate.Contains(plugin))
            {
                queueToGenerate.Add(plugin);
            }
            else
            {
                Debug.Log($"{plugin.Info.Metadata.Name}, You must AddToQueueToLoad and then you be allowed to GenerateAssetFolders");
            }
        }
        public static void GenerateReferenceAssets(this BaseUnityPlugin plugin)
        {
            if (queueToGenerate.Contains(plugin) && !queueRequiredReference.Contains(plugin))
            {
                queueRequiredReference.Add(plugin);
            }
            else
            {
                Debug.Log($"{plugin.Info.Metadata.Name}, You must GenerateAssetFolders and then you be allowed to GenerateAssetFolders");
            }
        }
        internal static void Log(string log)
        {
            if (PluginCore.pluginManagerLog)
            {
                Debug.Log(log);
            }
        }
        private static bool CheckDirectory(string path, bool generateFolder)
        {
            if (!Directory.Exists(path))
            {
                if (generateFolder)
                {
                    Log($"Path:{path} doesn't exists! Creating a new one!");
                    Directory.CreateDirectory(path);
                    return true;
                }
                return false;
            }
            return true;
        }
        internal static void InjectPluginDLLs()
        {
            AddType(AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToArray().UniqueCheck());
            Log($"Founded total types count: {types.Count} and ScriptableObject types count: {foundedScriptableObjectTypes.Count}.");
        }
        public static void AddType<T>() => AddType(typeof(T));
        public static void AddType(params Type[] typez)
        {
            foreach (var a in typez)
            {
                if (types.Contains(a))
                {
                    Log($"{a} was addend!");
                    continue;
                }
                if (a.ContainsAttribute(typeof(SkipScanning)))
                {
                    continue;
                }
                types.Add(a);
                if (!a.IsAbstract)
                {
                    if (typeof(ScriptableObject).IsAssignableFrom(a))
                    {
                        foundedScriptableObjectTypes.Add(a);
                    }
                    if (typeof(IAssetLoader<>).ContainsInterface(a))
                    {
                        a.GetConstGenericedType(typeof(IAssetLoader<>)).AddLoader((IAssetLoader<Object>)Activator.CreateInstance(a));
                    }
                }
            }
        }
        private static void LoadSpecificedAssets(BaseUnityPlugin plugin)
        {
            string pathTemp;
            foreach (var itm in AssetManager.assetLoaders.Keys)
            {
                pathTemp = Path.Combine(GetProjectFolder(plugin), itm.Name);
                if (CheckDirectory(pathTemp, plugin))
                {
                    Collections.GetAllFiles(pathTemp).ToList().ForEach(c => AssetManager.LoadFromPath(c, itm));
                }
            }
        }
        internal static void LoadAllPlugins()
        {
            CheckDirectory(Path.Combine(Application.streamingAssetsPath, "Projects"), true);
            queueToLoad.ForEach(a => LoadSpecificedAssets(a));
            queueToLoad.ForEach(a => PrepareEmptyScriptableObjects(a));
            queueToLoad.ForEach(a => LoadScriptableObjects(a));
        }
        #endregion
        #region"ScriptableObject"
        private static void PrepareEmptyScriptableObjects(BaseUnityPlugin plugin)
        {
            string startPath = Path.Combine(GetProjectFolder(plugin), "ScriptableObject"), curPath, templatePath;
            ScriptableObject scriptableObject;
            if (CheckDirectory(startPath, plugin))
            {
                foreach (var itmType in foundedScriptableObjectTypes)
                {
                    curPath = Path.Combine(startPath, itmType.Name);
                    if (CheckDirectory(curPath, plugin))
                    {
                        templatePath = Path.Combine(curPath, "Template.json");
                        if (queueToGenerate.Contains(plugin) && !File.Exists(templatePath))
                        {
                            scriptableObject = ScriptableObject.CreateInstance(itmType);
                            File.WriteAllText(templatePath, AssetManager.ReplaceInstanceIDs(itmType, JsonUtility.ToJson(scriptableObject), true));
                        }

                        templatePath = Path.Combine(curPath, "References");
                        if (queueRequiredReference.Contains(plugin) && !Directory.Exists(templatePath))
                        {
                            string s;
                            Directory.CreateDirectory(templatePath);
                            foreach (var itm in Resources.FindObjectsOfTypeAll(itmType))
                            {
                                s = Path.Combine(templatePath, $"Reference_{itm.name}.json");
                                File.WriteAllText(s, AssetManager.ReplaceInstanceIDs(itmType, JsonUtility.ToJson(itm), true));
                            }
                        }

                        foreach (var itmPath in Collections.GetAllFiles(curPath, ".json").Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
                        {
                            scriptableObject = ScriptableObject.CreateInstance(itmType);
                            scriptableObject.name = Path.GetFileNameWithoutExtension(itmPath);
                            AssetManager.AddAsset(scriptableObject);
                        }
                    }
                }
            }
        }

        private static void LoadScriptableObjects(BaseUnityPlugin plugin)
        {
            string startPath = Path.Combine(GetProjectFolder(plugin), "ScriptableObject"), curPath;
            if (CheckDirectory(startPath, plugin))
            {
                foreach (var itmType in foundedScriptableObjectTypes)
                {
                    curPath = Path.Combine(startPath, itmType.Name);
                    if (CheckDirectory(curPath, plugin))
                    {
                        foreach (var itmPath in Collections.GetAllFiles(curPath, ".json").Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
                        {
                            JsonUtility.FromJsonOverwrite(AssetManager.ReplaceInstanceIDs(itmType, File.ReadAllText(itmPath), false), AssetManager.loadedAssets[itmType][Path.GetFileNameWithoutExtension(itmPath)]);
                        }
                    }
                }
            }
        }
        #endregion
    }

    [HarmonyPatch]
    public static class AssetManager
    {
        public static Dictionary<Type, Dictionary<string, Object>> loadedAssets = new Dictionary<Type, Dictionary<string, Object>>();
        internal static Dictionary<Type, IAssetLoader<Object>> assetLoaders = new Dictionary<Type, IAssetLoader<Object>>();
        public static Transform prefabParent { get; internal set; }
        internal static void Log(string log)
        {
            if (PluginCore.assetSystemLog)
            {
                Debug.Log(log);
            }
        }
        public static void LoadFromPath<T>(string path) where T : Object => LoadFromPath(path, typeof(T));
        internal static void LoadFromPath(string path, Type type)
        {
            if (!assetLoaders.ContainsKey(type) || assetLoaders[type] == null)
            {
                Debug.LogWarning($"Type: {type.Name} of loader wasn't found! Please add it on AssetManager.AddLoader method during YOUR Awake Method!");
                return;
            }
            try
            {
                Object a = assetLoaders[type].LoadAsset(path);
                if (a)
                {
                    AddAsset(a);
                }
            }
            catch (Exception e)
            {
                Log($"FAILED! Path:{path} Exception:{e}");
            }
        }
        public static void AddAsset<T>(T asset) where T : Object
        {
            Type type = asset.GetType();
            if (!loadedAssets.ContainsKey(type))
            {
                loadedAssets.Add(type, new Dictionary<string, Object>());
            }
            if (!loadedAssets[type].ContainsKey(asset.name))
            {
                loadedAssets[type].Add(asset.name, asset);
                Log($"{type.Name}_{asset.name}_{asset.GetInstanceID()} was addend!");
            }
            else
            {
                Log($"AssetManager only supports unique names for each type! Name:{asset.name}");
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
            Log($"Type:{assetType.Name} of loader:{loader.GetType().Name} was addend into system!");
        }
        public static void ReplaceAsset<T>(T asset) where T : Object
        {
            Type type = asset.GetType();
            if (!loadedAssets.ContainsKey(type))
            {
                loadedAssets.Add(type, new Dictionary<string, Object>());
            }
            if (loadedAssets[type].ContainsKey(asset.name))
            {
                loadedAssets[type][asset.name] = asset;
                Log($"{type.Name}_{asset.name}_{asset.GetInstanceID()} was replaced!");
            }
            else
            {
                Log($"AssetManager asked you that have you addend it before! Name:{asset.name}");
            }
        }
        public static void SetAsPrefab(GameObject prefab) => prefab.transform.SetParent(prefabParent);
        internal static bool serMod;
        public static string ReplaceInstanceIDs(Type type, string json, bool ser)
        {
            serMod = ser;
            return ReplaceToken(type, JToken.Parse(json)).ToString(Formatting.Indented);
        }

        private static JToken ReplaceToken(Type type, JToken token)
        {
            Type fieldType, objType = null;
            if (token is JObject obj)
            {
                int id;
                string propVal;
                foreach (var prop in obj.Properties())
                {
                    propVal = prop.Value.ToString();
                    fieldType = type.GetField(prop.Name)?.FieldType;

                    if (prop.Name == "m_FileID")
                    {
                        objType = typeof(Object).IsAssignableFrom(type) ? type : fieldType;

                        if (objType != null)
                        {
                            if (serMod)
                            {
                                id = int.Parse(propVal);
                                prop.Value = (id == 0) ? "null" : Resources.InstanceIDToObject(id).name;
                            }
                            else
                            {
                                prop.Value = (propVal == "null") ? 0 : Resources.Load(propVal, type).GetInstanceID();
                            }
                        }
                    }

                    if (fieldType != null)
                    {
                        if (fieldType.IsEnum)
                        {
                            if (serMod)
                            {
                                id = int.Parse(propVal);
                                prop.Value = Enum.ToObject(fieldType, id).ToString();
                            }
                            else
                            {
                                prop.Value = (int)propVal.ToEnum(fieldType);
                            }
                        }
                        else
                        {
                            ReplaceToken(fieldType, prop.Value);
                        }
                    }
                }

            }
            else if (token is JArray arr)
            {
                fieldType = type.GetElementType();

                if (fieldType == null && type.IsGenericType)
                {
                    fieldType = type.GetGenericArguments()[0];
                    Debug.Log(fieldType.FullName);
                }

                if (fieldType != null)
                {
                    foreach (var item in arr)
                    {
                        ReplaceToken(fieldType, item);
                    }
                }
            }
            return token;
        }
        #region "Built-in Assets Loading"
        public static Texture2D GetTexture2DFromPathSimple(string path)
        {
            Texture2D t = new Texture2D(1, 1);
            t.name = Path.GetFileNameWithoutExtension(path);
            if (t.LoadImage(File.ReadAllBytes(path)))
            {
                return t;
            }
            Debug.LogWarning("Could not Get texture from path");
            return null;
        }
        public static Sprite Texture2DToSpriteSimple(Texture2D texture)
        {
            Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            s.name = texture.name;
            return s;
        }
        public static AudioClip GetAudioClipFromPath(string path, AudioType type = AudioType.UNKNOWN)
        {
            AudioClip clip;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, type))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone) { }
                clip = DownloadHandlerAudioClip.GetContent(request);
            }
            clip.name = Path.GetFileNameWithoutExtension(path);
            return clip;
        }
        [Obsolete("I'm sure that nobody will save a mesh asset in JSON format!")]
        public static Mesh GetMeshFromPath(string path)
        {
            Mesh mesh = JsonConvert.DeserializeObject<Mesh>(File.ReadAllText(path));
            mesh.name = Path.GetFileNameWithoutExtension(path);
            return mesh;
        }
        public static AssetBundle GetAssetBundleFromPath(string path)
        {
            if (Path.GetExtension(path) == ".manifest")
            {
                return null;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(path);
            foreach (var item in ab.LoadAllAssets())
            {
                AddAsset(item);
            }
            return ab;
        }
        #endregion
        #region "Resource Patch"
        [HarmonyPatch(typeof(Resources), "FindObjectsOfTypeAll", typeof(Type)), HarmonyPostfix]
        static void PostFix(Type type, ref Object[] __result)
        {
            if (loadedAssets.ContainsKey(type))
            {
                __result = __result.AddAs(loadedAssets[type].Values.ToArray());
            }
        }
        [HarmonyPatch(typeof(Resources), "Load", typeof(string), typeof(Type)), HarmonyPostfix]
        static void PostFix(string path, Type systemTypeInstance, ref Object __result)
        {
            if (__result == null)
            {
                __result = Resources.LoadAll(path, systemTypeInstance).FirstOrDefault();
            }
        }
        [HarmonyPatch(typeof(Resources), "LoadAll", typeof(string), typeof(Type)), HarmonyPostfix]
        static void PostFix0(string path, Type systemTypeInstance, ref Object[] __result) => __result = Resources.FindObjectsOfTypeAll(systemTypeInstance).Where(a => a.name.Contains(path)).ToArray();
        #endregion
        #region "Enum"     
        private static Dictionary<Type, List<string>> extraEnums = new Dictionary<Type, List<string>>();
        private static Dictionary<Type, int> extraEnumsCount = new Dictionary<Type, int>();
        private static bool sampleMode;
        public static T ToEnum<T>(this string name) where T : Enum => (T)name.ToEnum(typeof(T));
        public static object ToEnum(this string name, Type type)
        {
            if (Enum.IsDefined(type, name))
            {
                return Enum.Parse(type, name, true);
            }
            if (!extraEnums.ContainsKey(type))
            {
                extraEnums.Add(type, new List<string>());
                sampleMode = true;
                extraEnumsCount.Add(type, Enum.GetNames(type).Length);
                sampleMode = false;
            }
            if (!extraEnums[type].Contains(name))
            {
                extraEnums[type].Add(name);
            }
            return extraEnumsCount[type] + extraEnums[type].IndexOf(name);
        }
        [HarmonyPatch(typeof(Enum), "GetNames"), HarmonyPostfix]
        private static void Postfix_GetNames(Type enumType, ref string[] __result)
        {
            if (sampleMode || !extraEnums.ContainsKey(enumType))
            {
                return;
            }
            __result = __result.AddAs(extraEnums[enumType].ToArray());
        }
        [HarmonyPatch(typeof(Enum), "GetName"), HarmonyPostfix]
        private static void Postfix_GetName(Type enumType, object value, ref string __result)
        {
            int v = (int)value;
            if (sampleMode || !extraEnums.ContainsKey(enumType) || v < extraEnumsCount[enumType])
            {
                return;
            }
            __result = extraEnums[enumType][v - extraEnumsCount[enumType]];
        }
        #endregion
    }
    public interface IAssetLoader<out T> where T : Object
    {
        T LoadAsset(string path);
    }
}
#region"Asset Loaders"
namespace UnityInterface.AssetLoaders
{
    public class Texture2DLoader : IAssetLoader<Texture2D>
    {
        public Texture2D LoadAsset(string path) => LoadAssetWithMetadata(path, path.GetMetadata(new Texture2DMetadata()));
        public static Texture2D LoadAssetWithMetadata(string path, Texture2DMetadata metadata)
        {
            Texture2D texture = new Texture2D(1, 1);

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.wrapMode = metadata.wrapMode;
            texture.filterMode = metadata.filterMode;

            if (texture.LoadImage(File.ReadAllBytes(path), metadata.readable))
            {
                return texture;
            }

            Debug.LogWarning("Could not Get texture from path");
            return null;
        }
        [Serializable]
        public class Texture2DMetadata
        {
            public bool readable;
            public TextureWrapMode wrapMode;
            public FilterMode filterMode;
        }
    }
    public class AudioClipLoader : IAssetLoader<AudioClip>
    {
        public AudioClip LoadAsset(string path) => AssetManager.GetAudioClipFromPath(path);
    }
    public class SpriteLoader : IAssetLoader<Sprite>
    {
        public Sprite LoadAsset(string path)
        {
            if (Path.GetExtension(path) == ".meta")
            {
                return null;
            }

            Texture2D texture = AssetManager.GetTexture2DFromPathSimple(path);
            SpriteMetadata metadata0 = path.GetMetadata(new SpriteMetadata()
            {
                rect = new Rect(0, 0, texture.width, texture.height),
                pivot = Vector2.one * 0.5f,
                pixelPerUnit = 100
            });

            Sprite s = Sprite.Create(Texture2DLoader.LoadAssetWithMetadata(path, metadata0), metadata0.rect, metadata0.pivot, metadata0.pixelPerUnit);
            s.name = texture.name;
            return s;
        }
        [Serializable]
        public class SpriteMetadata : Texture2DLoader.Texture2DMetadata
        {
            public Rect rect;
            public Vector2 pivot;
            public float pixelPerUnit;
        }
    }
    public class MeshLoader : IAssetLoader<Mesh>
    {
        public Mesh LoadAsset(string path) => AssetManager.GetMeshFromPath(path);
    }
    public class AssetBundleLoader : IAssetLoader<AssetBundle>
    {
        public AssetBundle LoadAsset(string path) => AssetManager.GetAssetBundleFromPath(path);
    }
}
#endregion
