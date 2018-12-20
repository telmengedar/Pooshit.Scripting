using System;
using System.Collections.Generic;

namespace NightlyCode.Scripting {

    /// <summary>
    /// simple lookup for variables
    /// </summary>
    public class VariablePool : IScriptVariableHost {
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
    }
}