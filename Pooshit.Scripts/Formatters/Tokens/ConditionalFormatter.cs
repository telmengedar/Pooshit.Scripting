using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ConditionalToken"/> as <c>cond ? a : b</c>
    /// </summary>
    public class ConditionalFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ConditionalToken conditional = (ConditionalToken)token;
            formatters[conditional.Condition].FormatToken(conditional.Condition, resulttext, formatters);
            resulttext.Append(" ? ");
            formatters[conditional.WhenTrue].FormatToken(conditional.WhenTrue, resulttext, formatters);
            resulttext.Append(" : ");
            formatters[conditional.WhenFalse].FormatToken(conditional.WhenFalse, resulttext, formatters);
        }
    }
}
