using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// statement wrapping a body for exception handling
    /// </summary>
    public class Try : ControlToken {

        internal Try() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments) {
            try {
                return Body.Execute(arguments);
            }
            catch (Exception e) {
                if (Catch != null) {
                    VariableProvider handlerarguments = new VariableProvider(arguments, new Variable("exception", e));
                    return Catch?.Execute(handlerarguments);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <summary>
        /// body to execute when condition is not met
        /// </summary>
        public IScriptToken Catch { get; internal set; }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Catch != null)
                return $"try {Body} catch {Catch}";
            return $"try {Body}";
        }
    }
}