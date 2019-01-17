using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// shifts bits of a value to the right by the result of a token and assigns it to the same token
    /// </summary>
    public class ShrAssign : OperatorAssign
    {
        internal ShrAssign() {
        }

        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() >> (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShrAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} >>= {Rhs}";
        }
    }
}