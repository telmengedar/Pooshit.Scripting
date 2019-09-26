using System;
using System.Linq;
using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// simple lookup for variables
    /// </summary>
    class VariableContext : VariableProvider, IVariableContext {

        /// <summary>
        /// creates a new <see cref="VariableContext"/>
        /// </summary>
        /// <param name="parentprovider">parent variable context</param>
        /// <param name="initialvalues">variables to be contained initially in pool</param>
        internal VariableContext(IVariableProvider parentprovider = null, params Variable[] initialvalues)
            : base(parentprovider, initialvalues) {
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

        /// <inheritdoc />
        public void Clear() {
            Values.Clear();
        }

        /// <inheritdoc />
        public void SetVariable(string name, object value) {
            Values[name] = value;
        }

        /// <summary>
        /// removes a variable from this context
        /// </summary>
        /// <param name="name">name of variable to remove</param>
        public void RemoveVariable(string name) {
            Values.Remove(name);
        }

        /// <inheritdoc />
        void IDisposable.Dispose() {
            foreach (IDisposable disposablevalue in Values.Values.OfType<IDisposable>())
                disposablevalue.Dispose();
        }
    }
}