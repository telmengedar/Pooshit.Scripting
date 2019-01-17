using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// computes the bit-complement of the result of a token
    /// </summary>
    public class Complement : UnaryOperator {
        internal Complement() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken()
        {
            return ~(dynamic) Operand.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Complement;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"~{Operand}";
        }
    }
}