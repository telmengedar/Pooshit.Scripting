using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// compares whether lhs is less than rhs
    /// </summary>
    public class Less : Comparator {

        /// <inheritdoc />
        protected override object Compare()
        {
            return (dynamic)Lhs.Execute() < (dynamic)Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Less;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} < {Rhs}";
        }

    }
}