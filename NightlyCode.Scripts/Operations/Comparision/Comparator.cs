using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// operator used to compare two values resulting in a boolean
    /// </summary>
    public abstract class Comparator : ScriptToken, IBinaryToken, IOperator {

        /// <summary>
        /// compares lhs and rhs and returns value of comparision
        /// </summary>
        /// <returns>comparision value</returns>
        protected abstract object Compare(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments);

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments)
        {
            try {
                object lhs = Lhs.Execute(variables, arguments);
                object rhs = Rhs.Execute(variables, arguments);
                if (lhs != null && rhs != null)
                    rhs = Converter.Convert(rhs, lhs.GetType());
                return Compare(lhs, rhs, variables, arguments);
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