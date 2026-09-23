using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace UnityInterface
{
    /// <summary>
    /// Useful Stuffs
    /// </summary>
    [HarmonyPatch]
    public static class Collections
    {
        internal const BindingFlags bindingFlagsForMerge = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal const BindingFlags bindingFlagsDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        internal const BindingFlags bindingFlagsForParents = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        internal static Dictionary<(Type, BindingFlags), List<FieldInfo>> fieldsCache = new Dictionary<(Type, BindingFlags), List<FieldInfo>>();
        internal static Dictionary<(Type, BindingFlags), List<PropertyInfo>> propertiesCache = new Dictionary<(Type, BindingFlags), List<PropertyInfo>>();
        public static bool ContainsField(this object obj, string name, BindingFlags flags = bindingFlagsDefault) => obj.GetType().GetFieldsWithParents(flags).Contains(name);
        public static object GetValue(this object obj, string name, BindingFlags flags = bindingFlagsDefault)
        {
            var t = obj.GetType();
            var f = t.GetFieldsInfoWithParents(flags).FirstOrDefault(a => a.Name == name);
            if (f != null)
            {
                return f.GetValue(obj);
            }
            var p = t.GetPropertiesInfoWithParents(flags).FirstOrDefault(a => a.Name == name);
            var g = p?.GetGetMethod(true) ?? throw new MissingMemberException(t.FullName, name);
            return g.Invoke(obj, Array.Empty<object>());
        }
        public static T GetValue<T>(this object obj, string name, BindingFlags flags = bindingFlagsDefault) => (T)GetValue(obj, name, flags);
        public static void SetValue<T>(this object obj, string name, T value, BindingFlags flags = bindingFlagsDefault)
        {
            var t = obj.GetType();
            var f = t.GetFieldsInfoWithParents(flags).FirstOrDefault(a => a.Name == name);
            if (f != null)
            {
                f.SetValue(obj, value);
                return;
            }
            var p = t.GetPropertiesInfoWithParents(flags).FirstOrDefault(a => a.Name == name);
            var s = p?.GetSetMethod(true) ?? throw new MissingMemberException(t.FullName, name);
            s.Invoke(obj, new object[] { value });
        }
        public static T[] AddAs<T>(this T[] obj, params T[] value)
        {
            List<T> list = new List<T>(obj);
            list.AddRange(value);
            return list.ToArray();
        }
        public static List<(Component, List<string>)> GetReferencesFromGameObject(this Component referenced, BindingFlags flags = bindingFlagsForMerge) => referenced.GetComponentsInChildren<Component>().Where(a => a != null && a != referenced).Select(a => (a, a.GetType().GetFieldsWithParents(flags).Where(b => Equals(a.GetValue(b), referenced)).ToList())).ToList();
        public static void SetReferencesFromGameObject(this Component injection, List<(Component, List<string>)> data) => data.ForEach(a => a.Item2.ForEach(b => a.Item1?.SetValue(b, injection)));
        /// <summary>
        /// Merge all vars from parent to target.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent">Merge Sample</param>
        /// <param name="target">Merged</param>
        public static void Merge(this object parent, object target, BindingFlags flags = bindingFlagsForMerge) => parent.GetType().GetFieldsWithParents(flags).ForEach(a =>
                         {
                             if (target.ContainsField(a, flags))
                             {
                                 target.SetValue(a, parent.GetValue(a), flags);
                             }
                         });

        public static List<FieldInfo> GetFieldsInfoWithParents(this Type type, BindingFlags flags = bindingFlagsForParents)
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
        public static List<string> GetFieldsWithParents(this Type type, BindingFlags flags = bindingFlagsForParents) => type.GetFieldsInfoWithParents(flags).Select(a => a.Name).ToList();

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

        public static T[] NullRemoval<T>(this T[] array) where T : class
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
        public static T[] UniqueCheck<T>(this T[] array) where T : class => NullRemoval(array).Distinct().ToArray();

        public static bool ContainsInterface(this Type interfaceType, Type typeBase) => typeBase.GetInterfaces().Any(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType);
        public static Type GetConstGenericedType(this Type typeBase, Type interfaceType) => typeBase.GetInterfaces().Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceType).FirstOrDefault()?.GetGenericArguments()?.FirstOrDefault();

        public static bool ContainsAttribute<T>(this Type typeBase) where T : Attribute => typeBase.IsDefined(typeof(T), true);
        public static T GetAttribute<T>(this Type typeBase) where T : Attribute => typeBase.GetCustomAttribute<T>(true);
        public static object[] GetAttributes<T>(this Type typeBase) where T : Attribute => typeBase.GetCustomAttributes(typeof(T), true);

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
                    foreach (var a in types)
                    {
                        result.GetComponent(a).ApplyValuesComponent(plugin);
                    }
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
            O pref = GameObject.Instantiate(source, ResourcesManager.prefabParent);

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
        public static C Rescript<O, C>() where O : MonoBehaviour where C : MonoBehaviour => Rescript<O, C>(ResourcesManager.Get<O>().First());
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
                File.WriteAllText(path, ResourcesManager.ToJson(component));
            }
            JsonUtility.FromJsonOverwrite(ResourcesManager.PrepareJsonForOverwrite(File.ReadAllText(path), component.GetType()), component);
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
        public static string[] SafeSplit(this string s, params char[] seperator) => s.Split(seperator);

        public class UnityEventConverter
        {
            public readonly UnityEvent result = new UnityEvent();
            public static implicit operator UnityEventConverter(Action a)
            {
                var c = new UnityEventConverter();
                if (a != null)
                {
                    c.result.AddListener(() => a.Invoke());
                }
                return c;
            }
            public static implicit operator UnityEvent(UnityEventConverter converter) => converter.result;
        }

        public static bool CheckDirectory(string path, bool generateFolder = false)
        {
            if (!Directory.Exists(path))
            {
                if (generateFolder)
                {
                    Debug.LogWarning($"[{path}] its folder doesn't exists! Creating a new one!");
                    Directory.CreateDirectory(path);
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// It only works with ApplyProperties method.<br>If there aren't any GetFrom suits your request.<br>You can make an new on with the classs!
        /// </summary>
        public abstract class GetFromBase : Attribute
        {
            private Type typeOverride;
            public GetFromBase()
            {

            }
            public GetFromBase(Type componentType)
            {
                typeOverride = componentType;
            }
            public void Run(Component component, FieldInfo field)
            {
                if (typeOverride != null && !field.FieldType.IsAssignableFrom(typeOverride))
                {
                    Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{field.Name} {field.FieldType} doesn't match {typeOverride.Name}!");
                    return;
                }
                if (GetValue(component, typeOverride ?? field.FieldType, out var value))
                {
                    field.SetValue(field.IsStatic ? null : component, value);
                }
                else
                {
                    Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{field.Name} try get value failed!");
                }
            }

            public void Run(Component component, PropertyInfo property)
            {
                if (typeOverride != null && !property.PropertyType.IsAssignableFrom(typeOverride))
                {
                    Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{property.Name} {property.PropertyType} doesn't match {typeOverride.Name}!");
                    return;
                }
                var _methodInfo = property.GetSetMethod(true);
                if (_methodInfo != null)
                {
                    if (GetValue(component, typeOverride ?? property.PropertyType, out var value))
                    {
                        _methodInfo.Invoke(_methodInfo.IsStatic ? null : component, new object[] { value });
                    }
                    else
                    {
                        Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{property.Name} try get value failed!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{property.Name} doesn't have setter!");
                }
            }

            protected abstract bool GetValue(Component component, Type type, out object result);
        }

        public class GetFromResources : GetFromBase
        {
            string name = null;
            public GetFromResources(string name) => this.name = name;
            public GetFromResources(Type type, string name) : base(type) => this.name = name;
            protected override bool GetValue(Component component, Type type, out object result)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result = ResourcesManager.Get(type, name);
                    return result != null;
                }
                result = ResourcesManager.Get(type).FirstOrDefault();
                return result != null;
            }
        }

        public class GetFrom : GetFromBase
        {
            //I don't use index because it's rare to see multiple, same component on a game object!
            public GetFrom()
            {

            }
            public GetFrom(Type componentType) : base(componentType)
            {

            }

            protected override bool GetValue(Component component, Type type, out object result)
            {
                if (typeof(GameObject).IsAssignableFrom(type))
                {
                    result = component.gameObject;
                    return true;
                }

                var _component = component.GetComponent(type);
                if (_component == null)
                {
                    result = null;
                    return false;
                }

                result = _component;
                return true;
            }
        }

        public class GetFromChild : GetFromBase
        {
            public string gameObjectName = null;
            public GetFromChild()
            {

            }

            public GetFromChild(string gameObjectName) => this.gameObjectName = gameObjectName;
            public GetFromChild(string gameObjectName, Type componentType) : base(componentType) => this.gameObjectName = gameObjectName;

            protected override bool GetValue(Component component, Type type, out object result)
            {
                Transform _transform = string.IsNullOrWhiteSpace(gameObjectName) ? null : component.transform.Find(gameObjectName);

                if (typeof(GameObject).IsAssignableFrom(type))
                {
                    if (_transform)
                    {
                        result = _transform.gameObject;
                        return true;
                    }
                    result = null;
                    return false;
                }

                Component _component = null;
                if (string.IsNullOrWhiteSpace(gameObjectName))
                {
                    _component = component.GetComponentInChildren(type);
                }

                if (_transform)
                {
                    _component = _transform.GetComponent(type);
                }

                if (_component == null)
                {
                    result = null;
                    return false;
                }

                result = _component;
                return true;
            }
        }

        public class GetFromScene : GetFromBase
        {
            public string gameObjectName = null;
            public GetFromScene()
            {

            }

            public GetFromScene(string gameObjectName) => this.gameObjectName = gameObjectName;
            public GetFromScene(string gameObjectName, Type componentType) : base(componentType) => this.gameObjectName = gameObjectName;

            protected override bool GetValue(Component component, Type type, out object result)
            {
                if (typeof(GameObject).IsAssignableFrom(type))
                {
                    if (!string.IsNullOrWhiteSpace(gameObjectName))
                    {
                        result = UnityEngine.Object.FindObjectsOfType<GameObject>(true).FirstOrDefault(a => a.name == gameObjectName);
                        return result != null;
                    }
                    result = null;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(gameObjectName))
                {
                    result = UnityEngine.Object.FindObjectsOfType(type, true).FirstOrDefault(a => a.name == gameObjectName);
                    return result != null;
                }
                result = UnityEngine.Object.FindObjectsOfType(type, true).FirstOrDefault();
                return result != null;
            }
        }

        public static void ApplyProperties(this Component component, BindingFlags flags = bindingFlagsForMerge)
        {
            var arrayField = component.GetType().GetFieldsInfoWithParents(flags);
            foreach (var a in arrayField)
            {
                foreach (var b in a.GetCustomAttributes<GetFromBase>())
                {
                    try
                    {
                        b?.Run(component, a);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{a.Name} {a.FieldType} {b.GetType().Name} get failed! " + ex);
                    }
                }
            }

            var arrayProperties = component.GetType().GetPropertiesInfoWithParents(bindingFlagsForMerge).Where(a => a.GetSetMethod(true) != null);
            foreach (var a in arrayProperties)
            {
                foreach (var b in a.GetCustomAttributes<GetFromBase>())
                {
                    try
                    {
                        b?.Run(component, a);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{component.GetType().FullName}] {component.name}.{a.Name} {a.PropertyType} {b.GetType().Name} get failed! " + ex);
                    }
                }
            }
        }
    }
}
