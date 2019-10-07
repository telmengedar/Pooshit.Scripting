using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token containing body for an <see cref="If"/> statement of which condition is not met
    /// </summary>
    class Else : ControlToken {

        /// <inheritdoc />
        public override string Literal => "else";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }
    }
}