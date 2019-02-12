using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class GreaterOrEqual : Comparator {
        internal GreaterOrEqual() {
        }

        /// <inheritdoc />
        protected override object Compare(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) >= (dynamic)Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.GreaterOrEqual;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} >= {Rhs}";
        }

    }
}