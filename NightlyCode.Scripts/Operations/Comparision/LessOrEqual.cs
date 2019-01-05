using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    class LessOrEqual : Comparator {

        /// <inheritdoc />
        protected override object Compare()
        {
            return (dynamic)Lhs.Execute() <= (dynamic)Rhs.Execute();
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