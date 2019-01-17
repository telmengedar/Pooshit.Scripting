using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// divides LHS by RHS
    /// </summary>
    public class Division : ValueOperation {
        internal Division() {
        }

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