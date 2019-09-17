using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// imports a script method using the <see cref="IExternalMethodProvider"/> of the parser
    /// </summary>
    public class Import : ScriptToken {
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
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            return methodprovider.Import(key.Execute<string>(variables, arguments));
        }
    }
}