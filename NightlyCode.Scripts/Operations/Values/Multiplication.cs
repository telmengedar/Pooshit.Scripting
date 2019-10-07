using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// multiplies two values
    /// </summary>
    public class Multiplication : ValueOperation {
        internal Multiplication() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs * (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Multiplication;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} * {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "*";
    }
}