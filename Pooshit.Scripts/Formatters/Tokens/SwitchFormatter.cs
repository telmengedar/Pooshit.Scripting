using System.Text;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Control.Internal;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Switch"/>
    /// </summary>
    public class SwitchFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            Switch switchtoken = (Switch) token;
            resulttext.Append("switch(");
            foreach (IScriptToken parameter in switchtoken.Parameters)
                formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
            resulttext.AppendLine(")");

            foreach (IScriptToken child in ((ListStatementBlock)switchtoken.Body).Children) {
                AppendIntendation(resulttext, depth);
                if (child is Case @case) {
                    if (!@case.IsDefault) {
                        resulttext.Append("case(");
                        foreach (IScriptToken parameter in @case.Parameters) {
                            formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                            resulttext.Append(", ");
                        }

                        resulttext.Length -= 2;
                        resulttext.Append(")");
                    }
                    else {
                        resulttext.Append("default");
                    }

                    if (!(@case.Body is StatementBlock))
                        resulttext.AppendLine();
                    formatters[@case.Body].FormatToken(@case.Body, resulttext, formatters, depth + 1, true);
                }
                else formatters[child].FormatToken(child, resulttext, formatters, depth);
                resulttext.AppendLine();
            }

            resulttext.Length -= 2;
        }
    }
}