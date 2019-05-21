using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {
    /// <summary>
    /// base implementation of a script token with basic error handling
    /// </summary>
    public abstract class ScriptToken : IScriptToken {

        /// <inheritdoc />
        public object Execute(IVariableContext variables, IVariableProvider arguments = null) {
            try {
                return ExecuteToken(variables, arguments);
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute '{this}'", e.Message, e);
            }
        }

        /// <inheritdoc />
        public T Execute<T>(IVariableContext variables, IVariableProvider arguments) {
            return Converter.Convert<T>(Execute(variables, arguments));
        }

        /// <summary>
        /// evaluates the result of the token
        /// </summary>
        /// <returns>result of statement</returns>
        protected abstract object ExecuteToken(IVariableContext variables, IVariableProvider arguments);
    }
}