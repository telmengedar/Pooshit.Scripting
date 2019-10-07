using System.Collections.Generic;
using System.Linq;
using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="DictionaryToken"/>s
    /// </summary>
    public class DictionaryFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth) {
            DictionaryToken dictionary = (DictionaryToken) token;
            resulttext.Append("{");
            if (dictionary.Entries.Any()) {
                foreach (KeyValuePair<IScriptToken, IScriptToken> entry in dictionary.Entries) {
                    resulttext.AppendLine();
                    formatters[entry.Key].FormatToken(entry.Key, resulttext, formatters, depth + 1, true);
                    resulttext.Append(" : ");
                    formatters[entry.Value].FormatToken(entry.Value, resulttext, formatters, depth + 1);
                    resulttext.Append(',');
                }

                --resulttext.Length;
                resulttext.AppendLine();
                AppendIntendation(resulttext, depth);
                resulttext.Append('}');
            }
            else resulttext.Append('}');

        }
    }
}