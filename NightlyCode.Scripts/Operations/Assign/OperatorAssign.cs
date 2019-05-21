using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
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
        protected abstract object Compute(IVariableContext variables, IVariableProvider arguments);

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments)
        {
            return lhs.Assign(new ScriptValue(Compute(variables, arguments)), variables, arguments);
        }

        /// <inheritdoc />
        public IScriptToken Lhs
        {
            get => lhs;
            set
            {
                lhs = value as IAssignableToken;
                if (lhs == null)
                    throw new ScriptParserException("Left hand side of an operator assign must be assignable");
            }
        }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}