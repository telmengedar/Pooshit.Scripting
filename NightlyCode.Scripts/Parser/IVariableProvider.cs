using System.Collections.Generic;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// provides named variables to script
    /// </summary>
    public interface IVariableProvider {

        /// <summary>
        /// get value of a variable
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <returns>variable value</returns>
        object GetVariable(string name);

        /// <summary>
        /// determines whether the context contains a variable with the specified name
        /// </summary>
        /// <param name="name">name of variable to check for</param>
        /// <returns>true if variable is in this context, false otherwise</returns>
        bool ContainsVariable(string name);

        /// <summary>
        /// get provider in chain which contains a variable with the specified name
        /// </summary>
        /// <param name="variable">name of variable to check for</param>
        /// <returns>next provider which contains this variable, null if the variable is not found</returns>
        IVariableProvider GetProvider(string variable);

        /// <summary>
        /// variables available to this provider
        /// </summary>
        IEnumerable<string> Variables { get; }
    }
}