namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token representing a flow control statement
    /// </summary>
    public interface IControlToken : IScriptToken {

        /// <summary>
        /// body of control statement
        /// </summary>
        IScriptToken Body { get; set; }
    }
}