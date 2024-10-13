using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// a reference to an assignable token
    /// </summary>
    public class Reference : ScriptToken, IAssignableToken {
        IAssignableToken argument;

        /// <summary>
        /// creates a new <see cref="Reference"/>
        /// </summary>
        /// <param name="argument">argument to use as reference parameter</param>
        internal Reference(IAssignableToken argument) {
            this.argument = argument;
        }

        /// <inheritdoc />
        public override string Literal => "ref";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return argument.Execute(context);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token, ScriptContext context) {
            return argument.Assign(token, context);
        }
    }
}