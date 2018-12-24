namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// token applying an operation to a single operand
    /// </summary>
    public interface IUnaryToken : IScriptToken {

        /// <summary>
        /// operand for operation
        /// </summary>
        IScriptToken Operand { get; set; }
    }
}