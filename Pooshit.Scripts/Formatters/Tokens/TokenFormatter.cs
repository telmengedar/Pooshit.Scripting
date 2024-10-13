using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <inheritdoc />
    public abstract class TokenFormatter : ITokenFormatter {

        /// <summary>
        /// formats a body for a control token
        /// </summary>
        /// <param name="token">body token</param>
        /// <param name="resulttext">formatted result target</param>
        /// <param name="formatters">collection of token formatters</param>
        /// <param name="depth">indentation depth (should be depth of control token)</param>
        protected void FormatBody(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth) {
            if (!(token is StatementBlock))
                resulttext.AppendLine();
            formatters[token].FormatToken(token, resulttext, formatters, depth + 1, true);
        }

        /// <summary>
        /// formats a token
        /// </summary>
        /// <param name="token">token to format</param>
        /// <param name="resulttext">text to append formatted token to</param>
        /// <param name="formatters">formatter collection to retrieve token formatters from</param>
        /// <param name="depth">current indentation depth</param>
        protected abstract void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0);

        /// <summary>
        /// creates an intendation for a specified depth
        /// </summary>
        /// <param name="resulttext">text to append intendation for</param>
        /// <param name="depth">indentation depth</param>
        protected void AppendIntendation(StringBuilder resulttext, int depth) {
            for (int i = 0; i < depth; ++i)
                resulttext.Append('\t');
        }

        /// <inheritdoc />
        public void FormatToken(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0, bool mustident=false) {
            if(mustident)
                AppendIntendation(resulttext, depth);
            Format(token, resulttext, formatters, depth);
        }
    }
}