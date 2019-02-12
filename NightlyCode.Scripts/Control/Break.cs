using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Break : ScriptToken {

        /// <summary>
        /// creates a new <see cref="Break"/>
        /// </summary>
        /// <param name="depth">depth to break</param>
        internal Break(IScriptToken depth=null) {
            if (depth == null)
                depth = new ScriptValue(1);
            Depth = depth;
        }

        /// <summary>
        /// number of loops to break with this statement
        /// </summary>
        public IScriptToken Depth { get; }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments) {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "break";
        }
    }
}