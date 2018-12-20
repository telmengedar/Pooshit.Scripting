namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// value in a script command
    /// </summary>
    public class ScriptValue : IScriptToken {
        readonly object value;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="value">value</param>
        public ScriptValue(object value) {
            this.value = value;
        }

        /// <inheritdoc />
        public object Execute() {
            return value;
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            throw new ScriptException("Assignment to a value is not supported");
        }

        /// <inheritdoc />
        public override string ToString() {
            return value?.ToString() ?? "null";
        }
    }
}