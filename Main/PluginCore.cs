using System.Collections;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityInterface.AssetLoaders;

namespace UnityInterface
{
    /// <summary>
    /// If you have toggle done as true! It will still waiting!
    /// </summary>
    public class WaitForBuiltInResourceLoaded : CustomYieldInstruction
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
    [BepInPlugin("unity.interface", "Unity Interface", "1.0")]
    internal class PluginCore : BaseUnityPlugin
    {
        internal static bool assetSystemLog, pluginManagerLog;
        void Awake()
        {
            this.AddToLoad();
            this.GenerateAssetFolders();

            GameObject prefabsToManage = new GameObject("Prefabs");
            DontDestroyOnLoad(prefabsToManage);
            prefabsToManage.transform.SetParent(transform);
            prefabsToManage.SetActive(false);
            AssetManager.prefabParent = prefabsToManage.transform;

            new Harmony("imystman12.unity.interface").PatchAll();
            assetSystemLog = this.QuickOption("Asset System Logger", false);
            pluginManagerLog = this.QuickOption("Plugin Manager Logger", false);
        }
        IEnumerator Start()
        {
            yield return new WaitForBuiltInResourceLoaded();

            AssetManager.AddLoader(new Texture2DLoader());
            AssetManager.AddLoader(new SpriteLoader());
            AssetManager.AddLoader(new AudioClipLoader());
            AssetManager.AddLoader(new MeshLoader());
            AssetManager.AddLoader(new AssetBundleLoader());

            PluginManager.InjectPluginDLLs();
            yield return null;
            PluginManager.LoadAllPlugins();

            DEBUGGER.StartTest();
        }
    }
}