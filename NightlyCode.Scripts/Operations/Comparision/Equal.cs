using System.Text.RegularExpressions;
using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares two values for equality
    /// </summary>
    class Equal : Comparator {

        /// <inheritdoc />
        protected override object Compare()
        {
            return (dynamic) Lhs.Execute() == (dynamic) Rhs.Execute();
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