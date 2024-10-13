using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="NewLine"/>s
    /// </summary>
    public class NewLineFormatter : ITokenFormatter {

        /// <inheritdoc />
        public void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustindent = false) {
        }
    }
}