using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// negates the value of a token
    /// </summary>
    public class Negate : UnaryOperator
    {
        internal Negate() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            return -(dynamic) Operand.Execute();
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