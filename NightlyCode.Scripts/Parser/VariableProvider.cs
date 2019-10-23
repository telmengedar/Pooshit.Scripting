using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// basic implementation of a variable provider used if no custom provider is specified
    /// </summary>
    class VariableProvider : IVariableProvider {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();
        readonly IVariableProvider parentprovider;

        /// <summary>
        /// creates a new <see cref="VariableProvider"/>
        /// </summary>
        /// <param name="parentprovider">parent variable scope</param>
        /// <param name="variables">variables to provide</param>
        public VariableProvider(IVariableProvider parentprovider = null, params Variable[] variables) {
            this.parentprovider = parentprovider;
            foreach (Variable variable in variables)
                values[variable.Name] = variable.Value;
        }

        /// <summary>
        /// replaces a variable value
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <param name="value">value to write</param>
        internal void ReplaceVariable(string name, object value) {
            values[name] = value;
        }

        /// <summary>
        /// access to value lookup
        /// </summary>
        protected Dictionary<string, object> Values => values;

        /// <inheritdoc />
        public object GetVariable(string name) {
            if (!ContainsVariable(name))
                throw new ScriptRuntimeException($"Variable {name} not found", null);

            return values[name];
        }

        /// <inheritdoc />
        public bool ContainsVariable(string name) {
            return values.ContainsKey(name);
        }

        /// <summary>
        /// get provider in chain which contains a variable with the specified name
        /// </summary>
        /// <param name="variable">name of variable to check for</param>
        /// <returns>this if this provider contains this variable, null otherwise</returns>
        public IVariableProvider GetProvider(string variable) {
            if (ContainsVariable(variable))
                return this;
            return parentprovider?.GetProvider(variable);
        }

        /// <inheritdoc />
        public IEnumerable<string> Variables {
            get {
                if (parentprovider != null)
                    return values.Keys.Concat(parentprovider.Variables);
                return values.Keys;
            }
        }
    }
}