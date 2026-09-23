using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityInterface
{
    public static partial class ResourcesManager
    {
        internal static bool serializeMod;
        public static string ToJson(object obj) => ReplaceInstanceIDs(obj.GetType(), JsonUtility.ToJson(obj, true), true);
        public static string PrepareJsonForOverwrite(string json, Type type) => ReplaceInstanceIDs(type, json, false);
        public static object FromJson(string json, Type type) => JsonUtility.FromJson(ReplaceInstanceIDs(type, json, false), type);
        public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(ReplaceInstanceIDs(typeof(T), json, false));

        private static string ReplaceInstanceIDs(Type type, string json, bool serialize)
        {
            serializeMod = serialize;
            return ReplaceToken(type, JToken.Parse(json)).ToString(Formatting.Indented);
        }
        private static JToken ReplaceToken(Type type, JToken token)
        {
            Type fieldType;
            bool flag;
            int id;
            string propVal;
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    propVal = prop.Value.ToString();
                    if (prop.Name == "m_FileID")
                    {
                        if (serializeMod)
                        {
                            id = int.Parse(propVal);
                            prop.Value = (id == 0) ? "null" : Resources.InstanceIDToObject(id).name;
                        }
                        else
                        {
                            prop.Value = (propVal == "null") ? 0 : Get(type, propVal).GetInstanceID();
                        }
                    }
                    fieldType = type.GetField(prop.Name, Collections.bindingFlagsDefault)?.FieldType;
                    if (fieldType != null)
                    {
                        if (fieldType.IsEnum)
                        {
                            if (serializeMod)
                            {
                                id = int.Parse(propVal);
                                prop.Value = Enum.ToObject(fieldType, id).ToString();
                            }
                            else
                            {
                                prop.Value = (int)propVal.ToEnum(fieldType);
                            }
                        }
                        else
                        {
                            ReplaceToken(fieldType, prop.Value);
                        }
                    }
                }
            }
            else if (token is JArray arr)
            {
                fieldType = type.GetElementType();

                if (fieldType == null && type.IsGenericType)
                {
                    fieldType = type.GetGenericArguments()[0];
                }

                if (fieldType != null)
                {
                    foreach (var item in arr)
                    {
                        ReplaceToken(fieldType, item);
                    }
                }
            }
            return token;
        }
    }
}