using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Operations.Values {

    /// <summary>
    /// subtracts a RHS from LHS
    /// </summary>
    public class Subtraction : ValueOperation {
        internal Subtraction() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs - (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Subtraction;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} - {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "-";
    }
}