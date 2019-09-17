using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which represents multiple values
    /// </summary>
    class ScriptArray : ScriptToken {
        readonly IScriptToken[] values;

        /// <summary>
        /// creates a new <see cref="ScriptArray"/>
        /// </summary>
        /// <param name="values">values in array</param>
        public ScriptArray(IScriptToken[] values) {
            this.values = values;
        }

        /// <summary>
        /// values contained in array
        /// </summary>
        public IEnumerable<IScriptToken> Values => values;

        /// <inheritdoc />
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            return values.Select(v => v.Execute(variables, arguments)).ToArray();
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"[{string.Join(", ", values.Select(v => v.ToString()))}]";
        }
    }
}