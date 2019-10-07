using System.Text;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="IUnaryToken"/>s
    /// </summary>
    public class UnaryTokenFormatter : TokenFormatter {
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            IUnaryToken unarytoken = (IUnaryToken) token;

            if (unarytoken.IsPostToken) {
                formatters[unarytoken.Operand].FormatToken(unarytoken.Operand, resulttext, formatters, depth);
                resulttext.Append(unarytoken.Literal);
            }
            else {
                resulttext.Append(unarytoken.Literal);
                formatters[unarytoken.Operand].FormatToken(unarytoken.Operand, resulttext, formatters, depth);
            }
        }
    }
}