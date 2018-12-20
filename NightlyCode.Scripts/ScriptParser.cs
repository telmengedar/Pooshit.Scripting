using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser {
        readonly IScriptHostPool hostpool;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="hostpool">pool containing hosts for members</param>
        public ScriptParser(IScriptHostPool hostpool) {
            this.hostpool = hostpool;
        }

        IScriptToken GetHostOrString(string name) {
            if (hostpool.ContainsHost(name))
                return new ScriptHost(hostpool, name);
            return new ScriptValue(name);
        }

        IScriptToken ParseToken(string data, ref int index, IScriptVariableHost variablehost) {
            StringBuilder tokenname = new StringBuilder();

            for(; index < data.Length; ++index) {
                char character = data[index];
                if(tokenname.Length == 0 && (char.IsDigit(character) || character == '.' || character == '-'))
                    return ParseNumber(data, ref index);

                switch (character) {
                    case '.':
                    case ',':
                    case ')':
                    case ']':
                    case '=':
                    case '[':
                        if(tokenname.Length > 0)
                            return GetHostOrString(tokenname.ToString().TrimEnd(' '));
                        break;
                    case '"':
                        ++index;
                        return ParseLiteral(data, ref index);
                    case ' ':
                        if(tokenname.Length == 0)
                            continue;
                        tokenname.Append(character);
                        break;
                    case '\\':
                        ++index;
                        tokenname.Append(ParseSpecialCharacter(data[index]));
                        break;
                    default:
                        tokenname.Append(character);
                        break;
                }
            }

            if(tokenname.Length > 0)
                return GetHostOrString(tokenname.ToString().TrimEnd(' '));
            return new ScriptValue(null);
        }

        string ParseName(string data, ref int index)
        {
            StringBuilder tokenname = new StringBuilder();

            for (; index < data.Length; ++index)
            {
                char character = data[index];
                if (char.IsLetterOrDigit(character) || character == '_')
                    tokenname.Append(character);
                else
                    return tokenname.ToString();
            }

            if (tokenname.Length > 0)
                return tokenname.ToString();
            return null;
        }

        IScriptToken ParseNumber(string data, ref int index)
        {
            StringBuilder literal = new StringBuilder();
            bool done = false;
            for (; index < data.Length; ++index)
            {
                char character = data[index];
                switch (character)
                {
                    case ',':
                    case ')':
                    case ']':
                        done = true;
                        break;
                    default:
                        literal.Append(character);
                        break;
                }

                if(done)
                    break;
            }

            // this can't be a number
            if (literal.Length == 0)
                return new ScriptValue("");

            int dotcount = 0;
            for (int i = 0; i < literal.Length; ++i) {
                if(!char.IsDigit(literal[i]) && literal[i] != '.' || i > 0 && literal[i] == '-')
                    return new ScriptValue(literal.ToString());

                if (literal[i] == '.')
                    ++dotcount;
            }

            switch (dotcount)
            {
                case 0:
                    return new ScriptValue(long.Parse(literal.ToString()));
                case 1:
                    return new ScriptValue(double.Parse(literal.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture));
                default:
                    return new ScriptValue(literal.ToString());
            }
        }

        char ParseSpecialCharacter(char character) {
            switch(character) {
                case 't':
                    return '\t';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                default:
                    return character;
            }
        }

        IScriptToken ParseLiteral(string data, ref int index) {
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                switch(character) {
                    case '"':
                        ++index;
                        return new ScriptValue(literal.ToString());
                    case '\\':
                        ++index;
                        literal.Append(ParseSpecialCharacter(data[index]));
                        break;
                    default:
                        literal.Append(character);
                        break;
                }
            }

            throw new ScriptException("Literal not terminated");
        }

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, IScriptVariableHost variablehost) {
            StringBuilder membername = new StringBuilder();
            for (; index < data.Length; ++index)
            {
                char character = data[index];
                switch (character)
                {
                    case '(':
                        ++index;
                        return new ScriptMethod(hostpool, host, membername.ToString(), ParseParameters(data, ref index, variablehost));
                    case '=':
                    case ',':
                    case '.':
                    case ')':
                        if (membername.Length == 0)
                            throw new ScriptException("Member name expected");
                        return new ScriptMember(host, membername.ToString());
                    default:
                        membername.Append(character);
                        break;
                }
            }

            if(membername.Length > 0)
                return new ScriptMember(host, membername.ToString());
            throw new ScriptException("Member name expected");
        }

        IScriptToken[] ParseArray(string data, ref int index, IScriptVariableHost variablehost) {
            List<IScriptToken> array = new List<IScriptToken>();
            for (; index < data.Length;)
            {
                char character = data[index];
                switch (character)
                {
                case '[':
                    ++index;
                    array.Add(new ScriptArray(ParseArray(data, ref index, variablehost)));
                    break;
                case ']':
                    ++index;
                    return array.ToArray();
                case ',':
                    ++index;
                    break;
                default:
                    array.Add(Parse(data, ref index, variablehost));
                    break;
                }
            }

            throw new ScriptException("Array not terminated");
        }

        IScriptToken[] ParseParameters(string data, ref int index, IScriptVariableHost variablehost) {
            List<IScriptToken> parameters = new List<IScriptToken>();
            for(; index < data.Length;) {
                char character = data[index];
                switch(character) {
                    case '[':
                        ++index;
                        parameters.Add(new ScriptArray(ParseArray(data, ref index, variablehost)));
                        break;
                    case ')':
                    case ']':
                        ++index;
                        return parameters.ToArray();
                    case ',':
                        ++index;
                        break;
                    default:
                        parameters.Add(Parse(data, ref index, variablehost));
                        break;
                }
            }

            throw new ScriptException("Parameter list not terminated");
        }

        IScriptToken Parse(string data, ref int index, IScriptVariableHost variablehost) {
            IScriptToken lasttoken = null;
            while (index < data.Length) {
                switch (data[index]) {
                    case '=':
                        ++index;
                        lasttoken=new ScriptAssignment(lasttoken, Parse(data, ref index, variablehost));
                        break;
                    case '.':
                        ++index;
                        lasttoken = ParseMember(lasttoken, data, ref index, variablehost);
                        break;
                    case '$':
                        ++index;
                        lasttoken=new ScriptVariable(variablehost, ParseName(data, ref index));
                        break;
                    case '[':
                        ++index;
                        if (lasttoken == null)
                            lasttoken = new ScriptArray(ParseArray(data, ref index, variablehost));
                        else lasttoken = new ScriptIndexer(lasttoken, ParseParameters(data, ref index, variablehost));
                        break;
                    case ')':
                    case ',':
                    case ']':
                        return lasttoken;
                    default:
                        lasttoken = ParseToken(data, ref index, variablehost);
                        break;
                }
            }

            return lasttoken;
        }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <param name="variablehost">host for variables</param>
        /// <returns>script which can get executed</returns>
        public IScriptToken Parse(string data, IScriptVariableHost variablehost=null) {
            int index = 0;
            return Parse(data, ref index, variablehost);
        }
    }
}