namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// token to which a value can be assigned
    /// </summary>
    public interface IAssignableToken : IScriptToken {

        /// <summary>
        /// assigns a value to this token
        /// </summary>
        /// <param name="token">token resulting in value to assign to this token</param>
        /// <returns>resulting value after assignment</returns>
        object Assign(IScriptToken token);
    }
}