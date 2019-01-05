using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// divides LHS by RHS
    /// </summary>
    class Division : ValueOperation {

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() / (dynamic)Rhs.Execute();
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