using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class Greater : Comparator {
        internal Greater() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
        {
            return (dynamic)lhs > (dynamic)rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Greater;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} > {Rhs}";
        }

    }
}