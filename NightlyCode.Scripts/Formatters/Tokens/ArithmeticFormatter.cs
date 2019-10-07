using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ArithmeticBlock"/>s
    /// </summary>
    public class ArithmeticFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ArithmeticBlock block = (ArithmeticBlock) token;
            resulttext.Append('(');
            formatters[block.InnerBlock].FormatToken(block.InnerBlock, resulttext, formatters, depth);
            resulttext.Append(')');
        }
    }
}