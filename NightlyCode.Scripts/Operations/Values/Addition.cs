using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// adds two values or concatenates two strings
    /// </summary>
    public class Addition : ValueOperation {
        internal Addition() {
        }

        /// <inheritdoc />
        protected override object Operate(object lhs, object rhs, ScriptContext context) {
            return (dynamic) lhs + (dynamic) rhs;
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Addition;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} + {Rhs}";
        }
    }
}