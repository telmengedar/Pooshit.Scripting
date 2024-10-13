using Microsoft.CSharp.RuntimeBinder;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations.Values {

    /// <summary>
    /// arithmetic operation to apply to two operands
    /// </summary>
    public abstract class ValueOperation : ScriptToken, IBinaryToken, IOperator {

        /// <summary>
        /// executes the value operation
        /// </summary>
        /// <returns>result of operation</returns>
        protected abstract object Operate(object lhs, object rhs, ScriptContext context);

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            try {
                object lhs = Lhs.Execute(context);
                object rhs = Rhs.Execute(context);
                TypeInformation.ConvertOperands(ref lhs, ref rhs);
                return Operate(lhs, rhs, context);
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