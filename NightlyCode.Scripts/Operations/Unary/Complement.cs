using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// computes the bit-complement of the result of a token
    /// </summary>
    public class Complement : UnaryOperator {
        internal Complement() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments)
        {
            return ~(dynamic) Operand.Execute(variables, arguments);
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