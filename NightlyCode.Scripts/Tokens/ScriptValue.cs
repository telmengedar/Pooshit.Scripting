using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// value in a script command
    /// </summary>
    class ScriptValue : IScriptToken {
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
        public override string ToString() {
            return value?.ToString() ?? "null";
        }
    }
}