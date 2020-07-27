using System.Globalization;
using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Formatters.Tokens {

    /// <summary>
    /// formats <see cref="ScriptValue"/>s
    /// </summary>
    public class ValueFormatter : TokenFormatter {

        /// <inheritdoc />
        protected override void Format(IScriptToken token, StringBuilder resulttext, IFormatterCollection formatters, int depth = 0) {
            ScriptValue value = (ScriptValue)token;
            if(value.Value is string stringvalue) {
                resulttext.Append('"');
                foreach(char character in stringvalue) {
                    switch(character) {
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
                resulttext.Append('"');
            }
            else if(value.Value is bool boolean) {
                resulttext.Append(boolean ? "true" : "false");
            }
            else if(value.Value is decimal decimalvalue) {
                resulttext.Append(decimalvalue.ToString(CultureInfo.InvariantCulture)).Append('d');
            }
            else if(value.Value is double doublevalue) {
                resulttext.Append(doublevalue.ToString("0.0##############", CultureInfo.InvariantCulture));
            }
            else if(value.Value is float floatvalue) {
                resulttext.Append(floatvalue.ToString(CultureInfo.InvariantCulture)).Append('f');
            }
            else if(value.Value is byte bytevalue) {
                resulttext.Append(bytevalue.ToString(CultureInfo.InvariantCulture)).Append('b');
            }
            else if(value.Value is sbyte sbytevalue) {
                resulttext.Append(sbytevalue.ToString(CultureInfo.InvariantCulture)).Append("sb");
            }
            else if(value.Value is short shortvalue) {
                resulttext.Append(shortvalue.ToString(CultureInfo.InvariantCulture)).Append('s');
            }
            else if(value.Value is ushort ushortvalue) {
                resulttext.Append(ushortvalue.ToString(CultureInfo.InvariantCulture)).Append("us");
            }
            else if(value.Value is int intvalue) {
                resulttext.Append(intvalue.ToString(CultureInfo.InvariantCulture));
            }
            else if(value.Value is uint uintvalue) {
                resulttext.Append(uintvalue.ToString(CultureInfo.InvariantCulture)).Append("u");
            }
            else if(value.Value is long longvalue) {
                resulttext.Append(longvalue.ToString(CultureInfo.InvariantCulture)).Append('l');
            }
            else if(value.Value is ulong ulongvalue) {
                resulttext.Append(ulongvalue.ToString(CultureInfo.InvariantCulture)).Append("ul");
            }
            else if(value.Value is null)
                resulttext.Append("null");
        }
    }
}