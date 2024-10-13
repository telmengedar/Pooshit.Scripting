using System;
using System.Collections.Generic;
using System.Reflection;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// interface for a pool of script hosts
    /// </summary>
    public interface IExtensionProvider {

        /// <summary>
        /// get extension methods available for type
        /// </summary>
        /// <param name="host">type of host for which to get extension methods</param>
        /// <returns>methods available as extension methods</returns>
        IEnumerable<MethodInfo> GetExtensions(Type host);

        /// <summary>
        /// adds methods of an extension type
        /// </summary>
        /// <param name="extensiontype">type of which to add extension methods</param>
        void AddExtensions(Type extensiontype);

        /// <summary>
        /// adds methods of an extension type
        /// </summary>
        /// <typeparam name="T">type of which to add extension methods</typeparam>
        void AddExtensions<T>();
    }
}