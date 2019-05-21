using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// block containing an arithmetic evaluation
    /// </summary>
    public class ArithmeticBlock : ScriptToken {
        readonly IScriptToken inner;

        internal ArithmeticBlock(IScriptToken inner) {
            this.inner = inner;
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            return inner.Execute(variables, arguments);
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"({inner})";
        }
    }
}