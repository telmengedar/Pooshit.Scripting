using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// simple lookup for variables
    /// </summary>
    class VariableContext : IVariableContext {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();
        readonly IVariableContext parentcontext;

        /// <summary>
        /// creates a new <see cref="VariableContext"/>
        /// </summary>
        /// <param name="parentcontext">parent variable context</param>
        internal VariableContext(IVariableContext parentcontext=null) {
            this.parentcontext = parentcontext;
        }

        /// <summary>
        /// creates a new <see cref="VariableContext"/>
        /// </summary>
        /// <param name="parentcontext">parent variable context</param>
        /// <param name="initialvalues">variables to be contained initially in pool</param>
        internal VariableContext(IVariableContext parentcontext = null, params Variable[] initialvalues)
        : this(parentcontext)
        {
            foreach (Variable variable in initialvalues)
                SetVariable(variable.Name, variable.Value);
        }

        /// <summary>
        /// indexer for hosts
        /// </summary>
        /// <param name="name">name of host to get</param>
        /// <returns>host instance</returns>
        public object this[string name]
        {
            get => GetVariable(name);
            set => SetVariable(name, value);
        }

        /// <summary>
        /// determines whether the variable context is read only
        /// </summary>
        internal bool IsReadOnly { get; set; }

        /// <inheritdoc />
        public void Clear() {
            values.Clear();
        }

        /// <inheritdoc />
        public object GetVariable(string name) {
            if(!ContainsVariable(name))
                throw new ScriptRuntimeException($"Variable {name} not found");

            if (values.TryGetValue(name, out object value))
                return value;
            return parentcontext?.GetVariable(name);
        }

        /// <inheritdoc />
        public void SetVariable(string name, object value) {
            if (parentcontext?.ContainsVariable(name)??false)
                parentcontext.SetVariable(name, value);
            else {
                if (IsReadOnly)
                    throw new ScriptRuntimeException("Variables in this scope are read-only");
                values[name] = value;
            }
        }

        /// <inheritdoc />
        public bool ContainsVariable(string name) {
            return values.ContainsKey(name) || (parentcontext?.ContainsVariable(name) ?? false);
        }

        /// <inheritdoc />
        void IDisposable.Dispose() {
            foreach (IDisposable disposablevalue in values.Values.OfType<IDisposable>())
                disposablevalue.Dispose();
        }
    }
}