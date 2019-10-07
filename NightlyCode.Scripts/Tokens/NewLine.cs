namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token containing an empty line for human formatting purposes
    /// </summary>
    public class NewLine : IScriptToken {

        public string Literal => "";

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            return null;
        }

        /// <inheritdoc />
        public T Execute<T>(ScriptContext context) {
            return default(T);
        }

        /// <inheritdoc />
        public override string ToString() {
            return "\n";
        }
    }
}