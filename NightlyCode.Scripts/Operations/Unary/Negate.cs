using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// negates the value of a token
    /// </summary>
    public class Negate : UnaryOperator
    {
        internal Negate() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments)
        {
            return -(dynamic) Operand.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Negate;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"-{Operand}";
        }
    }
}