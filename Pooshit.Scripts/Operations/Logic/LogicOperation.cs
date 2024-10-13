using Microsoft.CSharp.RuntimeBinder;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations.Logic {

    /// <summary>
    /// logic operation on boolean operands
    /// </summary>
    public abstract class LogicOperation : ScriptToken, IBinaryToken, IOperator {

        /// <summary>
        /// executes the logic operation
        /// </summary>
        /// <returns>result of logic operation</returns>
        protected abstract object Operate(ScriptContext context);

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            try
            {
                return Operate(context);
            }
            catch (RuntimeBinderException e)
            {
                throw new ScriptRuntimeException(e.Message, null, e);
            }
        }

        /// <inheritdoc />
        public IScriptToken Lhs { get; set; }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        /// <inheritdoc />
        public abstract Operator Operator { get; }
    }
}