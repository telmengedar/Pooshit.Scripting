using System.Linq;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which represents multiple values
    /// </summary>
    public class ScriptArray : IScriptToken {
        readonly IScriptToken[] values;

        /// <summary>
        /// creates a new <see cref="ScriptArray"/>
        /// </summary>
        /// <param name="values">values in array</param>
        public ScriptArray(IScriptToken[] values) {
            this.values = values;
        }

        /// <inheritdoc />
        public object Execute() {
            return values.Select(v => v.Execute()).ToArray();
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"[{string.Join(", ", values.Select(v => v.ToString()))}]";
        }
    }
}