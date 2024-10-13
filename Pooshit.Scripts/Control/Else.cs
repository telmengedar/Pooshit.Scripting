using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// token containing body for an <see cref="If"/> statement of which condition is not met
    /// </summary>
    public class Else : ControlToken {

        /// <summary>
        /// creates a new <see cref="Else"/>
        /// </summary>
        internal Else() {
        }

        /// <inheritdoc />
        public override string Literal => "else";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Body.Execute(context);
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"else {Body}";
        }
    }
}