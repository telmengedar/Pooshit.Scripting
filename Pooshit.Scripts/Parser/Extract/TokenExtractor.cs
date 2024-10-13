using System;
using System.Text;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Extract {

    /// <inheritdoc />
    public class TokenExtractor : ITokenExtractor {
        readonly IScriptParser parser;

        /// <summary>
        /// creates a new <see cref="TokenExtractor"/>
        /// </summary>
        /// <param name="parser">parser to use to extract token</param>
        public TokenExtractor(IScriptParser parser) {
            this.parser = parser;
        }

        void SkipWeirdCharacters(StringBuilder result, string data, ref int index) {
            while (index < data.Length) {
                if(char.IsLetterOrDigit(data[index]) || data[index]=='_' || data[index]=='.')
                    return;

                switch (data[index]) {
                    case '(':
                    case '[':
                    case '"':
                    case '{':
                    case '$':
                    case '.':
                    case ')':
                    case ']':
                    case '}':
                        return;
                }

                result.Append(' ');
                ++index;
            }
        }

        void ScanString(StringBuilder result, string data, ref int index, bool isinterpolation=false) {
            bool escape = false;
            while (index < data.Length) {
                if (escape) {
                    result.Append(data[index]);
                    escape = false;
                    ++index;
                    continue;
                }

                switch (data[index]) {
                case '\\':
                    result.Append('\\');
                    escape = true;
                    break;
                case '"':
                    ++index;
                    result.Append('"');
                    return;
                default:
                    if (isinterpolation && data[index]=='{') {
                        ++index;
                        result.Append('{');
                        Scan(result, data, ref index, '}');
                        continue;
                    }
                    else {
                        result.Append(data[index]);
                    }
                    break;
                }

                ++index;
            }

            result.Append('"');
        }

        void ScanLiteral(StringBuilder builder, string data, ref int index) {
            while (index < data.Length && (char.IsLetterOrDigit(data[index]) || data[index] == '_' || data[index] == '.'))
                builder.Append(data[index++]);
            if (builder[builder.Length - 1] == '.')
                builder[builder.Length - 1] = ' ';
        }

        void ScanLiteral(string data, ref int index) {
            while (index < data.Length && (char.IsLetterOrDigit(data[index]) || data[index] == '_'))
                ++index;
        }

        void Scan(StringBuilder result, string data, ref int index, char start='\0') {
            SkipWeirdCharacters(result, data, ref index);
            while (index < data.Length) {
                if (start != '\0' && data[index] == start) {
                    ++index;
                    result.Append(start);
                    return;
                }

                if (char.IsLetterOrDigit(data[index]) || data[index]=='_' || data[index]=='.')
                    ScanLiteral(result, data, ref index);
                else {
                    switch (data[index]) {
                    case '$':
                        result.Append('$');
                        ++index;
                        if (index < data.Length && data[index] == '"') {
                            result.Append(data[index++]);
                            ScanString(result, data, ref index, true);
                        }
                        else ScanLiteral(result, data, ref index);
                        break;
                    case '"':
                        result.Append(data[index++]);
                        ScanString(result, data, ref index);
                        break;
                    case '(':
                        result.Append(data[index++]);
                        Scan(result, data, ref index, ')');
                        break;
                    case '[':
                        result.Append(data[index++]);
                        Scan(result, data, ref index, ']');
                        break;
                    case '{':
                        result.Append(data[index++]);
                        Scan(result, data, ref index, '}');
                        break;
                    default:
                        result.Append(' ');
                        ++index;
                        break;
                    }
                }
                SkipWeirdCharacters(result, data, ref index);
            }

            if (start != '\0')
                result.Append(start);
        }

        public IScriptToken ExtractToken(string data, int position, bool fulltoken=true, Func<IScriptToken, bool> filter=null) {
            position = Math.Min(Math.Max(position, 0), data.Length);
            int endposition = position;
            int startposition = position;
            if (startposition >= data.Length)
                --startposition;
            while (startposition > 0 && data[startposition] != '\n')
                --startposition;
            if (data[startposition] == '\n')
                ++startposition;

            if (startposition >= data.Length)
                return null;

            if(fulltoken)
                ScanLiteral(data, ref endposition);
            string line = data.Substring(startposition, endposition - startposition);

            StringBuilder finalline = new StringBuilder();
            int index = 0;
            Scan(finalline, line, ref index);
            
            while (finalline.Length > 0 && (finalline[finalline.Length - 1] == '.' || finalline[finalline.Length - 1] == ',' || finalline[finalline.Length - 1] == ' '))
                --finalline.Length;

            IScript script;
            try {
                script = parser.Parse(finalline.ToString());
            }
            catch (Exception) {
                return null;
            }

            TokenExtractionVisitor tokenvisitor = new TokenExtractionVisitor(position, filter);
            tokenvisitor.Visit(script);
            return tokenvisitor.Token;
        }
    }
}