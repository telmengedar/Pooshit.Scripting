using System.Text;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="IBinaryToken"/>s
    /// </summary>
    public class BinaryTokenFormatter : TokenFormatter {

        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            IBinaryToken binary = (IBinaryToken) token;
            formatters[binary.Lhs].FormatToken(binary.Lhs, resulttext, formatters);
            resulttext.Append(' ').Append(token.Literal).Append(' ');
            formatters[binary.Rhs].FormatToken(binary.Rhs, resulttext, formatters);
        }
    }
}