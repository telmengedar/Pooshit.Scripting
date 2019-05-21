using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares two values whether they are not equal
    /// </summary>
    public class NotEqual : Comparator {
        internal NotEqual() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
        {
            return (dynamic) lhs != (dynamic) rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.NotEqual;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} != {Rhs}";
        }
    }
}