using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Operations.Logic;
using NightlyCode.Scripting.Operations.Values;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser {

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        public ScriptParser()
        : this(new ScriptHosts())
        {
        }

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="hostpool">pool containing hosts for members</param>
        public ScriptParser(IScriptHosts hostpool) {
            Hosts = hostpool;
        }

        public IScriptHosts Hosts { get; }

        void SkipWhitespaces(string data, ref int index) {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;
        }

        IScriptToken[] ParseControlParameters(string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (data[index] != '(')
                throw new ScriptException($"Expected parameters for control statement");
            ++index;
            return ParseParameters(data, ref index, variables);
        }

        IScriptToken AnalyseToken(string token, string data, ref int index, IVariableContext variablehost, bool first) {
            if (first) {
                switch (token) {
                case "if":
                    return new If(ParseControlParameters(data, ref index, variablehost));
                case "else":
                    return new ParserToken("else");
                case "for":
                    return new For(ParseControlParameters(data, ref index, variablehost));
                case "foreach":
                    return new Foreach(ParseControlParameters(data, ref index, variablehost));
                case "while":
                    return new While(ParseControlParameters(data, ref index, variablehost));
                case "switch":
                    return new Switch(ParseControlParameters(data, ref index, variablehost));
                case "case":
                    return new Case(ParseControlParameters(data, ref index, variablehost));
                case "default":
                    return new Case();
                }
            }

            switch (token) {
                case "true":
                    return new ScriptValue(true);
                case "false":
                    return new ScriptValue(false);
                case "null":
                    return new ScriptValue(null);
                default:
                    if (Hosts.ContainsVariable(token))
                        return new ScriptHost(Hosts, token);
                    return new ScriptValue(token);
            }
        }

        IScriptToken ParseToken(string data, ref int index, IVariableContext variables, bool startofstatement) {
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
                    case '(':
                    case ' ':
                        if(tokenname.Length > 0)
                            return AnalyseToken(tokenname.ToString().TrimEnd(' '), data, ref index, variables, startofstatement);
                        break;
                    case '"':
                        ++index;
                        return ParseLiteral(data, ref index);
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
                return AnalyseToken(tokenname.ToString().TrimEnd(' '), data, ref index, variables, startofstatement);
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
            for (; index < data.Length; ++index) {
                if (char.IsDigit(data[index]) || data[index] == '.')
                    literal.Append(data[index]);
                else break;
            }

            // this can't be a number
            if (literal.Length == 0)
                return new ScriptValue(null);

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

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, IVariableContext variablehost) {
            StringBuilder membername = new StringBuilder();
            for (; index < data.Length; ++index)
            {
                char character = data[index];
                if (char.IsLetterOrDigit(character) || character == '_') {
                    membername.Append(character);
                    continue;
                }

                break;
            }

            if (index < data.Length) {
                switch (data[index]) {
                case '(':
                    ++index;
                    return new ScriptMethod(Hosts, host, membername.ToString(), ParseParameters(data, ref index, variablehost));
                }
            }

            if (membername.Length > 0)
                return new ScriptMember(host, membername.ToString());
            throw new ScriptException("Member name expected");
        }

        IScriptToken[] ParseArray(string data, ref int index, IVariableContext variablehost) {
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

        IScriptToken[] ParseParameters(string data, ref int index, IVariableContext variablehost) {
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


        IOperator ParseOperator(string data, ref int index) {
            StringBuilder token = new StringBuilder();

            bool done = false;
            while (index < data.Length && !done) {
                switch (data[index]) {
                case '=':
                case '!':
                case '~':
                case '<':
                case '>':
                case '/':
                case '+':
                case '*':
                case '-':
                case '%':
                case '&':
                case '|':
                case '^':
                    token.Append(data[index]);
                    ++index;
                    break;
                default:
                    done = true;
                    break;
                }
            }

            Operator @operator = token.ToString().ParseOperator();
            switch (@operator) {
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.Less:
                case Operator.LessOrEqual:
                case Operator.Greater:
                case Operator.GreaterOrEqual:
                case Operator.Matches:
                case Operator.NotMatches:
                    return new ScriptComparision(@operator);
                case Operator.Addition:
                    return new Addition();
                case Operator.Subtraction:
                    return new Subtraction();
                case Operator.Division:
                    return new Division();
                case Operator.Multiplication:
                    return new Multiplication();
                case Operator.Modulo:
                    return new Modulo();
                case Operator.LogicAnd:
                case Operator.LogicOr:
                case Operator.LogicXor:
                    return new LogicComparision(@operator);
                case Operator.BitwiseAnd:
                    return new BitwiseAnd();
                case Operator.BitwiseOr:
                    return new BitwiseOr();
                case Operator.BitwiseXor:
                    return new BitwiseXor();
                case Operator.Assignment:
                    return new ScriptAssignment();
                case Operator.ShiftLeft:
                    return new ShiftLeft();
                case Operator.ShiftRight:
                    return new ShiftRight();
                case Operator.Not:
                    return new Not();
                case Operator.Negate:
                    return new Negate();
                default:
                    throw new ScriptException($"Unsupported operator {token}");
            }
        }

        IScriptToken ParseBlock(string data, ref int index, IVariableContext variables) {
            IScriptToken block = Parse(data, ref index, variables);
            while (index < data.Length) {
                if (char.IsWhiteSpace(data[index]))
                {
                    ++index;
                    continue;
                }

                break;
            }

            if (data[index] != ')')
                throw new ScriptException("Block not terminated");

            ++index;
            return new Block(block);
        }

        IScriptToken Parse(string data, ref int index, IVariableContext variablehost, bool startofstatement=false) {
            List<IScriptToken> tokenlist=new List<IScriptToken>();
            List<OperatorIndex> indexlist=new List<OperatorIndex>();

            bool done = false;
            while (index < data.Length && !done) {
                SkipWhitespaces(data, ref index);

                switch (data[index]) {
                    case '=':
                    case '!':
                    case '~':
                    case '<':
                    case '>':
                    case '/':
                    case '+':
                    case '*':
                    case '-':
                    case '%':
                    case '&':
                    case '|':
                    case '^':
                        IOperator @operator = ParseOperator(data, ref index);
                        indexlist.Add(new OperatorIndex(tokenlist.Count, @operator));
                        tokenlist.Add(@operator);
                        break;
                    case '.':
                        ++index;
                        tokenlist[tokenlist.Count-1]=ParseMember(tokenlist[tokenlist.Count - 1], data, ref index, variablehost);
                        break;
                    case '$':
                        if (variablehost == null)
                            throw new ScriptException("No variablehost specified");
                        ++index;
                        tokenlist.Add(new ScriptVariable(variablehost, ParseName(data, ref index)));
                        break;
                    case '(':
                        ++index;
                        tokenlist.Add(ParseBlock(data, ref index, variablehost));
                        break;
                    case '[':
                        ++index;
                        if (tokenlist.Count==0||tokenlist[tokenlist.Count-1] is IOperator)
                            tokenlist.Add(new ScriptArray(ParseArray(data, ref index, variablehost)));
                        else tokenlist[tokenlist.Count-1] = new ScriptIndexer(tokenlist[tokenlist.Count - 1], ParseParameters(data, ref index, variablehost));
                        break;
                    case ')':
                    case ',':
                    case ']':
                        done = true;
                        break;
                    case ';':
                        ++index;
                        done = true;
                        break;
                    default:
                        IScriptToken token = ParseToken(data, ref index, variablehost, startofstatement);
                        if (token is IControlToken || token is ParserToken || token is Switch)
                            return token;
                        tokenlist.Add(token);
                        break;
                }
            }

            indexlist.Sort((lhs, rhs) => lhs.Token.Operator.CompareTo(rhs.Token.Operator));

            for (int i = 0; i < indexlist.Count; ++i) {
                OperatorIndex operatorindex = indexlist[i];
                if (operatorindex.Token is IUnaryToken unary) {
                    unary.Operand = tokenlist[operatorindex.Index + 1];
                    tokenlist.RemoveAt(operatorindex.Index + 1);
                    --operatorindex.Index;
                    for (int k = i; k < indexlist.Count; ++k)
                        if (indexlist[k].Index > operatorindex.Index)
                            --indexlist[k].Index;
                }
                else if (operatorindex.Token is IBinaryToken binary) {
                    binary.Lhs = tokenlist[operatorindex.Index - 1];
                    binary.Rhs = tokenlist[operatorindex.Index + 1];
                    tokenlist.RemoveAt(operatorindex.Index + 1);
                    tokenlist.RemoveAt(operatorindex.Index - 1);
                    --operatorindex.Index;
                    for (int k = i; k < indexlist.Count; ++k)
                        if(indexlist[k].Index>operatorindex.Index)
                            indexlist[k].Index = indexlist[k].Index - 2;
                }
            }

            return tokenlist.Single();
        }

        public IScriptToken ParseStatementBlock(string data, ref int index, IVariableContext variablehost) {
            List<IScriptToken> statements = new List<IScriptToken>();
            while (index < data.Length)
            {
                IScriptToken token = Parse(data, ref index, variablehost, true);
                if (token != null)
                    statements.Add(token);
            }

            if (statements.Count == 0)
                return null;
            if (statements.Count == 1)
                return statements.Single();

            for (int i = 0; i < statements.Count;++i)
            {
                if (statements[i] is IControlToken control) {
                    if (i + 1 >= statements.Count)
                        throw new ScriptException("If statement without execution block detected");
                    control.Body = statements[i + 1];
                    statements.RemoveAt(i + 1);

                    if (control is Case @case) {
                        Switch @switch = statements[i - 1] as Switch;
                        if (@switch == null)
                            throw new ScriptException("Case without connected switch statement found");
                        @switch.AddCase(@case);
                        statements.RemoveAt(i);
                        --i;
                    }
                }
                else if (statements[i] is ParserToken parsertoken) {
                    switch (parsertoken.Data) {
                        case "else":
                            if(i==0)
                                throw new ScriptException("Else without if detected");
                            if (i + 1 >= statements.Count)
                                throw new ScriptException("Else without execution block detected");

                            if (statements[i - 1] is If @if) {
                                @if.Else = statements[i+1];
                                statements.RemoveAt(i+1);
                                statements.RemoveAt(i);
                                --i;
                            }
                            else throw new ScriptException("Else without if detected");
                            break;
                    }
                }
            }
            return new StatementBlock(statements.ToArray());
        }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <param name="variablehost">host for variables</param>
        /// <returns>script which can get executed</returns>
        public IScriptToken Parse(string data, IVariableContext variablehost=null) {
            int index = 0;
            return ParseStatementBlock(data, ref index, variablehost);
        }
    }
}