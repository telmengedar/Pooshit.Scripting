using System.Text;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats a statement body for a script
    /// </summary>
    public class StatementBlockFormatter : ITokenFormatter {

        /// <inheritdoc />
        public void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustindent=false) {
            StatementBlock block = (StatementBlock) token;

            if (depth > -1)
                resulttext.AppendLine(" {");
            foreach (IScriptToken child in block.Children) {
                formatters[child].FormatToken(child, resulttext, formatters, depth >= 0 ? depth : 0, true);
                resulttext.AppendLine();
            }

            if (depth > -1) {
                for (int i = 0; i < depth-1; ++i)
                    resulttext.Append('\t');
                resulttext.Append('}');
            }

            if (depth < 0) {
                while (resulttext[resulttext.Length-1] == '\r' || resulttext[resulttext.Length-1] == '\n')
                    --resulttext.Length;
            }
        }
    }
}