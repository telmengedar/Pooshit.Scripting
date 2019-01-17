using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token representing a flow control statement
    /// </summary>
    interface IControlToken : IScriptToken {

        /// <summary>
        /// body of control statement
        /// </summary>
        IScriptToken Body { get; }
    }
}