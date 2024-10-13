using System.Collections.Generic;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Continue : ScriptToken, IParameterContainer {

        /// <summary>
        /// creates a new <see cref="Break"/>
        /// </summary>
        /// <param name="depth">loop depth to continue</param>
        internal Continue(IScriptToken depth = null) {
            if (depth == null)
                depth = new ScriptValue(1);
            Depth = depth;
        }

        /// <summary>
        /// specifies the depth of continue statement
        /// </summary>
        /// <remarks>
        /// a depth of 2 means that the current loop ended the the outer loop is continued instead
        /// </remarks>
        public IScriptToken Depth { get; }

        /// <inheritdoc />
        public override string Literal => "continue";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "continue";
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