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
        /// </remarks>
        public object Invoke(ScriptContext context, params object[] arguments) {
            return script.Execute(new VariableProvider(new Variable("arguments", arguments)), context.CancellationToken);
        }

        /// <inheritdoc />
        public override string ToString() {
            return "External Script";
        }
    }
}