using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityInterface
{
    public static  partial class Collections
    {
        public static void StartGetFroms(this Component component, BindingFlags flags = bindingFlagsForMerge)
        {
            var arrayField = component.GetType().GetFieldInfosWithParents(flags);
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
        //I don't use index because it's rare to see multiple, same components on a game object!
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
}