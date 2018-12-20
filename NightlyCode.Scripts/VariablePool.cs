using System;
using System.Collections.Generic;
using System.Linq;

namespace NightlyCode.Scripting {

    /// <summary>
    /// simple lookup for variables
    /// </summary>
    public class VariablePool : IScriptVariableHost, IDisposable {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>
        /// creates a new <see cref="VariablePool"/>
        /// </summary>
        public VariablePool() { }

        /// <summary>
        /// creates a new <see cref="VariablePool"/>
        /// </summary>
        /// <param name="initialvalues">variables to be contained initially in pool</param>
        public VariablePool(params Tuple<string, object>[] initialvalues) {
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
        void IDisposable.Dispose() {
            foreach (IDisposable disposablevalue in values.Values.OfType<IDisposable>())
                disposablevalue.Dispose();
        }
    }
}