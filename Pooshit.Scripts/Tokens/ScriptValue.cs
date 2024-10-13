using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// value in a script command
    /// </summary>
    public class ScriptValue : ScriptToken {

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="value">value</param>
        public ScriptValue(object value) {
            Value = value;
        }

        /// <summary>
        /// contained value
        /// </summary>
        public object Value { get; }

        /// <inheritdoc />
        public override string Literal => "value";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Value;
        }

        /// <inheritdoc />
        public override string ToString() {
            if(Value is string)
                return $"\"{Value}\"";
            return Value?.ToString() ?? "null";
        }
    }
}