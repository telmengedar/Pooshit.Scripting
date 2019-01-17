using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares two values whether they are not equal
    /// </summary>
    public class NotEqual : Comparator {
        internal NotEqual() {
        }

        /// <inheritdoc />
        protected override object Compare()
        {
            return (dynamic) Lhs.Execute() != (dynamic) Rhs.Execute();
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