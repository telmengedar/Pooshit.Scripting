using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// logical negation of boolean values
    /// </summary>
    class Not : UnaryOperator {

        /// <inheritdoc />
        public override object Execute() {
            return !Operand.Execute().ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Not;
    }
}