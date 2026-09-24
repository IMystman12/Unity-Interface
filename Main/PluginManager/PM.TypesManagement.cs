using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace UnityInterface
{
    public static partial class PluginManager
    {
        internal static List<Type> types = new List<Type>();
        static List<Type> foundedScriptableObjectTypes = new List<Type>();

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

        internal static void InjectPluginDLLs()
        {
            var array = AppDomain.CurrentDomain.GetAssemblies();
            ResourcesManager.compilerParameters.ReferencedAssemblies.Clear();
            ResourcesManager.compilerParameters.ReferencedAssemblies.AddRange(array.Select(a => a.Location).ToArray().UniqueCheck());
            AddType(array.SelectMany(a => a.GetTypes()).ToArray().UniqueCheck());
            Log(LogLevel.Info, $"Founded total types count: {types.Count} and ScriptableObject types count: {foundedScriptableObjectTypes.Count}!");
        }
    }
}