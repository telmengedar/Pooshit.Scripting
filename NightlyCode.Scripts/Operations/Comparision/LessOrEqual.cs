using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class LessOrEqual : Comparator {
        internal LessOrEqual() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, IVariableProvider arguments)
        {
            return (dynamic)lhs <= (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LessOrEqual;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} <= {Rhs}";
        }

    }
}