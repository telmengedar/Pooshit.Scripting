using System.Linq;
using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ScriptMethod"/>
    /// </summary>
    public class MethodFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptMethod method = (ScriptMethod) token;
            formatters[method.Host].FormatToken(method.Host, resulttext, formatters, depth);
            if(method.MethodName!="invoke")
                resulttext.Append('.').Append(method.MethodName);

            resulttext.Append('(');
            if (method.Parameters.Any()) {
                foreach (IScriptToken parameter in method.Parameters) {
                    formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                    resulttext.Append(", ");
                }
                resulttext.Length -= 2;
            }

            resulttext.Append(')');
        }
    }
}