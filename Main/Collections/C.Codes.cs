using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace UnityInterface
{
    public static partial class Collections
    {
        /// <summary>
        /// Use patches flexibly!
        /// </summary>
        public static Harmony Harmony => UnityInterfacePlugin.harmony;

        internal static Dictionary<(Type, BindingFlags), List<MethodInfo>> methodsCache = new Dictionary<(Type, BindingFlags), List<MethodInfo>>();
        public static List<MethodInfo> GetMethodInfosWithParents(this Type type, BindingFlags flags = bindingFlagsForParents)
        {
            List<MethodInfo> result = new List<MethodInfo>();
            (Type, BindingFlags) key = (type, flags);
            if (!fieldsCache.ContainsKey(key))
            {
                while (type != null && type != typeof(UnityEngine.Object) && type != typeof(object))
                {
                    result.AddRange(type.GetMethods(flags));
                    type = type.BaseType;
                }
                methodsCache.Add(key, result.Distinct().ToList());
            }
            return methodsCache[key];
        }
        public static List<string> GetMethodsWithParents(this Type type, BindingFlags flags = bindingFlagsForParents) => type.GetFieldInfosWithParents(flags).Select(a => a.Name).ToList();
    }
}