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
        protected override object Compare(IVariableProvider arguments)
        {
            return (dynamic) Lhs.Execute(arguments) == (dynamic) Rhs.Execute(arguments);
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