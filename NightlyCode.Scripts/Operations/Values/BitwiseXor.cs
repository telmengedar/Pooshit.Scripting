using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise XOR on two integer values
    /// </summary>
    public class BitwiseXor : ValueOperation {
        internal BitwiseXor() {
        }

        /// <inheritdoc />
        protected override object Operate(IVariableProvider arguments)
        {
            return (dynamic)Lhs.Execute(arguments) ^ (dynamic)Rhs.Execute(arguments);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.BitwiseXor;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} ^ {Rhs}";
        }

    }
}