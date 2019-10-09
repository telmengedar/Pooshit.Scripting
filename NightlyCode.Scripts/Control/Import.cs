using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// imports a script method using the <see cref="IExternalMethodProvider"/> of the parser
    /// </summary>
    public class Import : ScriptToken, IParameterContainer {
        readonly IScriptToken[] parameters;
        readonly IExternalMethodProvider methodprovider;

        /// <summary>
        /// creates a new <see cref="Import"/>
        /// </summary>
        /// <param name="methodprovider"></param>
        /// <param name="key"></param>
        public Import(IExternalMethodProvider methodprovider, params IScriptToken[] key) {
            this.methodprovider = methodprovider;
            this.parameters = key;
        }

        /// <inheritdoc />
        public override string Literal => "import";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return methodprovider.Import(parameters.Select(p=>p.Execute(context)).ToArray());
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters => parameters;
    }
}