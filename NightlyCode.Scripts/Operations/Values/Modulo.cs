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
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) % (dynamic)Rhs.Execute(arguments);
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