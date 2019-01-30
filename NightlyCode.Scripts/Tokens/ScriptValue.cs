using NightlyCode.Scripting.Parser;

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
        internal ScriptValue(object value) {
            this.value = value;
        }

        /// <summary>
        /// contained value
        /// </summary>
        public object Value => value;

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