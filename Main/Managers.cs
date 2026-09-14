using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static UnityInterface.UnityInterface.Assets.Texture2DLoader;
using Object = UnityEngine.Object;

namespace UnityInterface
{
    public class PluginSingleton<T> : BaseUnityPlugin
    {
        public static T Instance { get; private set; }
        protected virtual void Awake() => Instance = GetComponent<T>();
    }
    public class YieldInstructionSingleton<T> : CustomYieldInstruction where T : CustomYieldInstruction, new()
    {
        protected static T instance;
        public static T Instance => instance != null ? instance : (instance = new T());
        public override bool keepWaiting => false;
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class SkipScanning : Attribute
    {

    }
    public static class PluginManager
    {
        #region"Done"
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
        /// Return the folder: Project_{plugin.Info.Metadata.GUID} full path.
        /// </summary>
        /// <param name="plugin">Folder's owner</param>
        /// <returns></returns>
        public static string GetProjectFolder(BaseUnityPlugin plugin) => Path.Combine(Application.streamingAssetsPath, "Projects", $"Project_{plugin.Info.Metadata.GUID}");

        internal static List<BaseUnityPlugin> queueToLoad = new List<BaseUnityPlugin>();
        internal static void Log(string log)
        {
            if (UnityInterfacePlugin.pluginManagerLog)
            {
                Debug.Log(log);
            }
        }
        private static bool CheckDirectory(string path, bool generateFolder = false)
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
        public static string GetAssetedPathAndGenerate<T>(this BaseUnityPlugin plugin, string name)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "Projects");
            CheckDirectory(p, true);
            p = Path.Combine(GetProjectFolder(plugin), typeof(T).Name);
            CheckDirectory(p, true);
            return Path.Combine(p, $"{name}.json");
        }
        internal static void InjectPluginDLLs()
        {
            var array = AppDomain.CurrentDomain.GetAssemblies();
            ResourcesManager.compilerParameters.ReferencedAssemblies.Clear();
            ResourcesManager.compilerParameters.ReferencedAssemblies.AddRange(array.Select(a => a.Location).ToArray().UniqueCheck());
            AddType(array.SelectMany(a => a.GetTypes()).ToArray().UniqueCheck());
            Log($"Founded total types count: {types.Count} and ScriptableObject types count: {foundedScriptableObjectTypes.Count}.");
        }
        public static void AddType<T>() => AddType(typeof(T));
        public static void AddType(params Type[] typez)
        {
            foreach (var a in typez)
            {
                if (a.ContainsAttribute(typeof(SkipScanning)))
                {
                    continue;
                }
                if (!types.AddIfNotExsist(a))
                {
                    Log($"{a} was addend!");
                    continue;
                }
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
        static bool IsThisPlugin(BaseUnityPlugin plugin) => UnityInterfacePlugin.Instance == plugin;
        private static void LoadSpecificedAssets(BaseUnityPlugin plugin)
        {
            string pathTemp;
            bool flag = IsThisPlugin(plugin);
            foreach (var itm in ResourcesManager.assetLoaders.Keys)
            {
                pathTemp = Path.Combine(GetProjectFolder(plugin), itm.Name);
                if (CheckDirectory(pathTemp, flag))
                {
                    Collections.GetAllFiles(pathTemp).ToList().ForEach(c => ResourcesManager.LoadFromPath(c, itm));
                }
            }
        }
        internal static void LoadAllPlugins()
        {
            queueToLoad = Chainloader.PluginInfos.Values.Select(a => a.Instance).ToList();
            CheckDirectory(Path.Combine(Application.streamingAssetsPath, "Projects"), true);
            queueToLoad.ForEach(a => LoadSpecificedAssets(a));
            queueToLoad.ForEach(a => PrepareEmptyScriptableObjects(a));
            queueToLoad.ForEach(a => LoadScriptableObjects(a));
            WaitForPremadeResource.done = true;
        }
        #endregion
        #region"ScriptableObject"
        private static void PrepareEmptyScriptableObjects(BaseUnityPlugin plugin)
        {
            bool flag = IsThisPlugin(plugin);
            string startPath = Path.Combine(GetProjectFolder(plugin), "ScriptableObject"), curPath, templatePath;
            ScriptableObject scriptableObject;
            if (CheckDirectory(startPath, flag))
            {
                foreach (var itmType in foundedScriptableObjectTypes)
                {
                    curPath = Path.Combine(startPath, itmType.Name);
                    if (CheckDirectory(curPath, flag))
                    {
                        if (UnityInterfacePlugin.Instance == plugin)
                        {
                            templatePath = Path.Combine(curPath, "References");
                            if (!Directory.Exists(templatePath))
                            {
                                string s;
                                Directory.CreateDirectory(templatePath);
                                foreach (var itm in Resources.FindObjectsOfTypeAll(itmType))
                                {
                                    s = Path.Combine(templatePath, $"Reference_{itm.name}.json");
                                    File.WriteAllText(s, ResourcesManager.ToJson(itm));
                                }
                            }
                        }

                        foreach (var itmPath in Collections.GetAllFiles(curPath, ".json").Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
                        {
                            try
                            {
                                scriptableObject = ScriptableObject.CreateInstance(itmType);
                                scriptableObject.name = Path.GetFileNameWithoutExtension(itmPath);
                                ResourcesManager.Add(scriptableObject);
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"Type: {itmType.Name} Named: {Path.GetFileNameWithoutExtension(itmPath)} load failed at path: {itmPath}" + e);
                            }
                        }
                    }
                }
            }
        }

        private static void LoadScriptableObjects(BaseUnityPlugin plugin)
        {
            bool flag = IsThisPlugin(plugin);
            string startPath = Path.Combine(GetProjectFolder(plugin), "ScriptableObject"), curPath;
            if (CheckDirectory(startPath, flag))
            {
                foreach (var itmType in foundedScriptableObjectTypes)
                {
                    curPath = Path.Combine(startPath, itmType.Name);
                    if (CheckDirectory(curPath, flag))
                    {
                        foreach (var itmPath in Collections.GetAllFiles(curPath, ".json").Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
                        {
                            try
                            {
                                JsonUtility.FromJsonOverwrite(ResourcesManager.PrepareJsonForOverwrite(File.ReadAllText(itmPath), itmType), ResourcesManager.Get(itmType, Path.GetFileNameWithoutExtension(itmPath)));
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"Type: {itmType.Name} Named: {Path.GetFileNameWithoutExtension(itmPath)} load failed at path: {itmPath}" + e);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
    /// <summary>
    /// Manager assets from base game or loaded.
    /// </summary>
    [HarmonyPatch]
    public static class ResourcesManager
    {
        public static string ToJson(object obj) => ReplaceInstanceIDs(obj.GetType(), JsonUtility.ToJson(obj, true), true);
        public static string PrepareJsonForOverwrite(string json, Type type) => ReplaceInstanceIDs(type, json, false);
        public static object FromJson(string json, Type type) => JsonUtility.FromJson(ReplaceInstanceIDs(type, json, false), type);
        public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(ReplaceInstanceIDs(typeof(T), json, false));
        static Dictionary<Type, Dictionary<string, Object>> loadedAssets = new Dictionary<Type, Dictionary<string, Object>>();
        internal static Dictionary<Type, IAssetLoader<Object>> assetLoaders = new Dictionary<Type, IAssetLoader<Object>>();
        public static Transform prefabParent { get; internal set; }
        internal static void Log(string log)
        {
            if (UnityInterfacePlugin.assetSystemLog)
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
                Object a = assetLoaders[type].Load(path);
                if (a)
                {
                    Add(a);
                }
            }
            catch (Exception e)
            {
                Log($"FAILED! Path:{path} Exception:{e}");
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

        public static Object[] GetAll(Type type, string name) => Get(type).Where(a => a.name == name).ToArray();
        public static T[] GetAll<T>(string name) where T : Object => GetAll(typeof(T), name).OfType<T>().ToArray();

        public static Object Get(Type type, string name) => Get(type).FirstOrDefault(a => a.name == name);
        public static T Get<T>(string name) where T : Object => (T)Get(typeof(T), name);

        internal static bool serializeMod;
        private static string ReplaceInstanceIDs(Type type, string json, bool serialize)
        {
            serializeMod = serialize;
            return ReplaceToken(type, JToken.Parse(json)).ToString(Formatting.Indented);
        }
        private static JToken ReplaceToken(Type type, JToken token)
        {
            Type fieldType;
            bool flag;
            int id;
            string propVal;
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    propVal = prop.Value.ToString();
                    if (prop.Name == "m_FileID")
                    {
                        if (serializeMod)
                        {
                            id = int.Parse(propVal);
                            prop.Value = (id == 0) ? "null" : Resources.InstanceIDToObject(id).name;
                        }
                        else
                        {
                            prop.Value = (propVal == "null") ? 0 : Get(type, propVal).GetInstanceID();
                        }
                    }
                    fieldType = type.GetField(prop.Name, Collections.bindingFlagsDefualt)?.FieldType;
                    if (fieldType != null)
                    {
                        if (fieldType.IsEnum)
                        {
                            if (serializeMod)
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
        public static T GetMetadata<T>(this string pathBase, T defualt) where T : class
        {
            if (Path.GetExtension(pathBase) == ".meta")
            {
                return null;
            }
            string metaPath = pathBase + ".meta";
            T metadata0 = null;
            if (File.Exists(metaPath))
            {
                metadata0 = FromJson<T>(File.ReadAllText(metaPath));
            }
            if (metadata0 == null)
            {
                File.WriteAllText(metaPath, ToJson(defualt));
            }
            metadata0 = JsonUtility.FromJson<T>(File.ReadAllText(metaPath));
            return metadata0;
        }
        #region "Scriptable Objects"
        public static T GetScriptableObjectOrCreate<T>(this BaseUnityPlugin plugin, string name) where T : ScriptableObject
        {
            T result = Get<T>(name);
            if (!result)
            {
                result = ScriptableObject.CreateInstance<T>();
                SaveScriptableObject<T>(plugin, result);
            }
            return result;
        }
        public static void SaveScriptableObject<T>(this BaseUnityPlugin plugin, ScriptableObject itm) where T : ScriptableObject => File.WriteAllText(Path.Combine(PluginManager.GetProjectFolder(plugin), "ScriptableObject", typeof(T).Name, $"{itm.name}.json"), ToJson(itm));
        #endregion
        #region "Script"
        internal static CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
        internal static CompilerParameters compilerParameters = new CompilerParameters()
        {
            GenerateInMemory = true
        };
        public static Type LoadComponent<T>(string scriptContent) where T : Component => LoadCodes(scriptContent).GetTypes().Where(a => typeof(T).IsAssignableFrom(a)).FirstOrDefault();
        public static Assembly LoadCodes(string scriptContent)
        {
            var c = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, scriptContent);
            if (c.Errors.HasErrors)
            {
                Debug.LogError(c.Errors.ToString());
                return null;
            }
            var a = c.CompiledAssembly;
            try
            {
                compilerParameters.ReferencedAssemblies.Add(a.Location);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            return a;
        }
        #endregion
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
                Add(item);
            }
            return ab;
        }
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
                extraEnumsCount.Add(type, Enum.GetNames(type).Length + 1);
                sampleMode = false;
            }
            extraEnums[type].AddIfNotExsist(name);
            return extraEnumsCount[type] + extraEnums[type].IndexOf(name);
        }

        [HarmonyPatch(typeof(Enum), "GetNames"), HarmonyPostfix]
        private static void Postfix_GetNames(Type enumType, ref string[] __result)
        {
            if (enumType == null)
            {
                return;
            }
            if (sampleMode || !extraEnums.ContainsKey(enumType))
            {
                return;
            }
            if (!enumType.IsEnum)
            {
                Log($"Type: {enumType} isn't an enum!");
                return;
            }
            __result = __result.AddAs(extraEnums[enumType].ToArray());
        }
        [HarmonyPatch(typeof(Enum), "GetName"), HarmonyPostfix]
        private static void Postfix_GetName(Type enumType, object value, ref string __result)
        {
            if (!enumType.IsEnum || Convert.IsDBNull(value) || sampleMode || !extraEnums.ContainsKey(enumType))
            {
                return;
            }
            int v = (int)value;
            if (v < extraEnumsCount[enumType])
            {
                return;
            }
            __result = extraEnums[enumType][v - extraEnumsCount[enumType]];
        }
        #endregion
    }
    public interface IAssetLoader<out T> where T : Object
    {
        T Load(string path);
    }
    #region"Asset Loaders"
    namespace UnityInterface.Assets
    {
        public class Texture2DLoader : IAssetLoader<Texture2D>
        {
            [Serializable]
            public class Texture2DMetadata
            {
                public bool isReadable;
                public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
                public FilterMode filterMode = FilterMode.Point;
                public int anisoLevel = 1;
                public Texture2D LoadAndApply(string path)
                {
                    Texture2D t = new Texture2D(1, 1);
                    t.name = Path.GetFileNameWithoutExtension(path);

                    t.wrapMode = wrapMode;
                    t.filterMode = filterMode;
                    t.anisoLevel = anisoLevel;

                    if (t.LoadImage(File.ReadAllBytes(path), !isReadable))
                    {
                        return t;
                    }
                    Debug.LogWarning("Could not Get texture from path");
                    return null;
                }
            }
            public static Texture2DMetadata metadataEmpty = new Texture2DMetadata();
            public Texture2D Load(string path)
            {
                if (Path.GetExtension(path) == ".meta")
                {
                    return null;
                }

                return path.GetMetadata(metadataEmpty).LoadAndApply(path);
            }
        }
        public class AudioClipLoader : IAssetLoader<AudioClip>
        {
            public AudioClip Load(string path) => ResourcesManager.GetAudioClipFromPath(path);
        }
        public class SpriteLoader : IAssetLoader<Sprite>
        {
            [Serializable]
            public class SpriteMetadata : Texture2DMetadata
            {
                public float rectX = 0, rectY = 0, rectWidth, rectHeight, pivotX = 0.5f, pivotY = 0.5f, pixelsPerUnit = 100;
            }
            static SpriteMetadata metadataEmpty = new SpriteMetadata();
            public Sprite Load(string path)
            {
                if (Path.GetExtension(path) == ".meta")
                {
                    return null;
                }
                string pathMeta = path + ".meta";
                Texture2D texture; Sprite s; SpriteMetadata metadata;
                if (!File.Exists(pathMeta))
                {
                    texture = ResourcesManager.GetTexture2DFromPathSimple(path);

                    metadata = new SpriteMetadata();
                    metadata.rectX = 0;
                    metadata.rectY = 0;
                    metadata.rectWidth = texture.width;
                    metadata.rectHeight = texture.height;
                    metadata.pivotX = 0.5f;
                    metadata.pivotY = 0.5f;
                    metadata.pixelsPerUnit = 100;
                }
                else
                {
                    metadata = path.GetMetadata(metadataEmpty);
                    texture = metadata.LoadAndApply(path);
                }

                s = Sprite.Create(texture, new Rect(metadata.rectX, metadata.rectY, metadata.rectWidth, metadata.rectHeight), new Vector2(metadata.pivotX, metadata.pivotY), metadata.pixelsPerUnit);
                s.name = texture.name;
                return s;
            }
        }
        public class MeshLoader : IAssetLoader<Mesh>
        {
            public Mesh Load(string path) => ResourcesManager.GetMeshFromPath(path);
        }
        public class AssetBundleLoader : IAssetLoader<AssetBundle>
        {
            public AssetBundle Load(string path) => ResourcesManager.GetAssetBundleFromPath(path);
        }
        public class ScriptLoader : IAssetLoader<AssemblyObject>
        {
            public AssemblyObject Load(string path) => new AssemblyObject() { assembly = ResourcesManager.LoadCodes(path) };
        }
        [Serializable]
        public class AssemblyObject : Object
        {
            public Assembly assembly;
        }
    }
}
#endregion
