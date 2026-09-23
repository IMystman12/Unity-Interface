using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace UnityInterface
{
    /// <summary>
    /// If you have toggle done as true! Or else it still waiting!
    /// </summary>
    public class WaitForBuiltInResource : YieldInstructionSingleton<WaitForBuiltInResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
    public class WaitForPremadeResource : YieldInstructionSingleton<WaitForPremadeResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
    [BepInPlugin("unity.interface", "Unity Interface", "1.3")]
    internal class UnityInterfacePlugin : PluginSingleton<UnityInterfacePlugin>
    {
        internal static ManualLogSource assetLogger, pluginLogger;
        protected override void Awake()
        {
            base.Awake();

            GameObject prefabsToManage = new GameObject("Prefabs");
            DontDestroyOnLoad(prefabsToManage);
            prefabsToManage.transform.SetParent(transform);
            prefabsToManage.SetActive(false);
            ResourcesManager.prefabParent = prefabsToManage.transform;

            new Harmony("imystman12.unity.interface").PatchAll();

            if (this.QuickOption("Asset Logger", false))
            {
                assetLogger = BepInEx.Logging.Logger.CreateLogSource($" {Info.Metadata.Name} {Info.Metadata.Version} Asset Manager");
            }
            if (this.QuickOption("Plugin Logger", false))
            {
                pluginLogger = BepInEx.Logging.Logger.CreateLogSource($" {Info.Metadata.Name} {Info.Metadata.Version} Plugin Manager");
            }
        }
        IEnumerator Start()
        {
            PluginManager.InjectPluginDLLs();
            yield return WaitForBuiltInResource.Instance;
            yield return null;
            PluginManager.LoadAllPlugins();
        }
    }
}