using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Data {

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
        public object Invoke(IVariableProvider parentvariables, params object[] arguments) {
            return script.Execute(new VariableProvider(new Variable("arguments", arguments)));
        }

        /// <inheritdoc />
        public override string ToString() {
            return "External Script";
        }
    }
}