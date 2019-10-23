using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for type operations
    /// </summary>
    public static class TypeExtensions {

        /// <summary>
        /// determines the specified type from a string
        /// </summary>
        /// <param name="provider">provider for script named types</param>
        /// <param name="token">token from which type should be determined</param>
        /// <param name="typename">name of type</param>
        /// <returns>Type determined from string</returns>
        public static Type DetermineType(this ITypeProvider provider, IScriptToken token, string typename) {
            ITypeInstanceProvider instanceprovider = provider.GetType(typename);
            if (instanceprovider != null) {
                Type type = instanceprovider.ProvidedType;
                if (type == null)
                    throw new ScriptRuntimeException($"type '{typename}' does not provide type information", token);

                return type;
            }
            else {
                // try to load type dynamically
                Type type = Type.GetType(typename);
                if (type == null)
                    throw new ScriptRuntimeException($"Unknown type '{typename}'", token);

                return type;
            }
        }

        /// <summary>
        /// determines whether the specified type is a nullable type
        /// </summary>
        /// <param name="type">type to check</param>
        /// <returns></returns>
        public static bool IsNullable(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}