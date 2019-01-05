using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// computes the modulus when dividing LHS by RHS
    /// </summary>
    class Modulo : ValueOperation {

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() % (dynamic)Rhs.Execute();
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