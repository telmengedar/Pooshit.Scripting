using System;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Operations {

    /// <summary>
    /// base implementation which handles errors
    /// </summary>
    public abstract class AssignableToken : ScriptToken, IAssignableToken {

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="AssignToken"/> (eg. <c>ScriptVariable.AssignToken</c>) executes the right-hand side
        /// expression inline, so a cancellation or step-limit abort raised while evaluating it (eg.
        /// <c>$c = $src.count()</c>) must reach the caller unwrapped, exactly like any other checkpoint.
        /// </remarks>
        public object Assign(IScriptToken token, ScriptContext context) {
            try {
                return AssignToken(token, context);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute assignment '{this}'\n{e.Message}", token, e);
            }
        }

        /// <summary>
        /// executes assignment
        /// </summary>
        /// <param name="token">token with value to assign</param>
        /// <param name="context">script execution context</param>
        /// <returns>result of assignment</returns>
        protected abstract object AssignToken(IScriptToken token, ScriptContext context);
    }
}