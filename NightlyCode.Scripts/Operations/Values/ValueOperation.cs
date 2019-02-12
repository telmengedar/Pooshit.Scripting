using System;
using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Values {

    /// <summary>
    /// arithmetic operation to apply to two operands
    /// </summary>
    public abstract class ValueOperation : ScriptToken, IBinaryToken, IOperator {

        /// <summary>
        /// executes the value operation
        /// </summary>
        /// <returns>result of operation</returns>
        protected abstract object Operate(IVariableProvider arguments);

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableProvider arguments)
        {
            try {
                return Operate(arguments);
            }
            catch (RuntimeBinderException e) {
                throw new ScriptRuntimeException(e.Message, null, e);
            }
        }

        /// <inheritdoc />
        public abstract Operator Operator { get; }

        /// <inheritdoc />
        public IScriptToken Lhs { get; set; }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }
    }
}