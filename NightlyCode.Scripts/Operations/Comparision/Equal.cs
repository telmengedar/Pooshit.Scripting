using System.Text.RegularExpressions;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares two values for equality
    /// </summary>
    public class Equal : Comparator {
        internal Equal() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, IVariableProvider arguments)
        {
            return (dynamic) lhs == (dynamic) rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Equal;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} == {Rhs}";
        }
    }
}