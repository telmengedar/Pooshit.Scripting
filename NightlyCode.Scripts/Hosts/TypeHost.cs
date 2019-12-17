using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Hosts {

    /// <summary>
    /// provides methods to interact with types in scripts
    /// </summary>
    /// <remarks>
    /// handle with caution since type interaction can easily break security measures
    /// </remarks>
    public class TypeHost {

        /// <summary>
        /// creates a new <see cref="TypeHost"/>
        /// </summary>
        /// <param name="typeProvider">type provider containing script type information</param>
        public TypeHost(ITypeProvider typeProvider) {
            TypeProvider = typeProvider;
        }

        /// <summary>
        /// type provider to use
        /// </summary>
        public ITypeProvider TypeProvider { get; }

        /// <summary>
        /// creates a new type instance from a dictionary
        /// </summary>
        /// <param name="typename">name of type to create</param>
        /// <param name="dictionary">dictionary values containing values for instance properties</param>
        /// <returns>created type</returns>
        public object Create(string typename, Dictionary<object, object> dictionary) {
            if (!TypeProvider.HasType(typename))
                throw new ScriptRuntimeException($"Type '{typename}' not found", null);
            Type type = TypeProvider.GetType(typename).ProvidedType;
            return dictionary.ToType(type);
        }
    }
}