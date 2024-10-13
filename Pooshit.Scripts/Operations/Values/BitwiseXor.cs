using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// executes a bitwise XOR on two integer values
    /// </summary>
    public class BitwiseXor : ValueOperation {
        internal BitwiseXor() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context)
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

        /// <inheritdoc />
        public override string Literal => "^";
    }
}