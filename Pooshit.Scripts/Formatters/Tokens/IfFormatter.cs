using System.Text;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="If"/>
    /// </summary>
    public class IfFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            If iftoken = (If) token;
            resulttext.Append("if(");
            foreach (IScriptToken parameter in iftoken.Parameters)
                formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
            resulttext.Append(")");

            FormatBody(iftoken.Body, resulttext, formatters, depth);
            if (iftoken.Else != null) {
                resulttext.AppendLine();
                formatters[iftoken.Else].FormatToken(iftoken.Else, resulttext, formatters, depth);
            }
        }
    }
}