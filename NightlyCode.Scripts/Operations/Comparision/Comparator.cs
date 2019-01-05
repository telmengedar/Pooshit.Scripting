using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// operator used to compare two values resulting in a boolean
    /// </summary>
    abstract class Comparator : IBinaryToken, IOperator {

        /// <summary>
        /// compares lhs and rhs and returns value of comparision
        /// </summary>
        /// <returns>comparision value</returns>
        protected abstract object Compare();

        /// <inheritdoc />
        public object Execute() {
            try
            {
                return Compare();
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