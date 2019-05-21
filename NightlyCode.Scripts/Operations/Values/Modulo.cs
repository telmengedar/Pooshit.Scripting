using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// computes the modulus when dividing LHS by RHS
    /// </summary>
    public class Modulo : ValueOperation {
        internal Modulo() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
        {
            return (dynamic)lhs % (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Modulo;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} % {Rhs}";
        }

    }
}