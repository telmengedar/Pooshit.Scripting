using System;
using System.Collections.Generic;
using System.Reflection;

namespace NightlyCode.Scripting {

    /// <summary>
    /// plain implementation of <see cref="IExtensionProvider"/> providing hosts and extension methods
    /// </summary>
    public class ExtensionProvider : IExtensionProvider {
        readonly Dictionary<Type, HashSet<MethodInfo>> extensions = new Dictionary<Type, HashSet<MethodInfo>>();

        /// <summary>
        /// indexer for extension methods
        /// </summary>
        /// <param name="host">host type for which to get extension methods</param>
        /// <returns>available extension methods</returns>
        public IEnumerable<MethodInfo> this[Type host] => GetExtensions(host);

        /// <summary>
        /// adds an extension method to the script pool
        /// </summary>
        /// <param name="hosttype">type of host for which to add extension</param>
        /// <param name="method">method to add</param>
        public void AddExtensionMethod(Type hosttype, MethodInfo method) {
            if (!extensions.TryGetValue(hosttype, out HashSet<MethodInfo> methods))
                extensions[hosttype] = methods = new HashSet<MethodInfo>();
            methods.Add(method);
        }

        /// <summary>
        /// adds methods of an extension type
        /// </summary>
        /// <typeparam name="T">type of which to add extension methods</typeparam>
        public void AddExtensions<T>() {
            AddExtensions(typeof(T));
        }

        /// <summary>
        /// adds methods of an extension type
        /// </summary>
        /// <param name="extensiontype">type of which to add extension methods</param>
        public void AddExtensions(Type extensiontype) {
            foreach (MethodInfo method in extensiontype.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0)
                    continue;
                AddExtensionMethod(parameters[0].ParameterType, method);
            }
        }

        /// <inheritdoc />
        public IEnumerable<MethodInfo> GetExtensions(Type host) {
            if (!extensions.TryGetValue(host, out HashSet<MethodInfo> methods))
                yield break;
            foreach (MethodInfo method in methods)
                yield return method;
        }
    }
}