using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Resolvers {

    /// <summary>
    /// method to be called
    /// </summary>
    public interface IResolvedMethod {

        /// <summary>
        /// calls a method
        /// </summary>
        /// <param name="methodcall">original method token</param>
        /// <param name="host">host for which to call method</param>
        /// <param name="parameters">parameters for method call</param>
        /// <param name="context">script context</param>
        /// <returns>return value of method</returns>
        object Call(IScriptToken methodcall, object host, object[] parameters, ScriptContext context);
    }
}