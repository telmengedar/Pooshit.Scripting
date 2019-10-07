using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Comment"/>s
    /// </summary>
    public class CommentFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            Comment comment = (Comment) token;
            if (comment.IsMultiline) {
                resulttext.Append("/*").Append(comment.Text).Append("*/");
            }
            else {
                resulttext.Append("//").Append(comment.Text);
            }
        }
    }
}