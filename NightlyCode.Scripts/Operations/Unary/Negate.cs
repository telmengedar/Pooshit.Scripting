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
        public override string Literal => "-";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            return -(dynamic) Operand.Execute(context);
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