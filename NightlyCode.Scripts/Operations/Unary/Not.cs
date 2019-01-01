using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// logical negation of boolean values
    /// </summary>
    public class Not : UnaryOperator {

        /// <inheritdoc />
        public override object Execute() {
            return !Operand.Execute().ToBoolean();
        }

        public override Operator Operator => Operator.Not;
    }
}