using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.OpAssign {

    /// <summary>
    /// computes modulus of a value with the result of a token and assigns it to the same token
    /// </summary>
    public class ModAssign : OperatorAssign
    {
        protected override object Compute() {
            return (dynamic) Lhs.Execute() % (dynamic) Rhs.Execute();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.ModAssign;
    }
}