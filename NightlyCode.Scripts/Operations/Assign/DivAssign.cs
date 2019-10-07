using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// divides a value by the result of a token and assigns it to the same token
    /// </summary>
    public class DivAssign : OperatorAssign
    {
        internal DivAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(ScriptContext context) {
            return (dynamic) Lhs.Execute(context) / (dynamic) Rhs.Execute(context);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.DivAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} /= {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "/=";
    }
}