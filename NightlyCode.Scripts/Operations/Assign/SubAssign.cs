using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// subtracts a value from the result of a token and assigns it to the same token
    /// </summary>
    public class SubAssign : OperatorAssign
    {
        internal SubAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(IVariableContext variables, IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(variables, arguments) - (dynamic) Rhs.Execute(variables, arguments);
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