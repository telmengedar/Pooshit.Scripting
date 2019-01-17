using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// base implementation which handles errors
    /// </summary>
    public abstract class AssignableToken : ScriptToken, IAssignableToken {

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            try {
                return AssignToken(token);
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute assignment '{this}'", e.Message, e);
            }
        }

        /// <summary>
        /// executes assignment
        /// </summary>
        /// <param name="token">token with value to assign</param>
        /// <returns>result of assignment</returns>
        protected abstract object AssignToken(IScriptToken token);
    }
}