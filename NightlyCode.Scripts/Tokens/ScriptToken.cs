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
        public object Execute(IVariableProvider arguments = null) {
            try {
                return ExecuteToken(arguments);
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute '{this}'", e.Message, e);
            }
        }

        /// <inheritdoc />
        public T Execute<T>(IVariableProvider arguments) {
            return Converter.Convert<T>(Execute(arguments));
        }

        /// <summary>
        /// evaluates the result of the token
        /// </summary>
        /// <returns>result of statement</returns>
        protected abstract object ExecuteToken(IVariableProvider arguments);
    }
}