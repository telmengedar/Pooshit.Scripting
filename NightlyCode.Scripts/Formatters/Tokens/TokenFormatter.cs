using System.Linq;
using System.Text;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <inheritdoc />
    public abstract class TokenFormatter : ITokenFormatter {

        /// <summary>
        /// formats comments linked to a token
        /// </summary>
        /// <param name="token">token to format</param>
        /// <param name="resulttext">text to append formatted result to</param>
        /// <param name="depth">current indentation depth</param>
        protected void FormatLinkedComments(IScriptToken token, StringBuilder resulttext, int depth = 0) {
            ICommentContainer comments = token as ICommentContainer;
            if (comments?.Comments.Any() ?? false) {
                foreach (string commentline in comments.Comments.Unwrap()) {
                    resulttext.Append("//");
                    if (!commentline.StartsWith(" "))
                        resulttext.Append(" ");
                    resulttext.Append(commentline);
                    resulttext.AppendLine();
                    AppendIntendation(resulttext, depth);
                }
            }
        }

        /// <summary>
        /// formats a token
        /// </summary>
        /// <param name="token">token to format</param>
        /// <param name="resulttext">text to append formatted token to</param>
        /// <param name="formatters">formatter collection to retrieve token formatters from</param>
        /// <param name="depth">current indentation depth</param>
        protected abstract void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0);

        /// <summary>
        /// creates an intendation for a specified depth
        /// </summary>
        /// <param name="resulttext">text to append intendation for</param>
        /// <param name="depth">indentation depth</param>
        protected void AppendIntendation(StringBuilder resulttext, int depth) {
            for (int i = 0; i < depth; ++i)
                resulttext.Append('\t');
        }

        /// <inheritdoc />
        public void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustident=false) {
            if(mustident)
                AppendIntendation(resulttext, depth);
            Format(token, resulttext, formatters, depth);
        }
    }
}