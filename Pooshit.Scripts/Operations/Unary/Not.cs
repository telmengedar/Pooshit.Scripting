using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;

namespace Pooshit.Scripting.Operations.Unary {

    /// <summary>
    /// logical negation of boolean values
    /// </summary>
    public class Not : UnaryOperator {
        internal Not() {
        }

        /// <inheritdoc />
        public override string Literal => "!";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            return !Operand.Execute(context).ToBoolean();
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