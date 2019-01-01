using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// computes the bit-complement of the result of a token
    /// </summary>
    public class Complement : UnaryOperator {

        /// <inheritdoc />
        public override object Execute() {
            return ~(dynamic) Operand.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Complement;
    }
}