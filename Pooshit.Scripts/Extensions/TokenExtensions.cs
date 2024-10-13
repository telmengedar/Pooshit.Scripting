using System.Collections.Generic;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Extensions {

    /// <summary>
    /// extensions for <see cref="IScriptToken"/>s
    /// </summary>
    public static class TokenExtensions {

        /// <summary>
        /// unwraps all comment lines of a series of comments
        /// </summary>
        /// <param name="comments">comments to unwrap</param>
        /// <returns>comment lines contained in comment tokens</returns>
        public static IEnumerable<string> Unwrap(this IEnumerable<Comment> comments) {
            foreach (Comment comment in comments) {
                foreach (string line in comment.Text.Split('\n'))
                    yield return line.TrimEnd('\r');
            }
        }

        /// <summary>
        /// executes a script and converts the result
        /// </summary>
        /// <typeparam name="T">type to convert result to</typeparam>
        /// <param name="token">token to execute</param>
        /// <param name="context">context to provide for execution</param>
        /// <returns>converted token result</returns>
        public static T Execute<T>(this IScriptToken token, ScriptContext context) {
            return Converter.Convert<T>(token.Execute(context));
        }
    }
}