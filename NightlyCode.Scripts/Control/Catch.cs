using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token containing body for a <see cref="Try"/> statement when an exception is catched
    /// </summary>
    class Catch : ControlToken {

        /// <inheritdoc />
        public override string Literal => "catch";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }
    }
}