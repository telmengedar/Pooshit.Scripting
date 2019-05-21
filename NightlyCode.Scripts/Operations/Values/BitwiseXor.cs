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
        protected override object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments)
        {
            return (dynamic)lhs ^ (dynamic)rhs;
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