
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
namespace UnityInterface
{
    [HarmonyPatch]
    public static partial class ResourcesManager
    {
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
    }
}