using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Try"/> / Catch blocks
    /// </summary>
    public class TryFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            Try trytoken = (Try) token;
            resulttext.Append("try");
            if (!(trytoken.Body is StatementBlock))
                resulttext.AppendLine();
            formatters[trytoken.Body].FormatToken(trytoken.Body, resulttext, formatters, depth + 1, true);
            if (trytoken.Catch != null) {
                resulttext.AppendLine();
                AppendIntendation(resulttext, depth);
                formatters[trytoken.Catch].FormatToken(trytoken.Catch, resulttext, formatters, depth);
            }
        }
    }
}