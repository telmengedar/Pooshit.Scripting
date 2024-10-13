using System;
using System.Linq;
using System.Reflection;
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

            ITypeInstanceProvider instanceprovider = provider.GetType(typename);
            if (instanceprovider != null) {
                Type type = instanceprovider.ProvidedType;
                if (type == null)
                    throw new ScriptParserException(-1, -1, -1, $"type '{typename}' does not provide type information");

                if (isarray)
                    return type.MakeArrayType();
                return type;
            }
            else {
                // try to load type dynamically
                Type type = Type.GetType(typename);

                if (!typename.Contains(","))
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetTypes().FirstOrDefault(t => t.FullName == typename || t.Name == typename);
                        if (type != null)
                            return type;
                    }

                    foreach (AssemblyName assembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies()) {
                        type = Assembly.Load(assembly).GetTypes().FirstOrDefault(t => t.FullName == typename || t.Name == typename);
                        if (type != null)
                            return type;
                    }
                }

                if (type == null)
                    throw new ScriptParserException(-1, -1, -1, $"Unknown type '{typename}'");

                if(isarray)
                    return type.MakeArrayType();
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