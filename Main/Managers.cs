using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;
using static UnityInterface.Collections;

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
    public class SkipTypeScanning : Attribute
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
        public static T QuickOption<T>(this BaseUnityPlugin plugin, string name, T defualtVal, string section = "", string description = "") => plugin.Config.Bind(new ConfigDefinition(section, name), defualtVal, new ConfigDescription(description)).Value;

        /// <summary>
        /// Return the folder: Project_{plugin.Info.Metadata.GUID} full path.
        /// </summary>
        /// <param name="plugin">Folder's owner</param>
        /// <returns></returns>
        public static string GetProjectFolder(BaseUnityPlugin plugin) => Path.Combine(Application.streamingAssetsPath, "Projects", $"Project_{plugin.Info.Metadata.GUID}");

        internal static List<BaseUnityPlugin> queueToLoad = new List<BaseUnityPlugin>();

        internal static void Log(LogLevel logLevel, string log) => UnityInterfacePlugin.pluginLogger?.Log(logLevel, log);

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
            Log(LogLevel.Info, $"Founded total types count: {types.Count} and ScriptableObject types count: {foundedScriptableObjectTypes.Count}!");
        }
        public static void AddType<T>() => AddType(typeof(T));
        public static void AddType(params Type[] typez)
        {
            foreach (var a in typez)
            {
                try
                {
                    if (a.ContainsAttribute<SkipTypeScanning>())
                    {
                        continue;
                    }
                    if (!types.AddIfNotExsist(a))
                    {
                        Log(LogLevel.Warning, $"[{a}] was addend!");
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
                catch (Exception ex)
                {
                    Log(LogLevel.Error, $"[{a.FullName}] loaded failed! Excaption: {ex}");
                }
            }
        }

        static bool IsThisPlugin(BaseUnityPlugin plugin) => UnityInterfacePlugin.Instance == plugin;
        private static void LoadSpecificedAssets(BaseUnityPlugin plugin)
        {
            string pathTemp;
            bool flag = IsThisPlugin(plugin);
            foreach (var a in ResourcesManager.assetLoaders.Keys)
            {
                pathTemp = Path.Combine(GetProjectFolder(plugin), a.Name);
                if (CheckDirectory(pathTemp, flag))
                {
                    Directory.GetFiles(pathTemp, "*", SearchOption.AllDirectories).ToList().ForEach(c => ResourcesManager.LoadFromPath(c, a));
                }
            }
        }
        internal static void LoadAllPlugins()
        {
            Log(LogLevel.Info, "Load ALL Plugin Assets!");
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

                        foreach (var itmPath in Directory.GetFiles(curPath, "*.json", SearchOption.AllDirectories).Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
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
                        foreach (var itmPath in Directory.GetFiles(curPath, "*.json", SearchOption.AllDirectories).Where(a => "Template" != Path.GetFileNameWithoutExtension(a) && !a.Contains(Path.Combine(curPath, "References"))))
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
}