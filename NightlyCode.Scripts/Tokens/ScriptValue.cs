using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// value in a script command
    /// </summary>
    public class ScriptValue : ScriptToken {
        readonly object value;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="value">value</param>
        internal ScriptValue(object value) {
            this.value = value;
        }

        /// <summary>
        /// contained value
        /// </summary>
        public object Value => value;

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return value;
        }

        /// <inheritdoc />
        public override string ToString() {
            if (value is string)
                return $"\"{value}\"";
            return value?.ToString() ?? "null";
        }
    }
}