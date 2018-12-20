namespace NightlyCode.Scripting {

    /// <summary>
    /// token of a script which can get executed
    /// </summary>
    public interface IScriptToken {

        /// <summary>
        /// executes the token returning a result
        /// </summary>
        /// <returns>result of token call</returns>
        object Execute();

        /// <summary>
        /// assigns a value to the token
        /// </summary>
        /// <param name="token">token containing value to assign</param>
        /// <returns>assigned value</returns>
        object Assign(IScriptToken token);
    }
}