using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="If"/>
    /// </summary>
    public class IfFormatter : TokenFormatter {
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            If iftoken = (If) token;
            resulttext.Append("if(");
            foreach (IScriptToken parameter in iftoken.Parameters)
                formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
            resulttext.Append(")");

            FormatBody(iftoken.Body, resulttext, formatters, depth);
            if (iftoken.Else != null) {
                resulttext.AppendLine();
                AppendIntendation(resulttext, depth);
                resulttext.Append("else");
                FormatBody(iftoken.Else, resulttext, formatters, depth);
            }
        }
    }
}