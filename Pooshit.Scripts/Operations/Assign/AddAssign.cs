using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Operations.Assign {

    /// <summary>
    /// adds a value to the result of a token and assigns it at the same time
    /// </summary>
    public class AddAssign : OperatorAssign {
        internal AddAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(ScriptContext context) {
            return (dynamic) Lhs.Execute(context) + (dynamic) Rhs.Execute(context);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.AddAssign;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} += {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "+=";
    }
}