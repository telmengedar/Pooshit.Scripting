using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// shifts bits of a value to the left by the result of a token and assigns it to the same token
    /// </summary>
    public class ShlAssign : OperatorAssign
    {
        internal ShlAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(arguments) << (dynamic) Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShlAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} <<= {Rhs}";
        }
    }
}