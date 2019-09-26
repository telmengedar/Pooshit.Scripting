using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Assign {

    /// <summary>
    /// computes modulus of a value with the result of a token and assigns it to the same token
    /// </summary>
    public class ModAssign : OperatorAssign
    {
        internal ModAssign() {
        }

        /// <inheritdoc />
        protected override object Compute(ScriptContext context) {
            return (dynamic) Lhs.Execute(context) % (dynamic) Rhs.Execute(context);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ModAssign;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} %= {Rhs}";
        }
    }
}