using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityInterface
{
    public static partial class Collections
    {
        internal const BindingFlags bindingFlagsForMerge = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal const BindingFlags bindingFlagsDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        internal const BindingFlags bindingFlagsForParents = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        internal static Dictionary<(Type, BindingFlags), List<FieldInfo>> fieldsCache = new Dictionary<(Type, BindingFlags), List<FieldInfo>>();
        internal static Dictionary<(Type, BindingFlags), List<PropertyInfo>> propertiesCache = new Dictionary<(Type, BindingFlags), List<PropertyInfo>>();

        public static List<FieldInfo> GetFieldInfosWithParents(this Type type, BindingFlags flags = bindingFlagsForParents)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            (Type, BindingFlags) key = (type, flags);
            if (!fieldsCache.ContainsKey(key))
            {
                while (type != null && type != typeof(UnityEngine.Object) && type != typeof(object))
                {
                    result.AddRange(type.GetFields(flags));
                    type = type.BaseType;
                }
                fieldsCache.Add(key, result.Distinct().ToList());
            }
            return fieldsCache[key];
        }
        public static List<string> GetFieldsWithParents(this Type type, BindingFlags flags = bindingFlagsForParents) => type.GetFieldInfosWithParents(flags).Select(a => a.Name).ToList();

        public static List<PropertyInfo> GetPropertiesInfoWithParents(this Type type, BindingFlags flags = bindingFlagsForParents)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();
            (Type, BindingFlags) key = (type, flags);
            if (!propertiesCache.ContainsKey(key))
            {
                while (type != null && type != typeof(UnityEngine.Object) && type != typeof(object))
                {
                    result.AddRange(type.GetProperties(flags));
                    type = type.BaseType;
                }
                propertiesCache.Add(key, result.Distinct().ToList());
            }
            return propertiesCache[key];
        }
        public static List<string> GetPropertiesWithParents(this Type type, BindingFlags flags = bindingFlagsForParents) => type.GetPropertiesInfoWithParents(flags).Select(a => a.Name).ToList();

        public static bool ContainsInterface(this Type interfaceType, Type typeBase) => typeBase.GetInterfaces().Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType);
        public static Type GetConstGenericedType(this Type typeBase, Type interfaceType) => typeBase.GetInterfaces().Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType).FirstOrDefault()?.GetGenericArguments()?.FirstOrDefault();

        public static bool ContainsAttribute<T>(this Type typeBase) where T : Attribute => typeBase.IsDefined(typeof(T), true);
        public static T GetAttribute<T>(this Type typeBase) where T : Attribute => typeBase.GetCustomAttribute<T>(true);
        public static object[] GetAttributes<T>(this Type typeBase) where T : Attribute => typeBase.GetCustomAttributes(typeof(T), true);

    }
}