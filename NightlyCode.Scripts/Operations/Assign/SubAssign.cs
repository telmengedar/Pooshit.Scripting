using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// subtracts a value from the result of a token and assigns it to the same token
    /// </summary>
    public class SubAssign : OperatorAssign
    {
        internal SubAssign() {
        }

        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() - (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.SubAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} -= {Rhs}";
        }
    }
}