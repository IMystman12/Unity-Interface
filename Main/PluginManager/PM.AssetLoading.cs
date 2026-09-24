using static UnityInterface.Collections;
using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;
using BepInEx.Logging;
using System.Linq;

namespace UnityInterface
{
    public static partial class PluginManager
    {
        /// <summary>
        /// Return the folder: Project_{plugin.Info.Metadata.GUID} full path.
        /// </summary>
        /// <param name="plugin">Folder's owner</param>
        /// <returns></returns>
        public static string GetProjectFolder(BaseUnityPlugin plugin) => Path.Combine(Application.streamingAssetsPath, "Projects", $"Project_{plugin.Info.Metadata.GUID}");

        static bool IsThisPlugin(BaseUnityPlugin plugin) => UnityInterfacePlugin.Instance == plugin;

        internal static void LoadAllPlugins()
        {
            Log(LogLevel.Info, "Load ALL Plugin Assets!");
            var queueToLoad = Chainloader.PluginInfos.Values.Select(a => a.Instance).ToList();
            CheckDirectory(Path.Combine(Application.streamingAssetsPath, "Projects"), true);
            queueToLoad.ForEach(a => LoadSpecificedAssets(a));
            queueToLoad.ForEach(a => PrepareEmptyScriptableObjects(a));
            queueToLoad.ForEach(a => LoadScriptableObjects(a));
            WaitForPremadeResource.done = true;
        }

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
    }
}