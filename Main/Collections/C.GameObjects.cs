
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;


namespace UnityInterface
{
    public partial class Collections
    {
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

        public static List<(Component, List<string>)> GetReferencesFromGameObject(this Component referenced, BindingFlags flags = bindingFlagsForMerge) => referenced.GetComponentsInChildren<Component>().Where(a => a != null && a != referenced).Select(a => (a, a.GetType().GetFieldsWithParents(flags).Where(b => Equals(a.GetValue(b), referenced)).ToList())).ToList();
        public static void SetReferencesFromGameObject(this Component injection, List<(Component, List<string>)> data) => data.ForEach(a => a.Item2.ForEach(b => a.Item1?.SetValue(b, injection)));
    }
}