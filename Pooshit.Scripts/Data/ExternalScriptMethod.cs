using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting.Data {

    /// <summary>
    /// script method provided by resolver
    /// </summary>
    public class ExternalScriptMethod : IExternalMethod {
        readonly IScript script;

        /// <summary>
        /// creates a new <see cref="ExternalScriptMethod"/>
        /// </summary>
        /// <param name="script"></param>
        public ExternalScriptMethod(IScript script) {
            this.script = script;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The caller's cancellation token (and, when a timeout is configured, its deadline) carries through
        /// so an imported script can no longer run in a completely uncancellable region. The step budget
        /// deliberately does not cross this boundary — see docs/architecture/cancellation-support.md §7.8.
        /// The depth budget deliberately does — unlike a step budget, which bounds a per-script work
        /// allowance, a depth budget bounds a shared physical resource (there is exactly one call stack), so
        /// partitioning it per imported script would let a mutually-recursive import chain recurse to a
        /// <see cref="System.StackOverflowException"/> regardless of either script's own ceiling. When the
        /// nested script is the engine's own <see cref="Script"/> implementation, the caller's depth budget is
        /// handed down so the nested script's own lambda invocations continue the same shared count; a
        /// foreign <see cref="IScript"/> implementation has no way to accept it and does not inherit depth —
        /// see docs/architecture/execution-guards-depth-memory.md §7.3/§11.
        /// </remarks>
        public object Invoke(ScriptContext context, params object[] arguments) {
            DepthBudget depthBudget = context.DepthBudget;
            try {
                depthBudget?.Enter();

                VariableProvider variables = new(new Variable("arguments", arguments));
                return script is Script engineScript
                    ? engineScript.Execute(variables, context.CancellationToken, depthBudget)
                    : script.Execute(variables, context.CancellationToken);
            }
            finally {
                depthBudget?.Exit();
            }
        }

        /// <inheritdoc />
        public override string ToString() {
            return "External Script";
        }
    }
}