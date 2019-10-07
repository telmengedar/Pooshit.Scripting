using System.Collections.Generic;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// control token which is based on a condition
    /// </summary>
    public interface IParameterContainer : IScriptToken {

        /// <summary>
        /// evaluated condition
        /// </summary>
        IEnumerable<IScriptToken> Parameters { get; }
    }
}