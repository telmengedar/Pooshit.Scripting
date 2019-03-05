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

        /// <summary>
        /// invokes the method with the specified arguments
        /// </summary>
        /// <param name="arguments">arguments for script method</param>
        /// <returns>result of script execution</returns>
        public object Invoke(params object[] arguments) {
            return script.Execute(new Variable("arguments", arguments));
        }
    }
}