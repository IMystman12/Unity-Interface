using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Mono.Cecil;
using UnityEngine;
using UnityEngine.Events;
using static Mono.Security.X509.X520;
using static UnityInterface.ResourcesManager;

namespace UnityInterface
{
    /// <summary>
    /// A sort of goods!
    /// </summary>
    [HarmonyPatch]
    public static class Collections
    {
        private static Dictionary<object, Traverse> storage = new Dictionary<object, Traverse>();
        public static object GetValue(this object obj, string name) => GetValue<object>(obj, name);
        private static void CheckExists(object obj)
        {
            if (!storage.ContainsKey(obj))
            {
                storage.Add(obj, Traverse.Create(obj));
            }
        }
        public static bool ContainsField(this object obj, string name)
        {
            CheckExists(obj);
            return storage[obj].Field(name).FieldExists();
        }
        public static T GetValue<T>(this object obj, string name)
        {
            CheckExists(obj);
            return storage[obj].Field(name).GetValue<T>();
        }
        public static Traverse SetValue<T>(this object obj, string name, T value)
        {
            CheckExists(obj);
            return storage[obj].Field(name).SetValue(value);
        }
        public static T[] AddAs<T>(this T[] obj, params T[] value)
        {
            List<T> list = new List<T>(obj);
            list.AddRange(value);
            return list.ToArray();
        }
        public static List<(Component, List<string>)> GetReferencesFromGameObject(this Component referenced) => referenced.GetComponentsInChildren<Component>().Where(a => a != referenced).Select(a => (a, a.GetType().GetFieldsWithParents().Where(b => a.GetValue(b) == referenced).ToList())).ToList();
        public static void SetReferencesFromGameObject(this Component injection, List<(Component, List<string>)> data) => data.ForEach(a => a.Item2.ForEach(b => a.Item1?.SetValue(b, injection)));
        /// <summary>
        /// Merge all vars from parent to target.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">Merge Sample</param>
        /// <param name="target">Merged</param>
        public static void Merge(this object parent, object target)
        {
            CheckExists(parent);
            CheckExists(target);
            parent.GetType().GetFieldsWithParents().ForEach(a =>
                         {
                             if (storage[target].Field(a).FieldExists())
                             {
                                 target.SetValue(a, parent.GetValue(a));
                             }
                         });
        }
        internal static BindingFlags bindingFlagsDefualt => BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static List<string> GetFieldsWithParents(this Type type)
        {
            int i = 0;
            Type t = type;
            List<string> result = new List<string>();
            while (t != null&& t != typeof(UnityEngine.Object)&&t!=typeof(object))
            {
                result.AddRange(t.GetFields(bindingFlagsDefualt).Select(a => a.Name));
                t = t.BaseType;
                i++;
            }
            result = result.Distinct().ToList();
            return result;
        }
        public static string[] GetAllFiles(string path, string extensionWithDot = "") => Directory.GetFiles(path, $"*{extensionWithDot}", SearchOption.AllDirectories);
        [Obsolete("Use List<T>.Foreach() instead!", true)] public static void Foreach() => throw new Exception("It's unless!");
        public static T[] FindWithInactiveAll<T>(this UnityEngine.Object obj, string name) where T : UnityEngine.Object => GameObject.FindObjectsOfType<T>(true).Where(a => a.name == name).ToArray();
        public static T FindWithInactive<T>(this UnityEngine.Object obj, string name) where T : UnityEngine.Object => GameObject.FindObjectsOfType<T>(true).Where(a => a.name == name).First();
        public static T[] NullCheck<T>(this T[] array)
        {
            List<T> result = new List<T>();
            foreach (var item in array)
            {
                if (item != null)
                {
                    result.Add(item);
                }
            }
            return result.ToArray();
        }
        public static T[] UniqueCheck<T>(this T[] array) => NullCheck(array).Distinct().ToArray();
        public static bool ContainsInterface(this Type interfaceType, Type typeBase) => typeBase.GetInterfaces().Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType);
        public static Type GetConstGenericedType(this Type typeBase, Type interfaceType) => typeBase.GetInterfaces().Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType).FirstOrDefault()?.GetGenericArguments()?.FirstOrDefault();
        public static bool ContainsAttribute(this Type typeBase, Type attributeType) => typeBase.CustomAttributes.Any(a => attributeType.IsAssignableFrom(a.GetType()));
        public static string ReplaceExtension(this string pathBase, string extension) => $"{pathBase.Substring(0, Path.GetExtension(pathBase).Length)}{extension}";
        public static T ToGameObject<T>(this BaseUnityPlugin plugin, bool toPrefab = false, bool applyValues = false) => plugin.ToGameObject(toPrefab, applyValues, typeof(T)).GetComponent<T>();
        public static GameObject ToGameObject(this BaseUnityPlugin plugin, bool toPrefab, bool applyValues, params Type[] types)
        {
            if (types.Length > 0)
            {
                GameObject result = new GameObject(types.First().Name);

                if (toPrefab)
                {
                    ResourcesManager.SetAsPrefab(result);
                }

                foreach (var a in types)
                {
                    result.AddComponent(a);
                }

                if (applyValues)
                {
                    types.ToList().ForEach(a => result.GetComponent(a).ApplyValuesComponent(plugin));
                }
                return result;
            }
            return null;
        }
        /// <summary>
        /// The gameObject with script(O) and replace it to script(C)
        /// </summary>
        /// <typeparam name="O">Script(O) type</typeparam>
        /// <typeparam name="C">Script(C) type</typeparam>
        /// <returns>Replaced script(C)</returns>
        public static C Rescript<O, C>(O source) where O : MonoBehaviour where C : MonoBehaviour
        {
            O pref = GameObject.Instantiate(source, prefabParent);

            GameObject a = pref.gameObject;
            a.name = typeof(C).Name;

            var data = pref.GetReferencesFromGameObject();
            var b = pref.gameObject.AddComponent<C>();
            pref.Merge(b);
            b.SetReferencesFromGameObject(data);

            GameObject.Destroy(pref);

            return a.GetComponent<C>();
        }
        /// <summary>
        /// Find the first gameObject with script(O) and replace it to script(C)
        /// </summary>
        /// <typeparam name="O">Script(O) type</typeparam>
        /// <typeparam name="C">Script(C) type</typeparam>
        /// <returns>Replaced script(C)</returns>
        public static C Rescript<O, C>() where O : MonoBehaviour where C : MonoBehaviour => Rescript<O, C>(Get<O>().First());
        public static T Random<T>(this IEnumerable<T> selections) => Random(selections.ToArray());
        public static T Random<T>(params T[] selections) => selections[UnityEngine.Random.Range(0, selections.Length)];
        public static T Random<T>(this IEnumerable<T> selections, System.Random rng) => Random(rng, selections.ToArray());
        public static T Random<T>(System.Random rng, params T[] selections) => selections[rng.Next(0, selections.Length)];
        public static void ApplyValuesComponent(this Component component, BaseUnityPlugin plugin)
        {
            string name = $"{component.name} {component.GetType().Name}";
            string path = Path.Combine(PluginManager.GetProjectFolder(plugin), $"{name}.json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, ToJson(component));
            }
            JsonUtility.FromJsonOverwrite(FromJson(File.ReadAllText(path), component.GetType()), component);
        }
        public static void ApplyValues<T>(this T component, BaseUnityPlugin plugin) where T : Component => component.ApplyValuesComponent(plugin);
        /// <summary>
        /// Return true mean this itm was addend into list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="itm"></param>
        /// <returns></returns>
        public static bool AddIfNotExsist<T>(this List<T> list, T itm)
        {
            if (!list.Contains(itm))
            {
                list.Add(itm);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Scroll the int in [0,EnumLength-1]. Enum must be int-based.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumBase"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static T ScrollEnum<T>(this T enumBase, int direction, int customLengthSubtract = 1) where T : Enum
        {
            var val = ((int)(object)enumBase) + direction;
            int max = Enum.GetNames(typeof(T)).Length - customLengthSubtract;
            if (val < 0)
            {
                return (T)(object)max;
            }
            if (val > max)
            {
                return (T)(object)0;
            }
            return (T)(object)val;
        }
    }
}