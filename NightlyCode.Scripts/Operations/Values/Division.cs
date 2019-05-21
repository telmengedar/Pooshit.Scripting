using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// divides LHS by RHS
    /// </summary>
    public class Division : ValueOperation {
        internal Division() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
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