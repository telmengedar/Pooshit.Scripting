using System;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// provides a context which contains writable variables
    /// </summary>
    public interface IVariableContext : IVariableProvider, IDisposable {

        /// <summary>
        /// clears all variable entries
        /// </summary>
        void Clear();

        /// <summary>
        /// set a value of a variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value to set</param>
        void SetVariable(string name, object value);
    }
}