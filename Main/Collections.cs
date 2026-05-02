using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

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
        public static List<(Component, List<string>)> GetReferencesFromGameObject(this Component referenced) => referenced.GetComponents<Component>().Where(a => a != referenced).Select(a => (a, a.GetType().GetFieldsWithParents().Where(b => a.GetValue(b) == referenced).ToList())).ToList();
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
        public static List<string> GetFieldsWithParents(this Type type) => type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(a => a.Name).ToList();
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
        public static string ReplaceExtension(this string pathBase, string extension) => $"{pathBase.Remove(pathBase.Length - Path.GetExtension(pathBase).Length)}{extension}";
        public static T GetMetadata<T>(this string pathBase, T defualt) where T : class
        {
            if (Path.GetExtension(pathBase) == ".meta")
            {
                return null;
            }
            string metaPath = pathBase.ReplaceExtension(".meta");
            T metadata0 = null;
            if (File.Exists(metaPath))
            {
                metadata0 = JsonUtility.FromJson<T>(File.ReadAllText(metaPath));
            }
            if (metadata0 == null)
            {
                File.WriteAllText(metaPath, JsonUtility.ToJson(defualt));
            }
            metadata0 = JsonUtility.FromJson<T>(File.ReadAllText(metaPath));
            return metadata0;
        }
        public static T ToGameObject<T>(this object header, bool toPrefab = false) => header.ToGameObject(toPrefab, typeof(T)).GetComponent<T>();
        public static GameObject ToGameObject(this object header, bool toPrefab, params Type[] types)
        {
            if (types.Length > 0)
            {
                GameObject result = new GameObject(types.First().Name, types);
                if (toPrefab)
                {
                    AssetManager.SetAsPrefab(result);
                }
                return result;
            }
            return null;
        }
    }
}