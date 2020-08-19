using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// basic implementation of a variable provider used if no custom provider is specified
    /// </summary>
    public class VariableProvider : IVariableProvider {
        readonly IVariableProvider parentprovider;

        /// <summary>
        /// creates a new <see cref="VariableProvider"/>
        /// </summary>
        /// <param name="variables">variables to provide</param>
        public VariableProvider(params Variable[] variables)
        : this(null, variables) {
        }

        /// <summary>
        /// creates a new <see cref="VariableProvider"/>
        /// </summary>
        /// <param name="variables">variables to provide</param>
        public VariableProvider(Dictionary<string, object> variables)
            : this(null, variables) {
        }

        /// <summary>
        /// creates a new <see cref="VariableProvider"/>
        /// </summary>
        /// <param name="parentprovider">parent variable scope</param>
        /// <param name="variables">variables to provide</param>
        public VariableProvider(IVariableProvider parentprovider, params Variable[] variables) {
            this.parentprovider = parentprovider;
            foreach(Variable variable in variables)
                Values[variable.Name] = variable.Value;
        }

        /// <summary>
        /// creates a new <see cref="VariableProvider"/>
        /// </summary>
        /// <param name="parentprovider">parent variable scope</param>
        /// <param name="variables">variables to provide</param>
        public VariableProvider(IVariableProvider parentprovider, Dictionary<string, object> variables) {
            this.parentprovider = parentprovider;
            foreach(KeyValuePair<string, object> variable in variables)
                Values[variable.Key] = variable.Value;
        }

        /// <inheritdoc />
        public object this[string name] {
            get => GetVariable(name);
            set => Values[name] = value;
        }

        /// <summary>
        /// replaces a variable value
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <param name="value">value to write</param>
        internal void ReplaceVariable(string name, object value) {
            Values[name] = value;
        }

        /// <summary>
        /// access to value lookup
        /// </summary>
        protected Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public object GetVariable(string name) {
            if(!ContainsVariable(name))
                throw new ScriptRuntimeException($"Variable {name} not found", null);

            return Values[name];
        }

        /// <inheritdoc />
        public bool ContainsVariable(string name) {
            return Values.ContainsKey(name);
        }

        /// <inheritdoc />
        public bool ContainsVariableInHierarchy(string name) {
            return Values.ContainsKey(name) || (parentprovider?.ContainsVariable(name) ?? false);
        }

        /// <summary>
        /// get provider in chain which contains a variable with the specified name
        /// </summary>
        /// <param name="variable">name of variable to check for</param>
        /// <returns>this if this provider contains this variable, null otherwise</returns>
        public IVariableProvider GetProvider(string variable) {
            if(ContainsVariable(variable))
                return this;
            return parentprovider?.GetProvider(variable);
        }

        /// <inheritdoc />
        public IEnumerable<string> Variables {
            get {
                if(parentprovider != null)
                    return Values.Keys.Concat(parentprovider.Variables);
                return Values.Keys;
            }
        }
    }
}