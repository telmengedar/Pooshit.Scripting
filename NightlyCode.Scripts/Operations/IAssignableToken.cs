using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// token to which a value can be assigned
    /// </summary>
    interface IAssignableToken : IScriptToken {

        /// <summary>
        /// assigns a value to this token
        /// </summary>
        /// <param name="token">token resulting in value to assign to this token</param>
        /// <param name="context">script execution context</param>
        /// <returns>resulting value after assignment</returns>
        object Assign(IScriptToken token, ScriptContext context);
    }
}