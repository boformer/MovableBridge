using System;
using System.Reflection;

namespace MovableBridge {
    public static class ComponentExtensions {
        public static T CopyFrom<T, U>(this T comp, U other) where T : class {
            Type type;
            if (other.GetType().IsInstanceOfType(comp)) {
                type = other.GetType();
            } else if (comp.GetType().IsInstanceOfType(other)) {
                type = comp.GetType();
            } else {
                return null; // type mis-match
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos) {
                if (pinfo.CanWrite) {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    } catch {
                    } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }
    }
}
