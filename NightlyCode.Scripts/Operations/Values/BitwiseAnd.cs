using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise and on two integer values
    /// </summary>
    class BitwiseAnd : ValueOperation {

        /// <inheritdoc />
        protected override object Operate() {
            return (dynamic) Lhs.Execute() & (dynamic)Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.BitwiseAnd;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} & {Rhs}";
        }
    }
}