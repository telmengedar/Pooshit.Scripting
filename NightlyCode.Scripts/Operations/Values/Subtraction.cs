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
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) - (dynamic)Rhs.Execute(arguments);
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