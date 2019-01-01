using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.OpAssign {

    /// <summary>
    /// subtracts a value from the result of a token and assigns it to the same token
    /// </summary>
    public class SubAssign : OperatorAssign
    {
        protected override object Compute() {
            return (dynamic) Lhs.Execute() - (dynamic) Rhs.Execute();
        }

        public override Operator Operator => Operator.SubAssign;
    }
}