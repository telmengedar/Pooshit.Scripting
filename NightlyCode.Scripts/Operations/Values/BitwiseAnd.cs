using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise and on two integer values
    /// </summary>
    public class BitwiseAnd : ValueOperation {
        internal BitwiseAnd() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments) {
            return (dynamic) Lhs.Execute(arguments) & (dynamic)Rhs.Execute(arguments);
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