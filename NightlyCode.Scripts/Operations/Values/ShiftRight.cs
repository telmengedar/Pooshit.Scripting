using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// shifts the bits of LHS by RHS to the right
    /// </summary>
    public class ShiftRight : ValueOperation {

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() >> (dynamic)Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ShiftRight;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} >> {Rhs}";
        }

    }
}