using System.Linq;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// case for a <see cref="Switch"/> statement
    /// </summary>
    class Case : IControlToken {
        readonly IScriptToken[] conditions;

        /// <summary>
        /// creates a new <see cref="Case"/>
        /// </summary>
        public Case() { }

        /// <summary>
        /// creates a new <see cref="Case"/>
        /// </summary>
        /// <param name="conditions">conditions to match</param>
        public Case(IScriptToken[] conditions) {
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
        /// <returns>true if case matches value, false otherwise</returns>
        public bool Matches(object value) {
            return conditions.Any(c => c.Execute().Equals(value));
        }

        /// <inheritdoc />
        public object Execute() {
            return Body.Execute();
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }

        /// <inheritdoc />
        public override string ToString() {
            return $"case({string.Join<IScriptToken>(", ", conditions)}) {Body}";
        }
    }
}