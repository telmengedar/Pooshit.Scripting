using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// divides LHS by RHS
    /// </summary>
    public class Division : ValueOperation {
        internal Division() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
        {
            return (dynamic)lhs / (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Division;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} / {Rhs}";
        }

    }
}