using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// token representing a control structure
    /// </summary>
    public abstract class ControlToken : ScriptToken, IStatementContainer {

        /// <inheritdoc />
        public abstract IScriptToken Body { get; internal set; }
    }
}