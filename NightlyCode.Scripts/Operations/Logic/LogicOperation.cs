using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Logic {

    /// <summary>
    /// logic operation on boolean operands
    /// </summary>
    abstract class LogicOperation : IBinaryToken, IOperator {

        /// <summary>
        /// executes the logic operation
        /// </summary>
        /// <returns>result of logic operation</returns>
        protected abstract object Operate();

        public object Execute() {
            try
            {
                return Operate();
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