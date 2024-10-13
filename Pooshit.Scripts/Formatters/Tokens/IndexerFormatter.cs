using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ScriptIndexer"/>
    /// </summary>
    public class IndexerFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptIndexer indexer = (ScriptIndexer) token;
            formatters[indexer.Host].FormatToken(indexer.Host, resulttext, formatters, depth);
            resulttext.Append('[');
            foreach (IScriptToken parameter in indexer.Parameters) {
                formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                resulttext.Append(", ");
            }

            resulttext.Length -= 2;
            resulttext.Append(']');
        }
    }
}