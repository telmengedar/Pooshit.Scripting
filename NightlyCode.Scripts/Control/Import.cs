using System.Collections.Generic;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// imports a script method using the <see cref="IExternalMethodProvider"/> of the parser
    /// </summary>
    public class Import : ScriptToken, IParameterContainer {
        readonly IScriptToken key;
        readonly IExternalMethodProvider methodprovider;
        
        /// <summary>
        /// creates a new <see cref="Import"/>
        /// </summary>
        /// <param name="methodprovider"></param>
        /// <param name="key"></param>
        public Import(IExternalMethodProvider methodprovider, IScriptToken key) {
            this.methodprovider = methodprovider;
            this.key = key;
        }

        /// <summary>
        /// reference to import
        /// </summary>
        public IScriptToken Reference => key;

        /// <inheritdoc />
        public override string Literal => "import";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return methodprovider.Import(key.Execute<string>(context));
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get { yield return key; }
        }
    }
}