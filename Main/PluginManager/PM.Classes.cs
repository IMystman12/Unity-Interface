using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace UnityInterface
{
    public class PluginSingleton<T> : BaseUnityPlugin
    {
        public static T Instance { get; private set; }
        protected virtual void Awake() => Instance = GetComponent<T>();
    }
    /// <summary>
    /// If you don't want this type to be effected, this is a good choice!
    /// </summary>
    public class SkipTypeScanning : Attribute
    {
    }

    public static partial class PluginManager
    {
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

        internal static void Log(LogLevel logLevel, string log) => UnityInterfacePlugin.pluginLogger?.Log(logLevel, log);
    }
}