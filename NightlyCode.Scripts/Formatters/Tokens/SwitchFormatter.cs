using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="Switch"/>
    /// </summary>
    public class SwitchFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            FormatLinkedComments(token, resulttext, depth);
            Switch switchtoken = (Switch) token;
            resulttext.Append("switch(");
            foreach (IScriptToken parameter in switchtoken.Parameters)
                formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
            resulttext.AppendLine(")");

            if (switchtoken.Cases.Any()) {
                foreach (Case casetoken in switchtoken.Cases) {
                    AppendIntendation(resulttext, depth);
                    FormatLinkedComments(casetoken, resulttext, depth);
                    resulttext.Append("case(");
                    foreach (IScriptToken parameter in casetoken.Parameters) {
                        formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                        resulttext.Append(", ");
                    }

                    resulttext.Length -= 2;
                    resulttext.Append(")");

                    if (!(casetoken.Body is StatementBlock))
                        resulttext.AppendLine();
                    formatters[casetoken.Body].FormatToken(casetoken.Body, resulttext, formatters, depth + 1, true);
                    resulttext.AppendLine();
                }
            }

            if (switchtoken.Default != null) {
                AppendIntendation(resulttext, depth);
                resulttext.Append("default");
                if (!(switchtoken.Default.Body is StatementBlock))
                    resulttext.AppendLine();
                formatters[switchtoken.Default.Body].FormatToken(switchtoken.Default.Body, resulttext, formatters, depth + 1, true);
                resulttext.AppendLine();
            }

            resulttext.Length -= 2;
        }
    }
}