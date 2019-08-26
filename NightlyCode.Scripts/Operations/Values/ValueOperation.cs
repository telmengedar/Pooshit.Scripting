using System;
using Microsoft.CSharp.RuntimeBinder;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
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
        protected abstract object Operate(object lhs, object rhs, IVariableContext variables, IVariableProvider arguments);

        bool CanConvert(Type type) {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Object:
                case TypeCode.DateTime:
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments)
        {
            try {
                object lhs = Lhs.Execute(variables, arguments);
                object rhs = Rhs.Execute(variables, arguments);
                TypeInformation.ConvertOperands(ref lhs, ref rhs);
                return Operate(lhs, rhs, variables, arguments);
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