using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes bitwise and of lhs and rhs and assigns the result to lhs
    /// </summary>
    public class AndAssign : OperatorAssign
    {
        internal AndAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(arguments) & (dynamic) Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.AndAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} &= {Rhs}";
        }
    }
}