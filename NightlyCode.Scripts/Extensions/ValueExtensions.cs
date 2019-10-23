using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for values
    /// </summary>
    static class ValueExtensions {

        public static object GetMask(Type masktype, int numberofbits) {
            object mask = Activator.CreateInstance(masktype);
            for (int i = 0; i < numberofbits; ++i) {
                mask = (dynamic)mask << 1;
                mask = (dynamic) mask | 1;
            }

            return mask;
        }

        public static int GetNumberOfBits(this object value, IScriptToken token) {
            if (value is int || value is uint)
                return 32;
            if (value is long || value is ulong)
                return 64;
            if (value is short || value is ushort)
                return 16;
            if (value is byte || value is sbyte)
                return 8;
            throw new ScriptRuntimeException("Type not supported for operation", token);
        }

        public static bool ToBoolean(this object value) {
            if (value == null)
                return false;

            if (value is bool b)
                return b;

            if (value is string s)
                return s != "";

            if (value is IComparable comparable)
                return comparable.CompareTo(Activator.CreateInstance(value.GetType())) != 0;
            return false;
        }

        public static T Convert<T>(this object value) {
            try {
                return Converter.Convert<T>(value);
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to convert {value} to {typeof(T)}", null, e);
            }
        }
    }
}