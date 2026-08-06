using System;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting.Extensions {

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
        public static Type DetermineType(this ITypeProvider provider, string typename) {
            bool isarray = typename.EndsWith("[]");
            if (isarray)
                typename = typename.Substring(0, typename.Length - 2);

            // registered types only - a script-supplied name must never reach an arbitrary assembly
            ITypeInstanceProvider instanceprovider = provider.GetType(typename);
            if (instanceprovider == null)
                throw new ScriptParserException(-1, -1, -1, $"Unknown type '{typename}'");

            Type type = instanceprovider.ProvidedType;
            if (type == null)
                throw new ScriptParserException(-1, -1, -1, $"type '{typename}' does not provide type information");

            return isarray ? type.MakeArrayType() : type;
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