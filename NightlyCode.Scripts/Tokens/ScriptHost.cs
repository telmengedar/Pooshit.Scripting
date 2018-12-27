namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token providing a known host instance
    /// </summary>
    public class ScriptHost : IScriptToken {
        readonly IScriptHosts hostpool;
        readonly string hostname;

        /// <summary>
        /// creates a new <see cref="ScriptHost"/>
        /// </summary>
        /// <param name="hostpool">pool containing available hosts</param>
        /// <param name="hostname">name of host</param>
        public ScriptHost(IScriptHosts hostpool, string hostname) {
            this.hostpool = hostpool;
            this.hostname = hostname;
        }

        /// <inheritdoc />
        public object Execute() {
            return hostpool.GetVariable(hostname);
        }

        public object Assign(IScriptToken token) {
            throw new ScriptException("Assignment to a script host not supported");
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"{hostname}";
        }
    }
}