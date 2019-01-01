using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.OpAssign {

    /// <summary>
    /// adds a value to the result of a token and assigns it at the same time
    /// </summary>
    public class AddAssign : OperatorAssign {

        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() + (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.AddAssign;
    }
}