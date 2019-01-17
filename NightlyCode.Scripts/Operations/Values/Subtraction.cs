using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// subtracts a RHS from LHS
    /// </summary>
    public class Subtraction : ValueOperation {
        internal Subtraction() {
        }

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() - (dynamic)Rhs.Execute();
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