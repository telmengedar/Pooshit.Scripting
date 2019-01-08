﻿namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token of a script which can get executed
    /// </summary>
    interface IScriptToken {

        /// <summary>
        /// executes the token returning a result
        /// </summary>
        /// <returns>result of token call</returns>
        object Execute();
    }
}