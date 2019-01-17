using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes modulus of a value with the result of a token and assigns it to the same token
    /// </summary>
    public class ModAssign : OperatorAssign
    {
        internal ModAssign() {
        }

        protected override object Compute() {
            return (dynamic) Lhs.Execute() % (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ModAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} %= {Rhs}";
        }
    }
}