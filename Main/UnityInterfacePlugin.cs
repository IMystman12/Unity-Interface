using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace UnityInterface
{
    [BepInPlugin("unity.interface", "Unity Interface", "1.3")]
    internal class UnityInterfacePlugin : PluginSingleton<UnityInterfacePlugin>
    {
        internal static Harmony harmony;

        internal static ManualLogSource assetLogger, pluginLogger;

        protected override void Awake()
        {
            base.Awake();

            GameObject prefabsToManage = new GameObject("Prefabs");
            DontDestroyOnLoad(prefabsToManage);
            prefabsToManage.transform.SetParent(transform);
            prefabsToManage.SetActive(false);
            ResourcesManager.prefabParent = prefabsToManage.transform;

            harmony = new Harmony("imystman12.unity.interface");
            harmony.PatchAll();

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