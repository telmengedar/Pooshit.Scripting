using System.Text;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="StringInterpolation"/>s
    /// </summary>
    public class InterpolationFormatter : TokenFormatter {

        void FormatString(string value, StringBuilder resulttext) {
            foreach (char character in value) {
                switch (character) {
                case '"':
                    resulttext.Append("\\\"");
                    break;
                case '\t':
                    resulttext.Append("\\t");
                    break;
                case '\r':
                    resulttext.Append("\\r");
                    break;
                case '\n':
                    resulttext.Append("\\n");
                    break;
                case '\\':
                    resulttext.Append("\\\\");
                    break;
                default:
                    resulttext.Append(character);
                    break;
                }
            }
        }

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            StringInterpolation interpolation = (StringInterpolation) token;
            resulttext.Append("$\"");

            foreach (IScriptToken child in interpolation.Children) {
                if (child is ScriptValue value && value.Value is string stringvalue)
                    FormatString(stringvalue, resulttext);
                else {
                    resulttext.Append('{');
                    if (child is StatementBlock block) {
                        foreach (IScriptToken blockchild in block.Children) {
                            formatters[blockchild].FormatToken(blockchild, resulttext, formatters, depth >= 0 ? depth : 0);
                            resulttext.Append(' ');
                        }

                        --resulttext.Length;
                    }
                    else formatters[child].FormatToken(child, resulttext, formatters, depth);
                    resulttext.Append('}');
                }
            }

            resulttext.Append('"');
        }
    }
}