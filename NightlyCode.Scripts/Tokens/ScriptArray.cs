using System.Collections.Generic;
using System.Linq;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which represents multiple values
    /// </summary>
    public class ScriptArray : ScriptToken {
        readonly IScriptToken[] values;

        /// <summary>
        /// creates a new <see cref="ScriptArray"/>
        /// </summary>
        /// <param name="values">values in array</param>
        internal ScriptArray(IScriptToken[] values) {
            this.values = values;
        }

        /// <summary>
        /// values contained in array
        /// </summary>
        public IEnumerable<IScriptToken> Values => values;

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return values.Select(v => v.Execute(context)).ToArray();
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"[{string.Join(", ", values.Select(v => v.ToString()))}]";
        }
    }
}