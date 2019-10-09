using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formatter to use if no formatter applies
    /// </summary>
    public class DefaultFormatter : TokenFormatter {

        /// <summary>
        /// static instance of a default formatter
        /// </summary>
        public static readonly DefaultFormatter Instance = new DefaultFormatter();

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            FormatLinkedComments(token, resulttext, depth);
            resulttext.Append(token.Literal);

            if (token is IParameterContainer parametertoken && parametertoken.Parameters != null) {
                resulttext.Append('(');
                if (parametertoken.Parameters.Any()) {
                    foreach (IScriptToken parameter in parametertoken.Parameters) {
                        formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                        resulttext.Append(", ");
                    }
                    resulttext.Length -= 2;
                }

                resulttext.Append(')');
            }

            if (token is IStatementContainer controltoken) {
                if (!(controltoken.Body is StatementBlock))
                    resulttext.AppendLine();
                formatters[controltoken.Body].FormatToken(controltoken.Body, resulttext, formatters, depth + 1, true);
            }
        }
    }
}