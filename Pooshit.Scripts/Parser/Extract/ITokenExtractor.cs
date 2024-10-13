using System;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Parser.Extract {

    /// <summary>
    /// extracts single tokens from scripts
    /// </summary>
    public interface ITokenExtractor {

        /// <summary>
        /// extracts token which is stored at the specified position
        /// </summary>
        /// <param name="data">script data to analyse</param>
        /// <param name="position">position at which to extract token</param>
        /// <param name="fulltoken">determines whether to extract the full token or just the token literal up until the specified position</param>
        /// <param name="filter">filter for tokens to match to get considered for extraction(optional)</param>
        /// <returns>extracted token</returns>
        IScriptToken ExtractToken(string data, int position, bool fulltoken=true, Func<IScriptToken, bool> filter=null);
    }
}