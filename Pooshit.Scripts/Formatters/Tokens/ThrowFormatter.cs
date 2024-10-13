using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Throw"/>s
    /// </summary>
    public class ThrowFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            Throw throwtoken = (Throw) token;

            resulttext.Append("throw(");
            formatters[throwtoken.Message].FormatToken(throwtoken.Message, resulttext, formatters, depth);
            if (throwtoken.Context != null) {
                resulttext.Append(", ");
                formatters[throwtoken.Context].FormatToken(throwtoken.Context, resulttext, formatters, depth);
            }
            resulttext.Append(")");
        }
    }
}