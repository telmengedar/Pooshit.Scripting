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
        /// <param name="name">name of script</param>
        /// <param name="script"></param>
        public ExternalScriptMethod(string name, IScript script) {
            this.script = script;
            Name = name;
        }

        /// <summary>
        /// name of script
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        public object Invoke(IVariableProvider parentvariables, params object[] arguments) {
            return script.Execute(new VariableProvider(new Variable("arguments", arguments)));
        }

        /// <inheritdoc />
        public override string ToString() {
            return Name;
        }
    }
}