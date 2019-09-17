using System.Collections.Generic;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token which takes parameters
    /// </summary>
    public interface IParameterizedToken {

        /// <summary>
        /// parameters for token
        /// </summary>
        IEnumerable<IScriptToken> Parameters { get; }
    }
}