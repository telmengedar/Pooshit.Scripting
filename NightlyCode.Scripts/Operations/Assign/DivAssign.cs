using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// divides a value by the result of a token and assigns it to the same token
    /// </summary>
    public class DivAssign : OperatorAssign
    {
        internal DivAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(arguments) / (dynamic) Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.DivAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} /= {Rhs}";
        }

    }
}