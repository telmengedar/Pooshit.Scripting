using System.Linq;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// case for a <see cref="Switch"/> statement
    /// </summary>
    public class Case : IControlToken {
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

        public bool Matches(object value) {
            return conditions.Any(c => c.Execute().Equals(value));
        }

        public object Execute() {
            Body.Execute();
            return null;
        }

        public object Assign(IScriptToken token) {
            throw new System.NotImplementedException();
        }

        public IScriptToken Body { get; set; }

        public override string ToString() {
            return $"case({string.Join<IScriptToken>(", ", conditions)}) {Body}";
        }
    }
}