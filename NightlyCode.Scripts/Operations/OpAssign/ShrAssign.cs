using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.OpAssign {

    /// <summary>
    /// shifts bits of a value to the right by the result of a token and assigns it to the same token
    /// </summary>
    public class ShrAssign : OperatorAssign
    {
        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() >> (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShrAssign;
    }
}