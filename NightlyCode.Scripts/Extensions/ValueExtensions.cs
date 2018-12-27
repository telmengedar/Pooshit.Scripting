using System;
using System.Collections.Generic;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for values
    /// </summary>
    public static class ValueExtensions {
        static readonly List<Type> typelist = new List<Type> {
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string)
        };

        public static int GetTypeIndex(this Type type) {
            return typelist.IndexOf(type);
        }

        public static Type GetValueType(this int index) {
            return typelist[index];
        }

        public static bool ToBoolean(this object value) {
            if (value == null)
                return false;

            if (value is bool b)
                return b;
            if (value is IComparable comparable)
                return comparable.CompareTo(Activator.CreateInstance(value.GetType())) != 0;
            return false;
        }
    }
}