using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Comment"/>s
    /// </summary>
    public class CommentFormatter : TokenFormatter {

        int CountTabs(string line) {
            int tabs = 0;
            foreach (char character in line) {
                if (character != '\t')
                    break;
                ++tabs;
            }

            return tabs;
        }
        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            Comment comment = (Comment) token;
            if (comment.IsMultiline && comment.Text.Contains("\n")) {
                resulttext.Append("/* ");
                resulttext.Append(comment.Text);
                resulttext.Append(" */");
            }
            else {
                if (comment.IsPostComment) {
                    bool done = false;
                    while (resulttext.Length>0 && !done) {
                        switch (resulttext[resulttext.Length - 1]) {
                        case '\t':
                        case '\r':
                        case '\n':
                            --resulttext.Length;
                            break;
                        default:
                            done = true;
                            break;
                        }
                    }

                    if (resulttext.Length > 0)
                        resulttext.Append(' ');
                }
                resulttext.Append("//").Append(comment.Text);
            }
        }
    }
}