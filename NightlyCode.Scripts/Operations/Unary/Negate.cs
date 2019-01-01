using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// negates the value of a token
    /// </summary>
    public class Negate : UnaryOperator
    {
        /// <inheritdoc />
        public override object Execute() {
            return -(dynamic) Operand.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Negate;
    }
}