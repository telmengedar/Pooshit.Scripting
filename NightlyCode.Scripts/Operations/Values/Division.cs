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
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) / (dynamic)Rhs.Execute(arguments);
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