using System;
using System.Collections.Generic;
using System.Reflection;

namespace NightlyCode.Scripting {

    /// <summary>
    /// interface for a pool of script hosts
    /// </summary>
    public interface IScriptHostPool {

        /// <summary>
        /// get host providing members
        /// </summary>
        /// <param name="name">name of host</param>
        /// <returns>host instance</returns>
        object GetHost(string name);

        /// <summary>
        /// determines whether the pool contains a host with the specified name
        /// </summary>
        /// <param name="name">name of host to check for</param>
        /// <returns>true if pool contains a host with <paramref name="name"/>, false otherwise</returns>
        bool ContainsHost(string name);

        /// <summary>
        /// get extension methods available for type
        /// </summary>
        /// <param name="host">type of host for which to get extension methods</param>
        /// <returns>methods available as extension methods</returns>
        IEnumerable<MethodInfo> GetExtensions(Type host);
    }
}