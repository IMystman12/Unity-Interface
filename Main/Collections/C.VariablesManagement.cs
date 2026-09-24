using System;
using System.Linq;
using System.Reflection;

namespace UnityInterface
{
    public partial class Collections
    {

        public static bool ContainsField(this object obj, string name, BindingFlags flags = bindingFlagsDefault) => obj.GetType().GetFieldsWithParents(flags).Contains(name);

        public static object GetValue(this object obj, string name, BindingFlags flags = bindingFlagsDefault)
        {
            var t = obj.GetType();
            var f = t.GetFieldInfosWithParents(flags).FirstOrDefault(a => a.Name == name);
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
            var f = t.GetFieldInfosWithParents(flags).FirstOrDefault(a => a.Name == name);
            if (f != null)
            {
                f.SetValue(obj, value);
                return;
            }
            var p = t.GetPropertiesInfoWithParents(flags).FirstOrDefault(a => a.Name == name);
            var s = p?.GetSetMethod(true) ?? throw new MissingMemberException(t.FullName, name);
            s.Invoke(obj, new object[] { value });
        }

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
    }
}