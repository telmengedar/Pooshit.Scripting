using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// interface for a provider of type-instances
    /// </summary>
    public interface ITypeInstanceProvider {

        /// <summary>
        /// creates an instance using the specified parameters
        /// </summary>
        /// <param name="parameters">parameters to use to create the instance</param>
        /// <returns>created instance</returns>
        object Create(params IScriptToken[] parameters);
    }
}