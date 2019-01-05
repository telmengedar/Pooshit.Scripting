using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// divides a value by the result of a token and assigns it to the same token
    /// </summary>
    public class DivAssign : OperatorAssign
    {
        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() / (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.DivAssign;
    }
}