using System;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// host for script variables
    /// </summary>
    interface IVariableContext : IDisposable {

        /// <summary>
        /// clears all variable entries
        /// </summary>
        void Clear();

        /// <summary>
        /// get value of a variable
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <returns>variable value</returns>
        object GetVariable(string name);

        /// <summary>
        /// set a value of a variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value to set</param>
        void SetVariable(string name, object value);

        /// <summary>
        /// determines whether the context contains a variable with the specified name
        /// </summary>
        /// <param name="name">name of variable to check for</param>
        /// <returns>true if variable is in this context, false otherwise</returns>
        bool ContainsVariable(string name);
    }
}