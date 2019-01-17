using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise OR on two integer values
    /// </summary>
    public class BitwiseOr : ValueOperation {
        internal BitwiseOr() {
        }

        /// <inheritdoc />
        protected override object Operate()
        {
            return (dynamic)Lhs.Execute() | (dynamic)Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.BitwiseOr;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} | {Rhs}";
        }

    }
}