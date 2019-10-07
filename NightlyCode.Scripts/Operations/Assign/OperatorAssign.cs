using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes the result of a binary operation and assigns the result to lhs
    /// </summary>
    public abstract class OperatorAssign : ScriptToken, IBinaryToken, IOperator
    {
        IAssignableToken lhs;

        /// <summary>
        /// computes the result of the operation
        /// </summary>
        /// <returns></returns>
        protected abstract object Compute(ScriptContext context);

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            return lhs.Assign(new ScriptValue(Compute(context)), context);
        }

        /// <inheritdoc />
        public IScriptToken Lhs
        {
            get => lhs;
            set
            {
                lhs = value as IAssignableToken;
                if (lhs == null)
                    // TODO: try to provide position of token here
                    throw new ScriptParserException(-1,-1,"Left hand side of an operator assign must be assignable");
            }
        }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}