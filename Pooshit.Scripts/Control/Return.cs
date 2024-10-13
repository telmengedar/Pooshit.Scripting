using System.Collections.Generic;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// returns a value and end execution of current method
    /// </summary>
    public class Return : ScriptToken, IParameterContainer {
        readonly IScriptToken value;

        /// <summary>
        /// creates a new <see cref="Return"/>
        /// </summary>
        /// <param name="value">token to return</param>
        internal Return(IScriptToken value) {
            this.value = value;
        }

        /// <summary>
        /// token resulting in value to return
        /// </summary>
        public IScriptToken Value => value;

        /// <inheritdoc />
        public override string Literal => "return";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return new Return(new ScriptValue(Value?.Execute(context)));
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"return {value}";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                if (value != null)
                    yield return value;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => true;
    }
}