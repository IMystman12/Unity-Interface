using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil;
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
                    if (!a.ContainsAttribute<SkipTypeScanning>())
                    {
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
                                ResourcesManager.AddLoader(a.GetConstGenericedType(typeof(IAssetLoader<>)), (IAssetLoader<UnityEngine.Object>)Activator.CreateInstance(a));
                            }
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

            AddType(array.SelectMany(a =>
            {
                try
                {
                    return a.GetTypes().NullRemoval();
                }
                catch (ReflectionTypeLoadException rx)
                {
                    Log(LogLevel.Error, $"[{a.FullName}] tried to get types but some of them were wrong! Exception: {rx.ToString()}");
                    return rx.Types.NullRemoval();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, $"[{a.FullName}] tried to get types but errors! Exception: {ex.ToString()}");
                    return Array.Empty<Type>();
                }
            }).ToArray().UniqueCheck());

            Log(LogLevel.Info, $"Founded types count: {types.Count} and scriptableObject types count: {foundedScriptableObjectTypes.Count}!");
        }
    }
}