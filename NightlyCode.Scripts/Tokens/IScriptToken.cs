using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token of a script which can get executed
    /// </summary>
    public interface IScriptToken {

        /// <summary>
        /// executes the token returning a result
        /// </summary>
        /// <returns>result of token call</returns>
        object Execute(IVariableProvider arguments);

        /// <summary>
        /// executes the token returning a result
        /// </summary>
        /// <returns>result of token call</returns>
        T Execute<T>(IVariableProvider arguments);
    }
}