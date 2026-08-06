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
        /// The depth budget deliberately does, since it bounds a shared physical resource rather than
        /// per-script work — see docs/architecture/execution-guards-depth-memory.md §7.3. A foreign
        /// <see cref="IScript"/> implementation has no way to accept the inherited budget and does not
        /// inherit depth.
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