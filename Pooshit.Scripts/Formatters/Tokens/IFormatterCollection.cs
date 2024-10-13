using System;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// collection of available formatters
    /// </summary>
    public interface IFormatterCollection {

        /// <summary>
        /// get a formatter for the specified token
        /// </summary>
        /// <param name="tokentype">type of token to get formatter for</param>
        /// <returns>formatter to use to format token</returns>
        ITokenFormatter this[IScriptToken tokentype] { get; }
    }
}