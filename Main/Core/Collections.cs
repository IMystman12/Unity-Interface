using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityInterface
{
    public delegate void OnSthOutputInMultiply(params object[] args);
    public delegate void OnSthOutputInSingle(object arg);
    public delegate void OnSthOutputInSingleWithRef(ref object arg);
    public delegate bool NeedWaiting();
    public delegate void OnSthHappening();
    public delegate void OnSthOutputInMultiply<T>(params T[] args);
    public delegate void OnSthOutputInSingle<T>(T arg);
    public delegate void OnSthOutputInSingleWithRef<T>(ref T arg);
    public static class Collections
    {
        public class CustomYield : CustomYieldInstruction
        {
            NeedWaiting waiting;
            public CustomYield(NeedWaiting nw)
            {
                waiting = nw;
            }
            public override bool keepWaiting
            {
                get
                {
                    return waiting != null && waiting.Invoke();
                }
            }
        }
        public static T LoadComponent<T>(this GameObject obj, string json) where T : Component
        {
            T v = obj.AddComponent<T>();
            JsonConvert.DeserializeObject<T>(json).Merge(v);
            return v;
        }
        private static void CheckExists(object obj)
        {
            if (!storge.ContainsKey(obj))
            {
                storge.Add(obj, Traverse.Create(obj));
            }
        }
        private static Dictionary<object, Traverse> storge = new Dictionary<object, Traverse>();
        public static object GetValue(this object obj, string name)
        {
            CheckExists(obj);
            return storge[obj].Field(name).GetValue();
        }
        public static T GetValue<T>(this object obj, string name)
        {
            CheckExists(obj);
            return storge[obj].Field(name).GetValue<T>();
        }
        public static void FixType(ref object value, object from)
        {
            Type t = from.GetType();
            if (t != value.GetType())
            {
                if (t == typeof(float))
                {
                    value = Convert.ToSingle(value);
                }
                if (t == typeof(int))
                {
                    value = Convert.ToInt32(value);
                }
            }
        }
        public static string[] GetValueNames(this object obj)
        {
            CheckExists(obj);
            return storge[obj].Fields().ToArray();
        }
        public static object SetValue(this object obj, string name, object value)
        {
            CheckExists(obj);
            FixType(ref value, obj.GetValue(name));
            return storge[obj].Field(name).SetValue(value);
        }
        public static object AddToArray<T>(this object obj, string name, T value)
        {
            CheckExists(obj);
            T[] vals = obj.GetValue<T[]>(name);
            Add(ref vals, value);
            return storge[obj].Field(name).SetValue(vals);
        }
        public static object AddToList<T>(this object obj, string name, T value)
        {
            CheckExists(obj);
            List<T> vals = obj.GetValue<List<T>>(name);
            vals.Add(value);
            return storge[obj].Field(name).SetValue(vals);
        }
        public static void Add<T>(this T[] objs, params T[] value)
        {
            try
            {
                Add(ref objs, value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("failed to add sth in the array! Log: " + ex);
            }
        }
        public static void Add<T>(ref T[] obj, params T[] value)
        {
            List<T> list = new List<T>();
            try
            {
                list.AddRange(obj);
                list.AddRange(value);
                obj = list.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("failed to add sth in the array! Log: " + ex);
            }
        }
        public static void Merge<P, T>(this P parent, T merged)
        {
            Traverse p = Traverse.Create(parent);
            foreach (var item in p.Fields())
            {
                try
                {
                    merged.SetValue(item, GetValue(p, item));
                    if (GetValue(merged, item) == GetValue(p, item))
                    {
                        Debug.Log($"{typeof(T).Name}.{item} is equals to his  parent!");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"{typeof(T).Name}.{item} doesn't exist in target!" + " Log:" + e.ToString());
                }
            }
        }
        public static void RepeatOperations(OnSthOutputInMultiply onSthOutput, params object[][] input)
        {
            if (input == null || input.Length == 0)
                return;

            int length = input[0].Length;
            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == null || input[i].Length != length)
                {
                    throw new ArgumentException("All input arrays must have the same length.");
                }
            }

            for (int i = 0; i < length; i++)
            {
                List<object> list = new List<object>();
                for (int j = 0; j < input.Length; j++)
                {
                    list.Add(input[j][i]);
                }
                onSthOutput?.Invoke(list.ToArray());
            }
        }
    }
    public static class NewJson
    {
        public delegate object OnBoxingExclude(object obj);
        public static string To<T>(T obj, OnBoxingExclude OnBoxingExclude = null, params Type[] exclude)
        {
            object field;
            NewJsonItems items = new NewJsonItems();
            items.items = new List<NewJsonItem>();
            NewJsonItem itm;
            foreach (var item in Traverse.Create(obj).Fields())
            {
                field = obj.GetValue(item);
                if (exclude.Contains(field.GetType()))
                {
                    field = OnBoxingExclude?.Invoke(field);
                }
                itm = new NewJsonItem();
                itm.name = item;
                itm.value = field;
                items.items.Add(itm);
            }
            return JsonConvert.SerializeObject(items);
        }
        public static T From<T>(T result, string json, OnSthOutputInSingleWithRef OnUnboxingCoverted = null, params Type[] coverted)
        {
            NewJsonItems newJsonItems = JsonConvert.DeserializeObject<NewJsonItems>(json);
            for (int i = 0; i < newJsonItems.items.Count; i++)
            {
                NewJsonItem item = newJsonItems.items[i];
                if (coverted.Contains(item.value.GetType()))
                {
                    OnUnboxingCoverted?.Invoke(ref item.value);
                }
                result.SetValue(item.name, item.value);
            }
            return result;
        }
    }
    [Serializable]
    public struct NewJsonItems
    {
        public List<NewJsonItem> items;
    }
    [Serializable]
    public struct NewJsonItem
    {
        public string name;
        public object value;
    }
}