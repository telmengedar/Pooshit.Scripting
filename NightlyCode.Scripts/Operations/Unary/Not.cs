using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Unary {

    /// <summary>
    /// logical negation of boolean values
    /// </summary>
    public class Not : UnaryOperator {
        internal Not() {
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments)
        {
            return !Operand.Execute(arguments).ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Not;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"!{Operand}";
        }
    }
}