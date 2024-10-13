using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Break : ScriptToken, IParameterContainer {

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
        public override string Literal => "break";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "break";
        }

        public IEnumerable<IScriptToken> Parameters {
            get {
                if (Depth is ScriptValue value && !1.Equals(value.Value))
                    yield return Depth;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => true;
    }
}