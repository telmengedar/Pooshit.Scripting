using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ImpliciteTypeCast"/>s
    /// </summary>
    public class CastFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ImpliciteTypeCast cast = (ImpliciteTypeCast) token;

            resulttext.Append(cast.Keyword).Append('(');
            formatters[cast.Argument].FormatToken(cast.Argument, resulttext, formatters, depth);
            resulttext.Append(')');
        }
    }
}