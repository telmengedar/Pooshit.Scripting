using System;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// statement wrapping a body for exception handling
    /// </summary>
    public class Try : ControlToken {

        internal Try() {
        }

        /// <inheritdoc />
        public override string Literal => "try";

        /// <inheritdoc />
        /// <remarks>
        /// An engine-driven cancellation must never be swallowable by script <c>try</c>/<c>catch</c>, or a
        /// host watchdog cancelling the context's own token could never reliably stop a script — hence the
        /// dedicated rethrow below, guarded on the context token's own cancelled state rather than a
        /// blanket exception-type check. A task cancelled by a host's own <em>unrelated</em> token is not
        /// affected: it is not this context's token, so it still reaches the generic catch as ordinary,
        /// catchable script control flow. Any <see cref="ScriptAbortException"/> — a step-limit overrun, an
        /// execution timeout, or a recursion-depth breach — is likewise the engine aborting execution, not a
        /// script-level error, and is rethrown unconditionally. This closes DiVoid #7734: an imported
        /// script's own <see cref="ScriptTimeoutException"/> reaches this <c>try</c> from inside the outer
        /// script's token tree, and was previously swallowed here because only the step-limit type was
        /// named; catching the base rather than enumerating leaf types covers every abort, present and
        /// future, without this site needing to be revisited again.
        /// </remarks>
        protected override object ExecuteToken(ScriptContext context) {
            try {
                return Body.Execute(context);
            }
            catch(OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) {
                throw;
            }
            catch(ScriptAbortException) {
                throw;
            }
            catch(Exception e) {
                if(Catch != null) {
                    ScriptContext catchcontext = new ScriptContext(context);
                    catchcontext.Arguments["exception"] = e;
                    return Catch?.Execute(catchcontext);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <summary>
        /// body to execute when condition is not met
        /// </summary>
        public Catch Catch { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            if(Catch != null)
                return $"try {Body} {Catch}";
            return $"try {Body}";
        }
    }
}