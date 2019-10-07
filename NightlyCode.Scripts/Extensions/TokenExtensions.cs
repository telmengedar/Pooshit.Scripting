using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Extensions {

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
    }
}