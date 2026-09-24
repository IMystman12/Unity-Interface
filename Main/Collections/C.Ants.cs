using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace UnityInterface
{
    /// <summary>
    /// Useful Stuffs
    /// </summary>
    public static partial class Collections
    {
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

        public static T[] AddAs<T>(this T[] obj, params T[] value)
        {
            List<T> list = new List<T>(obj);
            list.AddRange(value);
            return list.ToArray();
        }
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

        public static T Random<T>(this IEnumerable<T> selections) => Random(selections.ToArray());
        public static T Random<T>(params T[] selections) => selections[UnityEngine.Random.Range(0, selections.Length)];
        public static T Random<T>(this IEnumerable<T> selections, System.Random rng) => Random(rng, selections.ToArray());
        public static T Random<T>(System.Random rng, params T[] selections) => selections[rng.Next(0, selections.Length)];

        public static string[] SafeSplit(this string s, params char[] seperator) => s.Split(seperator);

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

    }
}