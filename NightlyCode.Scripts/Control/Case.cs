using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// case for a <see cref="Switch"/> statement
    /// </summary>
    public class Case : ControlToken, IParameterContainer {
        readonly IScriptToken[] conditions;

        /// <summary>
        /// creates a new <see cref="Case"/>
        /// </summary>
        internal Case() { }

        /// <summary>
        /// creates a new <see cref="Case"/>
        /// </summary>
        /// <param name="conditions">conditions to match</param>
        internal Case(IScriptToken[] conditions) {
            this.conditions = conditions;
        }

        /// <summary>
        /// determines whether this is the default case
        /// </summary>
        public bool IsDefault => conditions == null;

        /// <summary>
        /// determines whether case matches a value
        /// </summary>
        /// <param name="value">value to match</param>
        /// <param name="context">script execution context</param>
        /// <returns>true if case matches value, false otherwise</returns>
        public bool Matches(object value, ScriptContext context) {
            return conditions.Any(c => value.IsEqual(c.Execute(context)));
        }

        /// <inheritdoc />
        public override string Literal => "case";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Body.Execute(context);
        }

        /// <inheritdoc />
        public override IScriptToken Body { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"case({string.Join<IScriptToken>(", ", conditions)}) {Body}";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                if(IsDefault)
                    yield break;

                foreach(IScriptToken parameter in conditions)
                    yield return parameter;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}