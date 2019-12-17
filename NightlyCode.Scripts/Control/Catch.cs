using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token containing body for a <see cref="Try"/> statement when an exception is catched
    /// </summary>
    public class Catch : ControlToken {

        internal Catch() {
        }

        /// <inheritdoc />
        public override string Literal => "catch";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Body.Execute(context);
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"catch {Body}";
        }
    }
}