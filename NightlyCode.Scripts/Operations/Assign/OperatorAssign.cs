using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes the result of a binary operation and assigns the result to lhs
    /// </summary>
    public abstract class OperatorAssign : IBinaryToken, IOperator
    {
        IAssignableToken lhs;

        /// <summary>
        /// computes the result of the operation
        /// </summary>
        /// <returns></returns>
        protected abstract object Compute();

        /// <inheritdoc />
        public object Execute()
        {
            return lhs.Assign(new ScriptValue(Compute()));
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