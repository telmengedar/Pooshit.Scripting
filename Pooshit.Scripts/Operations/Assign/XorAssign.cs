using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Operations.Assign {

    /// <summary>
    /// computes bitwise and of lhs and rhs and assigns the result to lhs
    /// </summary>
    public class XorAssign : OperatorAssign
    {
        internal XorAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(ScriptContext context) {
            return (dynamic) Lhs.Execute(context) ^ (dynamic) Rhs.Execute(context);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.XorAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} ^= {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "^=";
    }
}