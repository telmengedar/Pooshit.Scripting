using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Continue : IScriptToken {

        /// <summary>
        /// creates a new <see cref="Break"/>
        /// </summary>
        /// <param name="parameters"></param>
        internal Continue(params IScriptToken[] parameters) {
            if (parameters.Length > 1)
                throw new ScriptParserException("Too many parameters for continue token");
            if (parameters.Length == 1)
                Depth = parameters[0].Execute().Convert<int>();
        }

        /// <summary>
        /// specifies the depth of continue statement
        /// </summary>
        /// <remarks>
        /// a depth of 2 means that the current loop ended the the outer loop is continued instead
        /// </remarks>
        public int Depth { get; } = 1;

        /// <inheritdoc />
        public object Execute() {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "continue";
        }
    }
}