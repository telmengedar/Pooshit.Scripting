using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes bitwise and of lhs and rhs and assigns the result to lhs
    /// </summary>
    public class AndAssign : OperatorAssign
    {
        internal AndAssign() {
        }

        /// <inheritdoc />
        protected override object Compute() {
            return (dynamic) Lhs.Execute() & (dynamic) Rhs.Execute();
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