using System;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Parser {

    /// <summary>
    /// interface for a provider of type-instances
    /// </summary>
    public interface ITypeInstanceProvider {

        /// <summary>
        /// type which is provided
        /// </summary>
        /// <remarks>
        /// this type actually does not need to match up with the provided type and is just used as info
        /// it would be nice however to be honest here
        /// </remarks>
        Type ProvidedType { get; }

        /// <summary>
        /// creates an instance using the specified parameters
        /// </summary>
        /// <param name="parameters">parameters to use to create the instance</param>
        /// <param name="context">script execution context</param>
        /// <returns>created instance</returns>
        object Create(IScriptToken[] parameters, ScriptContext context);
    }
}