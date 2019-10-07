using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formatter for a <see cref="ScriptVariable"/>
    /// </summary>
    public class VariableFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptVariable value = (ScriptVariable) token;
            resulttext.Append($"${value.Name}");
        }
    }
}