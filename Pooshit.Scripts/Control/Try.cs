using System;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// statement wrapping a body for exception handling
    /// </summary>
    public class Try : ControlToken {

        internal Try() {
        }

        /// <inheritdoc />
        public override string Literal => "try";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            try {
                return Body.Execute(context);
            }
            catch(Exception e) {
                if(Catch != null) {
                    ScriptContext catchcontext = new ScriptContext(context);
                    catchcontext.Arguments["exception"] = e;
                    return Catch?.Execute(catchcontext);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <summary>
        /// body to execute when condition is not met
        /// </summary>
        public Catch Catch { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            if(Catch != null)
                return $"try {Body} {Catch}";
            return $"try {Body}";
        }
    }
}