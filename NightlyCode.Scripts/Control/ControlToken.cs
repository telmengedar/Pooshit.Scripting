using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token representing a control structure
    /// </summary>
    public abstract class ControlToken : ScriptToken, IControlToken {

        /// <inheritdoc />
        public abstract IScriptToken Body { get; internal set; }
    }
}