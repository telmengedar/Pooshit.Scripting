using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// subtracts a RHS from LHS
    /// </summary>
    public class Subtraction : ValueOperation {
        internal Subtraction() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
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

    }
}