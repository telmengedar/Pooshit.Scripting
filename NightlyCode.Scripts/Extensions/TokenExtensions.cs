using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extension methods for tokens
    /// </summary>
    public static class TokenExtensions {

        /// <summary>
        /// calls an operator for a token result
        /// </summary>
        /// <param name="token">token on which to call operator</param>
        /// <param name="operatorname">name of operator to call</param>
        /// <param name="parameters">parameters for operator</param>
        /// <returns>result of operator call</returns>
        public static object CallOperator(this IScriptToken token, string operatorname, params IScriptToken[] parameters) {
            object host = token.Execute();
            MethodInfo[] methods = host.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == operatorname).ToArray();

            StringBuilder executionlog = new StringBuilder();
            foreach (MethodInfo method in methods) {
                try
                {
                    return MethodOperations.CallMethod(null, method, parameters);
                }
                catch (Exception e)
                {
                    executionlog.AppendLine(e.Message);
                }
            }

            if (executionlog.Length == 0)
                throw new ScriptException($"Operator '{operatorname}' matching the specified parameters count not found on type {host.GetType().Name}", executionlog.ToString());

            throw new ScriptException("None of the matching methods could be invoked using the specified parameters", executionlog.ToString());
        }
    }
}