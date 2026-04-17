using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

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
        /// <summary>
        /// Merge all vars from parent to target.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">Merge Sample</param>
        /// <param name="target">Merged</param>
        public static void Merge<P, T>(this P parent, T target)
        {
            CheckExists(parent);
            CheckExists(target);
            foreach (var item in storage[parent].Fields())
            {
                if (storage[target].Field(item).FieldExists())
                {
                    target.SetValue(item, GetValue(parent, item));
                }
            }
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
        public static bool ContainsAttribute(this Type typeBase, Type attributeType) => typeBase.GetCustomAttributes(true).Any(a => attributeType.IsAssignableFrom(a.GetType()));
    }
}