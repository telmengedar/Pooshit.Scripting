using System.Linq;
using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ScriptArray"/>s
    /// </summary>
    public class ArrayFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptArray array = (ScriptArray) token;
            resulttext.Append('[');

            if (array.Values.Any()) {
                foreach (IScriptToken value in array.Values) {
                    formatters[value].FormatToken(value, resulttext, formatters, depth);
                    resulttext.Append(", ");
                }

                resulttext.Length -= 2;
            }

            resulttext.Append(']');
        }
    }
}