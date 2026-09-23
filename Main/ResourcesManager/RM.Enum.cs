using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace UnityInterface
{
    [HarmonyPatch]
    public static partial class ResourcesManager
    {
        private static Dictionary<Type, List<string>> extraEnums = new Dictionary<Type, List<string>>();
        private static Dictionary<Type, int> extraEnumsCount = new Dictionary<Type, int>();
        private static bool sampleMode;
        public static T ToEnum<T>(this string name) where T : Enum => (T)name.ToEnum(typeof(T));
        public static object ToEnum(this string name, Type type)
        {
            if (Enum.IsDefined(type, name))
            {
                return Enum.Parse(type, name, true);
            }
            if (!extraEnums.ContainsKey(type))
            {
                extraEnums.Add(type, new List<string>());
                sampleMode = true;
                extraEnumsCount.Add(type, Enum.GetNames(type).Length + 1);
                sampleMode = false;
            }
            extraEnums[type].AddIfNotExsist(name);
            return extraEnumsCount[type] + extraEnums[type].IndexOf(name);
        }

        [HarmonyPatch(typeof(Enum), "GetNames"), HarmonyPostfix]
        private static void Postfix_GetNames(Type enumType, ref string[] __result)
        {
            if (enumType == null)
            {
                return;
            }
            if (sampleMode || !extraEnums.ContainsKey(enumType))
            {
                return;
            }
            if (!enumType.IsEnum)
            {
                Log(LogLevel.Error, $"[{enumType.FullName}] isn't an enum type!");
                return;
            }
            __result = __result.AddAs(extraEnums[enumType].ToArray());
        }
        [HarmonyPatch(typeof(Enum), "GetName"), HarmonyPostfix]
        private static void Postfix_GetName(Type enumType, object value, ref string __result)
        {
            if (!enumType.IsEnum || Convert.IsDBNull(value) || sampleMode || !extraEnums.ContainsKey(enumType))
            {
                return;
            }
            int v = (int)value;
            if (v < extraEnumsCount[enumType])
            {
                return;
            }
            __result = extraEnums[enumType][v - extraEnumsCount[enumType]];
        }
    }
}