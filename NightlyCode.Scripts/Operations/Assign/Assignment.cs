using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// assignment in a script
    /// </summary>
    public class Assignment : AssignableToken, IBinaryToken, IOperator {
        IAssignableToken lhs;

        internal Assignment() {
        }

        /// <inheritdoc />
        public override string Literal => "=";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return lhs.Assign(Rhs, context);
        }

        /// <inheritdoc />
        protected override object AssignToken(IScriptToken token, ScriptContext context) {
            if (Rhs is IAssignableToken assignable)
                return assignable.Assign(token, context);
            throw new ScriptRuntimeException("Can't assign value to non assignable token", token);
        }

        /// <inheritdoc />
        public IScriptToken Lhs {
            get => lhs;
            set {
                lhs=value as IAssignableToken;
                if (lhs == null)
                    throw new ScriptParserException(-1, -1, -1, "Left hand side of assignment has to be an assignable token");
            }
        }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public Operator Operator => Operator.Assignment;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} = {Rhs}";
        }
    }
}