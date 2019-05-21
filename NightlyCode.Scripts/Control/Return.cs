using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// returns a value and end execution of current method
    /// </summary>
    public class Return : ScriptToken {
        readonly IScriptToken value;

        /// <summary>
        /// creates a new <see cref="Return"/>
        /// </summary>
        /// <param name="value">token to return</param>
        internal Return(IScriptToken value) {
            this.value = value;
        }

        /// <summary>
        /// token resulting in value to return
        /// </summary>
        public IScriptToken Value => value;

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            return new Return(new ScriptValue(Value?.Execute(variables, arguments)));
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"return {value}";
        }
    }
}