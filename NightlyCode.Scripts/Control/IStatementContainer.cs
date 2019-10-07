using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token representing a flow control statement
    /// </summary>
    public interface IStatementContainer : IScriptToken {

        /// <summary>
        /// body of control statement
        /// </summary>
        IScriptToken Body { get; }
    }
}