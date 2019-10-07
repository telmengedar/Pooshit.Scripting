using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// shifts bits of a value to the left by the result of a token and assigns it to the same token
    /// </summary>
    public class ShlAssign : OperatorAssign
    {
        internal ShlAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(ScriptContext context) {
            return (dynamic) Lhs.Execute(context) << (dynamic) Rhs.Execute(context);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShlAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} <<= {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "<<=";
    }
}