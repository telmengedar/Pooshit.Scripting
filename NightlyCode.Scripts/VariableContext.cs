using System;
using System.Collections.Generic;
using System.Linq;

namespace NightlyCode.Scripting {

    /// <summary>
    /// simple lookup for variables
    /// </summary>
    public class VariableContext : IVariableContext, IDisposable {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();
        IVariableContext parentcontext;

        /// <summary>
        /// creates a new <see cref="VariableContext"/>
        /// </summary>
        public VariableContext(IVariableContext parentcontext=null) {
            this.parentcontext = parentcontext;
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
        /// creates a new <see cref="VariableContext"/>
        /// </summary>
        /// <param name="initialvalues">variables to be contained initially in pool</param>
        public VariableContext(params Tuple<string, object>[] initialvalues) {
            foreach(Tuple<string, object> value in initialvalues)
                SetVariable(value.Item1, value.Item2);
        }

        /// <inheritdoc />
        public object GetVariable(string name) {
            if (values.TryGetValue(name, out object value))
                return value;
            throw new ScriptException($"Variable {name} not found");
        }

        /// <inheritdoc />
        public void SetVariable(string name, object value) {
            values[name] = value;
        }

        /// <inheritdoc />
        public bool ContainsVariable(string name) {
            return values.ContainsKey(name);
        }

        /// <inheritdoc />
        void IDisposable.Dispose() {
            foreach (IDisposable disposablevalue in values.Values.OfType<IDisposable>())
                disposablevalue.Dispose();
        }
    }
}