using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Break : IScriptToken {

        /// <summary>
        /// creates a new <see cref="Break"/>
        /// </summary>
        /// <param name="parameters"></param>
        internal Break(params IScriptToken[] parameters) {
            if (parameters.Length > 1)
                throw new ScriptParserException("Too many parameters for break token");
            if (parameters.Length == 1)
                Depth = parameters[0].Execute().Convert<int>();
        }

        /// <summary>
        /// number of loops to break with this statement
        /// </summary>
        public int Depth { get; } = 1;

        /// <inheritdoc />
        public object Execute() {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "break";
        }
    }
}