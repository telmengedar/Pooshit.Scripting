using System;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Tokens {
    /// <summary>
    /// base implementation of a script token with basic error handling
    /// </summary>
    public abstract class ScriptToken : ICodePositionToken {

        /// <inheritdoc />
        public abstract string Literal { get; }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            try {
                return ExecuteToken(context);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute '{this}'\n{e.Message}", this, e);
            }
        }

        /// <summary>
        /// evaluates the result of the token
        /// </summary>
        /// <returns>result of statement</returns>
        protected abstract object ExecuteToken(ScriptContext context);

        /// <inheritdoc />
        public int LineNumber { get; internal set; }

        /// <inheritdoc />
        public int TextIndex { get; internal set; }

        /// <inheritdoc />
        public int TokenLength { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return Literal;
        }
    }
}