
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// host which serves variables of an instance
    /// </summary>
    public class InstanceVariableHost : IVariableContext {
        readonly object instance;

        /// <summary>
        /// creates a new <see cref="InstanceVariableHost"/>
        /// </summary>
        /// <param name="instance">instance of which to serve variables</param>
        public InstanceVariableHost(object instance) {
            this.instance = instance;
        }

        /// <inheritdoc />
        public object GetVariable(string name) {
            return new ScriptMember(new ScriptValue(instance), name).Execute();
        }

        /// <inheritdoc />
        public void SetVariable(string name, object value) {
            new ScriptMember(new ScriptValue(instance), name).Assign(new ScriptValue(value));
        }

        public bool ContainsVariable(string name) {
            throw new System.NotImplementedException();
        }
    }
}