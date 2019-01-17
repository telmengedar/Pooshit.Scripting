using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// multiplies a value from the result of a token and assigns it to the same token
    /// </summary>
    public class MulAssign : OperatorAssign
    {
        internal MulAssign() {
        }

        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() * (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.MulAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} *= {Rhs}";
        }
    }
}