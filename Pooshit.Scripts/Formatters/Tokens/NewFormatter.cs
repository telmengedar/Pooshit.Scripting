using System.Linq;
using System.Text;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="NewInstance"/>s
    /// </summary>
    public class NewFormatter : TokenFormatter{

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            NewInstance instance = (NewInstance) token;

            resulttext.Append("new ").Append(instance.TypeName).Append('(');
            if (instance.Parameters.Any()) {
                foreach (IScriptToken parameter in instance.Parameters) {
                    formatters[parameter].FormatToken(parameter, resulttext, formatters, depth);
                    resulttext.Append(", ");
                }
                resulttext.Length -= 2;
            }
            resulttext.Append(')');
        }
    }
}