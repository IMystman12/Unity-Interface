using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rewired.Utils.Classes.Data;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using UnityInterface.Assets;
using static UnityEngine.GraphicsBuffer;
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
        public static void IntializeBaseOptions(this BaseUnityPlugin plugin)
        {
            bool a = plugin.QuickOption("Load assets automatically", false),
                b = plugin.QuickOption("Generate if not exsists", false),
                c = plugin.QuickOption("Generate references", false);
            if (a)
            {
                if (!queueToLoad.Contains(plugin))
                {
                    queueToLoad.Add(plugin);
                }
                if (b)
                {
                    if (!queueToGenerate.Contains(plugin))
                    {
                        queueToGenerate.Add(plugin);
                    }
                    if (c && !queueRequiredReference.Contains(plugin))
                    {
                        queueRequiredReference.Add(plugin);
                    }
                }
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
                        if (queueRequiredReference.Contains(plugin))
                        {
                            templatePath = Path.Combine(curPath, "Template.json");
                            if (queueToGenerate.Contains(plugin) && !File.Exists(templatePath))
                            {
                                scriptableObject = ScriptableObject.CreateInstance(itmType);
                                File.WriteAllText(templatePath, AssetManager.ReplaceInstanceIDs(itmType, JsonUtility.ToJson(scriptableObject), true));
                            }

                            templatePath = Path.Combine(curPath, "References");
                            if (!Directory.Exists(templatePath))
                            {
                                string s;
                                Directory.CreateDirectory(templatePath);
                                foreach (var itm in Resources.FindObjectsOfTypeAll(itmType))
                                {
                                    s = Path.Combine(templatePath, $"Reference_{itm.name}.json");
                                    File.WriteAllText(s, AssetManager.ReplaceInstanceIDs(itmType, JsonUtility.ToJson(itm), true));
                                }
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
                            JsonUtility.FromJsonOverwrite(AssetManager.ReplaceInstanceIDs(itmType, File.ReadAllText(itmPath), false), AssetManager.GetAsset(itmType, Path.GetFileNameWithoutExtension(itmPath)).First());
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
        static Dictionary<Type, Dictionary<string, Object>> loadedAssets = new Dictionary<Type, Dictionary<string, Object>>();
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
        public static Object[] GetAsset(Type type)
        {
            List<Object> result = loadedAssets.TryGetValue(type, out var val) ? val.Values.ToList() : new List<Object>();
            if (typeof(Component).IsAssignableFrom(type))
            {
                result.AddRange(prefabParent.GetComponentsInChildren(type, true));
            }
            return result.ToArray().UniqueCheck();
        }
        public static T[] GetAsset<T>() where T : Object => GetAsset(typeof(T)).OfType<T>().ToArray();
        public static Object[] GetAsset(Type type, string name) => GetAsset(type).Where(a => a.name == name).ToArray();
        public static T[] GetAsset<T>(string name) where T : Object => GetAsset(typeof(T), name).OfType<T>().ToArray();
        internal static bool serializeMod;
        public static string ReplaceInstanceIDs(Type type, string json, bool serialize)
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
                            prop.Value = (propVal == "null") ? 0 : Resources.Load(propVal, type).GetInstanceID();
                        }
                    }
                    fieldType = type.GetField(prop.Name)?.FieldType;
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
        [Serializable]
        public class Values : ScriptableObject
        {
            public List<Value> values = new List<Value>();
        }
        public class Value
        {
            public string name;
            public string value;
        }
        public static void Override(this Values values, Object @object) => values.values.ForEach(a =>
        {
            if (@object.ContainsField(a.name))
            {
                @object.SetValue(a.name, FromString(a.value, @object.GetType().GetFieldType(a.name)));
            }
        });

        public static void CopyFrom(this Values values, Object @object) => @object.GetType().GetFieldsWithParents().ForEach(a =>
        {
            values.values.Add(new Value()
            {
                name = a,
                value = ToString(@object.GetValue(a))
            });
        });
        private static object FromString(string value, Type type)
        {
            if (type == typeof(string)) return value;
            if (type == typeof(int) && int.TryParse(value, out int intVal)) return intVal;
            if (type == typeof(float) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal)) return floatVal;
            if (type == typeof(double) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleVal)) return doubleVal;
            if (type == typeof(long) && long.TryParse(value, out long longVal)) return longVal;
            if (type == typeof(short) && short.TryParse(value, out short shortVal)) return shortVal;
            if (type == typeof(byte) && byte.TryParse(value, out byte byteVal)) return byteVal;
            if (type == typeof(bool) && bool.TryParse(value, out bool boolVal)) return boolVal;
            if (type == typeof(decimal) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalVal)) return decimalVal;
            if (type == typeof(uint) && uint.TryParse(value, out uint uintVal)) return uintVal;
            if (type == typeof(ulong) && ulong.TryParse(value, out ulong ulongVal)) return ulongVal;
            if (type == typeof(ushort) && ushort.TryParse(value, out ushort ushortVal)) return ushortVal;
            if (type == typeof(sbyte) && sbyte.TryParse(value, out sbyte sbyteVal)) return sbyteVal;
            if (type == typeof(char) && char.TryParse(value, out char charVal)) return charVal;
            if (type.IsEnum) return value.ToEnum(type);
            if (type == typeof(DateTime) && DateTime.TryParse(value, out DateTime dateVal)) return dateVal;
            if (type == typeof(TimeSpan) && TimeSpan.TryParse(value, out TimeSpan timeVal)) return timeVal;
            if (type == typeof(Guid) && Guid.TryParse(value, out Guid guidVal)) return guidVal;
            if (type == typeof(Object)) return (value == "null") ? null : Resources.Load(value, type);
            return JsonUtility.FromJson(value, type);
        }
        private static string ToString(object value)
        {
            Type type = value.GetType();
            if (type == typeof(string)) return (string)value;
            if (type == typeof(int)) return ((int)value).ToString();
            if (type == typeof(float)) return ((float)value).ToString(CultureInfo.InvariantCulture);
            if (type == typeof(double)) return ((double)value).ToString(CultureInfo.InvariantCulture);
            if (type == typeof(long)) return ((long)value).ToString();
            if (type == typeof(short)) return ((short)value).ToString();
            if (type == typeof(byte)) return ((byte)value).ToString();
            if (type == typeof(bool)) return ((bool)value).ToString().ToLower();
            if (type == typeof(decimal)) return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            if (type == typeof(uint)) return ((uint)value).ToString();
            if (type == typeof(ulong)) return ((ulong)value).ToString();
            if (type == typeof(ushort)) return ((ushort)value).ToString();
            if (type == typeof(sbyte)) return ((sbyte)value).ToString();
            if (type == typeof(char)) return ((char)value).ToString();
            if (type == typeof(DateTime)) return ((DateTime)value).ToString("o");  // ISO 8601
            if (type == typeof(TimeSpan)) return ((TimeSpan)value).ToString();
            if (type == typeof(Guid)) return ((Guid)value).ToString();
            if (type.IsEnum) return value.ToString();
            if (value is Object unityObj) return unityObj != null ? unityObj.name : "null";
            return JsonUtility.ToJson(value);
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
        static void PostFix(Type type, ref Object[] __result) => __result = __result.AddAs(GetAsset(type));

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
                extraEnumsCount.Add(type, Enum.GetNames(type).Length + 1);
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
namespace UnityInterface.Assets
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
