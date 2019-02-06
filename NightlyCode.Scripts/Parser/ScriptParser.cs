﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Operations.Assign;
using NightlyCode.Scripting.Operations.Comparision;
using NightlyCode.Scripting.Operations.Logic;
using NightlyCode.Scripting.Operations.Unary;
using NightlyCode.Scripting.Operations.Values;
using NightlyCode.Scripting.Parser.Operators;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser : IScriptParser {
        readonly OperatorTree operatortree = new OperatorTree();
        readonly IVariableProvider globalvariables;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="globalvariables">provider for global variables</param>
        public ScriptParser(IVariableProvider globalvariables) {
            InitializeOperators();
            this.globalvariables = globalvariables;
            Types.AddType<List<object>>("list");
        }

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="variables">global variables of script parser</param>
        public ScriptParser(params Variable[] variables)
            : this(new VariableProvider(null, variables)) {
        }

        /// <summary>
        /// access to extensions available to script environment
        /// </summary>
        public IExtensionProvider Extensions { get; } = new ExtensionProvider();

        /// <summary>
        /// access to types which can be created using 'new' keyword
        /// </summary>
        public ITypeProvider Types { get; } = new TypeProvider();

        void InitializeOperators() {
            operatortree.Add("~", Operator.Complement);
            operatortree.Add("!", Operator.Not);
            operatortree.Add("=", Operator.Assignment);
            operatortree.Add("==", Operator.Equal);
            operatortree.Add("!=", Operator.NotEqual);
            operatortree.Add("<>", Operator.NotEqual);
            operatortree.Add("<", Operator.Less);
            operatortree.Add("<=", Operator.LessOrEqual);
            operatortree.Add(">", Operator.Greater);
            operatortree.Add(">=", Operator.GreaterOrEqual);
            operatortree.Add("~~", Operator.Matches);
            operatortree.Add("!~", Operator.NotMatches);
            operatortree.Add("+", Operator.Addition);
            operatortree.Add("-", Operator.Subtraction);
            operatortree.Add("*", Operator.Multiplication);
            operatortree.Add("/", Operator.Division);
            operatortree.Add("%", Operator.Modulo);
            operatortree.Add("&", Operator.BitwiseAnd);
            operatortree.Add("|", Operator.BitwiseOr);
            operatortree.Add("^", Operator.BitwiseXor);
            operatortree.Add("<<", Operator.ShiftLeft);
            operatortree.Add(">>", Operator.ShiftRight);
            operatortree.Add("<<<", Operator.RolLeft);
            operatortree.Add(">>>", Operator.RolRight);
            operatortree.Add("&&", Operator.LogicAnd);
            operatortree.Add("||", Operator.LogicOr);
            operatortree.Add("^^", Operator.LogicXor);
            operatortree.Add("+=", Operator.AddAssign);
            operatortree.Add("-=", Operator.SubAssign);
            operatortree.Add("*=", Operator.MulAssign);
            operatortree.Add("/=", Operator.DivAssign);
            operatortree.Add("%=", Operator.ModAssign);
            operatortree.Add("<<=", Operator.ShlAssign);
            operatortree.Add(">>=", Operator.ShrAssign);
            operatortree.Add("&=", Operator.AndAssign);
            operatortree.Add("|=", Operator.OrAssign);
            operatortree.Add("^=", Operator.XorAssign);
            operatortree.Add("++", Operator.Increment);
            operatortree.Add("--", Operator.Decrement);
            operatortree.Add("//", Operator.SingleLineComment);
            operatortree.Add("/*", Operator.MultilineComment);
        }

        void SkipWhitespaces(string data, ref int index) {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;
        }

        IScriptToken[] TryParseControlParameters(string data, ref int index, IVariableProvider variables) {
            SkipWhitespaces(data, ref index);
            if (index >= data.Length || data[index] != '(')
                return new IScriptToken[0];
            return ParseControlParameters(data, ref index, variables);
        }

        IScriptToken[] ParseControlParameters(string data, ref int index, IVariableProvider variables) {
            SkipWhitespaces(data, ref index);
            if (data[index] != '(')
                throw new ScriptParserException("Expected parameters for control statement");
            ++index;
            return ParseParameters(data, ref index, variables);
        }

        IScriptToken AnalyseToken(string token, string data, int start, ref int index, IVariableProvider variables, bool first) {
            if (token.Length == 0)
                throw new ScriptParserException("Unexpected structure leading to empty token");

            if (char.IsDigit(token[0])) {
                try {
                    IScriptToken number = ParseNumber(token);
                    if (number is ScriptValue value && value.Value is string)
                        TokenParsed?.Invoke(TokenType.Literal, start, index);
                    else TokenParsed?.Invoke(TokenType.Number, start, index);
                    return number;
                }
                catch (Exception) {
                    TokenParsed?.Invoke(TokenType.Literal, start, index);
                    return new ScriptValue(token);
                }
            }

            if (first) {
                switch (token) {
                case "if":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new If(ParseControlParameters(data, ref index, variables));
                case "else":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Else();
                case "for":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new For(ParseControlParameters(data, ref index, variables));
                case "foreach":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Foreach(ParseControlParameters(data, ref index, variables));
                case "while":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new While(ParseControlParameters(data, ref index, variables));
                case "switch":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Switch(ParseControlParameters(data, ref index, variables));
                case "case":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Case(ParseControlParameters(data, ref index, variables));
                case "default":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Case();
                case "return":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Return(Parse(data, ref index, variables));
                case "throw":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Throw(ParseControlParameters(data, ref index, variables));
                case "break":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Break(TryParseControlParameters(data, ref index, variables));
                case "continue":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Continue(TryParseControlParameters(data, ref index, variables));
                case "using":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Using(ParseControlParameters(data, ref index, variables));
                }
            }

            switch (token) {
                case "true":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new ScriptValue(true);
                case "false":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new ScriptValue(false);
                case "null":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new ScriptValue(null);
                case "new":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    start = index;
                    string type = ParseName(data, ref index);
                    TokenParsed?.Invoke(TokenType.Type, start, index);
                    ITypeInstanceProvider typeprovider = Types.GetType(type.ToLower());
                    return new NewInstance(typeprovider, ParseControlParameters(data, ref index, variables));
                default:
                    IVariableProvider provider = variables.GetProvider(token);
                    if (provider == null) {
                        TokenParsed?.Invoke(TokenType.Literal, start, index);
                        return new ScriptValue(token);
                    }

                    TokenParsed?.Invoke(TokenType.Variable, start, index);
                    return new ScriptVariable(provider, token);
            }
        }

        IScriptToken ParseToken(string data, ref int index, IVariableProvider variables, bool startofstatement) {
            SkipWhitespaces(data, ref index);
            int start = index;
            bool parsenumber = false;
            if (index < data.Length) {
                char character = data[index];
                if (char.IsDigit(character) || character == '.' || character == '-')
                    parsenumber = true;

                if (character == '"')
                {
                    ++index;
                    return ParseLiteral(data, ref index);
                }

                if (character == '\'') {
                    ++index;
                    return ParseCharacter(data, ref index);
                }
            }

            StringBuilder tokenname = new StringBuilder();
            for (; index < data.Length; ++index) {
                char character = data[index];

                if (char.IsLetterOrDigit(character) || character == '_' || (parsenumber && character == '.'))
                    tokenname.Append(character);
                else if (character == '"' || character == '\\') {
                    ++index;
                    tokenname.Append(ParseSpecialCharacter(data[index]));
                }
                else return AnalyseToken(tokenname.ToString(), data, start, ref index, variables, startofstatement);
            }

            if(tokenname.Length > 0)
                return AnalyseToken(tokenname.ToString(), data, start, ref index, variables, startofstatement);
            return new ScriptValue(null);
        }

        string ParseName(string data, ref int index)
        {
            SkipWhitespaces(data, ref index);
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

        IScriptToken ParseNumber(string data) {
            string numberdata = data.ToLower();
            if (numberdata.StartsWith("0x")) {
                if (numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(0, numberdata.Length - 2), 16));
                if (numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(0, numberdata.Length - 1), 16));
                if (numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(0, numberdata.Length - 2), 16));
                if (numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(0, numberdata.Length - 1), 16));
                if (numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(0, numberdata.Length - 2), 16));
                if (numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(0, numberdata.Length - 1), 16));
                if (numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(0, numberdata.Length - 1), 16));
                return new ScriptValue(Convert.ToInt32(numberdata, 16));
            }

            if (numberdata.StartsWith("0o"))
            {
                if (numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(2, numberdata.Length-4), 8));
                if (numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(2, numberdata.Length - 3), 8));
                if (numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(2, numberdata.Length - 4), 8));
                if (numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(2, numberdata.Length - 3), 8));
                if (numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(2, numberdata.Length - 4), 8));
                if (numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(2, numberdata.Length - 3), 8));
                if (numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(2, numberdata.Length - 3), 8));
                return new ScriptValue(Convert.ToInt32(numberdata.Substring(2), 8));
            }

            if (numberdata.StartsWith("0b"))
            {
                if (numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(2, numberdata.Length - 4), 2));
                if (numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(2, numberdata.Length - 3), 2));
                if (numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(2, numberdata.Length - 4), 2));
                if (numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(2, numberdata.Length - 3), 2));
                if (numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(2, numberdata.Length - 4), 2));
                if (numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(2, numberdata.Length - 3), 2));
                if (numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(2, numberdata.Length - 3), 2));
                return new ScriptValue(Convert.ToInt32(numberdata.Substring(2), 2));
            }

            if (numberdata.EndsWith("ul"))
                return new ScriptValue(Convert.ToUInt64(numberdata.Substring(0, numberdata.Length - 2), 10));
            if (numberdata.EndsWith("l"))
                return new ScriptValue(Convert.ToInt64(numberdata.Substring(0, numberdata.Length - 1), 10));
            if (numberdata.EndsWith("us"))
                return new ScriptValue(Convert.ToUInt16(numberdata.Substring(0, numberdata.Length - 2), 10));
            if (numberdata.EndsWith("s"))
                return new ScriptValue(Convert.ToInt16(numberdata.Substring(0, numberdata.Length - 1), 10));
            if (numberdata.EndsWith("sb"))
                return new ScriptValue(Convert.ToSByte(numberdata.Substring(0, numberdata.Length - 2), 10));
            if (numberdata.EndsWith("b"))
                return new ScriptValue(Convert.ToByte(numberdata.Substring(0, numberdata.Length - 1), 10));
            if (numberdata.EndsWith("u"))
                return new ScriptValue(Convert.ToUInt32(numberdata.Substring(0, numberdata.Length - 1), 10));

            int dotcount = 0;
            for (int i = 0; i < numberdata.Length; ++i) {
                if (numberdata[i] == '.')
                    ++dotcount;
            }

            switch (dotcount)
            {
                case 0:
                    return new ScriptValue(int.Parse(numberdata));
                case 1:
                    if (numberdata.EndsWith("d"))
                        return new ScriptValue(decimal.Parse(numberdata.Substring(0, numberdata.Length-1), NumberStyles.Float, CultureInfo.InvariantCulture));
                    if (numberdata.EndsWith("f"))
                        return new ScriptValue(float.Parse(numberdata.Substring(0, numberdata.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture));
                    return new ScriptValue(double.Parse(numberdata, NumberStyles.Float, CultureInfo.InvariantCulture));
                default:
                    return new ScriptValue(numberdata);
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

        IScriptToken ParseCharacter(string data, ref int index) {
            int start = index - 1;
            char character = data[index];
            if (character == '\\') {
                ++index;
                character = ParseSpecialCharacter(data[index]);
            }

            ++index;
            if (data[index] != '\'')
                throw new ScriptParserException("Character literal not terminated");
            ++index;
            TokenParsed?.Invoke(TokenType.Literal, start, index);
            return new ScriptValue(character);
        }

        IScriptToken ParseLiteral(string data, ref int index) {
            int start = index - 1;
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                switch(character) {
                    case '"':
                        ++index;
                        TokenParsed?.Invoke(TokenType.Literal, start, index);
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

            throw new ScriptParserException("Literal not terminated");
        }

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, IVariableProvider variables) {
            int start = index;
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
                    TokenParsed?.Invoke(TokenType.Method, start, index);
                    return new ScriptMethod(Extensions, host, membername.ToString(), ParseParameters(data, ref index, variables));
                }
            }

            if (membername.Length > 0) {
                TokenParsed?.Invoke(TokenType.Property, start, index);
                return new ScriptMember(host, membername.ToString());
            }

            throw new ScriptParserException("Member name expected");
        }

        IScriptToken[] ParseArray(string data, ref int index, IVariableProvider variables) {
            SkipWhitespaces(data, ref index);
            List<IScriptToken> array = new List<IScriptToken>();
            for (; index < data.Length;)
            {
                char character = data[index];
                switch (character)
                {
                case '[':
                    ++index;
                    array.Add(new ScriptArray(ParseArray(data, ref index, variables)));
                    break;
                case ']':
                    ++index;
                    return array.ToArray();
                case ',':
                    ++index;
                    break;
                default:
                    IScriptToken element = Parse(data, ref index, variables);
                    if (element == null)
                        throw new ScriptParserException("Invalid array specification");
                    array.Add(element);
                    break;
                }
            }

            throw new ScriptParserException("Array not terminated");
        }

        IScriptToken[] ParseParameters(string data, ref int index, IVariableProvider variables) {
            List<IScriptToken> parameters = new List<IScriptToken>();
            for(; index < data.Length;) {
                char character = data[index];
                switch(character) {
                    case '[':
                        ++index;
                        parameters.Add(new ScriptArray(ParseArray(data, ref index, variables)));
                        break;
                    case ')':
                    case ']':
                        ++index;
                        return parameters.ToArray();
                    case ',':
                        ++index;
                        break;
                    default:
                        parameters.Add(Parse(data, ref index, variables));
                        break;
                }
            }

            throw new ScriptParserException("Parameter list not terminated");
        }


        IOperator ParseOperator(int parsestart,string data, ref int index) {
            int startoperator = index;

            OperatorNode node = operatortree.Root;
            bool done = false;
            while (index < data.Length) {
                char character = data[index];
                switch (character) {
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
                    ++index;
                    break;
                default:
                    done = true;
                    break;
                }

                if (done)
                    break;

                OperatorNode current = node[character];
                if (current == null)
                    break;

                node = current;
                if (!node.HasChildren)
                    break;
            }

            if (node == null)
                throw new ScriptParserException("Operator expected but nothing found");

            switch (node.Operator) {
                case Operator.SingleLineComment:
                    ParseSingleLineComment(data, ref index);
                    TokenParsed?.Invoke(TokenType.Comment, startoperator, index);
                    return null;
                case Operator.MultilineComment:
                    ParseMultiLineComment(data, ref index);
                    TokenParsed?.Invoke(TokenType.Comment, startoperator, index);
                    return null;
            }


            TokenParsed?.Invoke(TokenType.Operator, startoperator, index);
            switch (node.Operator) {
                case Operator.Increment:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Increment(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Increment(false);
                    else
                        throw new ScriptParserException("Increment without connected operand detected");
                case Operator.Decrement:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Decrement(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Decrement(false);
                    else
                        throw new ScriptParserException("Increment without connected operand detected");
                case Operator.Equal:
                    return new Equal();
                case Operator.NotEqual:
                    return new NotEqual();
                case Operator.Less:
                    return new Less();
                case Operator.LessOrEqual:
                    return new LessOrEqual();
                case Operator.Greater:
                    return new Greater();
                case Operator.GreaterOrEqual:
                    return new GreaterOrEqual();
                case Operator.Matches:
                    return new Matches();
                case Operator.NotMatches:
                    return new MatchesNot();
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
                    return new LogicAnd();
                case Operator.LogicOr:
                    return new LogicOr();
                case Operator.LogicXor:
                    return new LogicXor();
                case Operator.BitwiseAnd:
                    return new BitwiseAnd();
                case Operator.BitwiseOr:
                    return new BitwiseOr();
                case Operator.BitwiseXor:
                    return new BitwiseXor();
                case Operator.ShiftLeft:
                    return new ShiftLeft();
                case Operator.ShiftRight:
                    return new ShiftRight();
                case Operator.RolLeft:
                    return new RolLeft();
                case Operator.RolRight:
                    return new RolRight();
                case Operator.Not:
                    return new Not();
                case Operator.Complement:
                    return new Complement();
                case Operator.Assignment:
                    return new Assignment();
                case Operator.AddAssign:
                    return new AddAssign();
                case Operator.SubAssign:
                    return new SubAssign();
                case Operator.MulAssign:
                    return new MulAssign();
                case Operator.DivAssign:
                    return new DivAssign();
                case Operator.ModAssign:
                    return new ModAssign();
                case Operator.ShlAssign:
                    return new ShlAssign();
                case Operator.ShrAssign:
                    return new ShrAssign();
                case Operator.AndAssign:
                    return new AndAssign();
                case Operator.OrAssign:
                    return new OrAssign();
                case Operator.XorAssign:
                    return new XorAssign();
                default:
                    throw new ScriptParserException($"Unsupported operator '{node.Operator}'");
            }
        }

        void ParseMultiLineComment(string data, ref int index) {
            while (index < data.Length - 1 && data[index] != '*' && data[index + 1] != '/')
                ++index;
            index += 2;
        }

        void ParseSingleLineComment(string data, ref int index) {
            while (index < data.Length && data[index] != '\n')
                ++index;
            ++index;
        }

        IScriptToken ParseBlock(string data, ref int index, IVariableProvider variables) {
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
                throw new ScriptParserException("Block not terminated");

            ++index;
            return new ArithmeticBlock(block);
        }

        IScriptToken Parse(string data, ref int index, IVariableProvider variables, bool startofstatement=false) {
            List<IScriptToken> tokenlist=new List<IScriptToken>();
            List<OperatorIndex> indexlist=new List<OperatorIndex>();

            bool concat = true;
            bool done = false;
            int parsestart = index;

            SkipWhitespaces(data, ref index);
            while (index < data.Length && !done) {
                int starttoken = index;
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
                        IOperator @operator = ParseOperator(parsestart, data, ref index);
                        if (@operator == null)
                            // this most likely means the operator was actually a comment
                            break;

                        if ((@operator.Operator == Operator.Increment || @operator.Operator == Operator.Decrement) && !((IUnaryToken)@operator).IsPostToken && !concat)
                        {
                            index -= 2;
                            done = true;
                            break;
                        }

                        if (@operator.Operator == Operator.Subtraction && (tokenlist.Count == 0 || tokenlist[tokenlist.Count - 1] is IOperator))
                            @operator = new Negate();

                        indexlist.Add(new OperatorIndex(tokenlist.Count, @operator));
                        tokenlist.Add(@operator);
                        if (!(@operator is IUnaryToken))
                            concat = true;
                        break;
                    case '.':
                        ++index;
                        tokenlist[tokenlist.Count-1]=ParseMember(tokenlist[tokenlist.Count - 1], data, ref index, variables);
                        concat = false;
                        break;
                    case '$':
                        if (!concat)
                        {
                            done = true;
                            break;
                        }
                        
                        ++index;
                        tokenlist.Add(new ScriptVariable(variables, ParseName(data, ref index)));
                        TokenParsed?.Invoke(TokenType.Variable, starttoken, index);
                        concat = false;
                        break;
                    case '(':
                        ++index;
                        tokenlist.Add(ParseBlock(data, ref index, variables));
                        concat = false;
                        break;
                    case '[':
                        ++index;
                        if (tokenlist.Count==0||tokenlist[tokenlist.Count-1] is IOperator)
                            tokenlist.Add(new ScriptArray(ParseArray(data, ref index, variables)));
                        else tokenlist[tokenlist.Count-1] = new ScriptIndexer(tokenlist[tokenlist.Count - 1], ParseParameters(data, ref index, variables));
                        concat = false;
                        break;
                    case '{':
                        ++index;
                        return ParseStatementBlock(data, ref index, variables);
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
                        if (!concat) {
                            done = true;
                            break;
                        }

                        IScriptToken token = ParseToken(data, ref index, variables, startofstatement);
                        if (token is IControlToken || token is ParserToken || token is Switch || token is Return)
                            return token;
                        tokenlist.Add(token);
                        concat = false;
                        break;
                }
                if (index == starttoken && !done)
                    throw new ScriptParserException("Unable to parse code");

                SkipWhitespaces(data, ref index);
            }

            if (tokenlist.Count > 1)
            {
                indexlist.Sort((lhs, rhs) =>
                    lhs.Token.Operator.GetOrderNumber().CompareTo(rhs.Token.Operator.GetOrderNumber()));

                for (int i = 0; i < indexlist.Count; ++i)
                {
                    OperatorIndex operatorindex = indexlist[i];
                    if (operatorindex.Token is IUnaryToken unary)
                    {
                        if (unary.IsPostToken)
                        {
                            if (operatorindex.Index == 0)
                                throw new ScriptParserException("Posttoken at beginning of tokenlist detected");

                            unary.Operand = tokenlist[operatorindex.Index - 1];
                            tokenlist.RemoveAt(operatorindex.Index-1);
                        }
                        else
                        {
                            unary.Operand = tokenlist[operatorindex.Index + 1];
                            tokenlist.RemoveAt(operatorindex.Index + 1);
                            --operatorindex.Index;
                        }

                        for (int k = i; k < indexlist.Count; ++k)
                            if (indexlist[k].Index > operatorindex.Index)
                                --indexlist[k].Index;
                    }
                    else if (operatorindex.Token is IBinaryToken binary)
                    {
                        binary.Lhs = tokenlist[operatorindex.Index - 1];
                        binary.Rhs = tokenlist[operatorindex.Index + 1];
                        tokenlist.RemoveAt(operatorindex.Index + 1);
                        tokenlist.RemoveAt(operatorindex.Index - 1);
                        --operatorindex.Index;
                        for (int k = i; k < indexlist.Count; ++k)
                            if (indexlist[k].Index > operatorindex.Index)
                                indexlist[k].Index = indexlist[k].Index - 2;
                    }
                }
            }

            // there has to be a single statement or nothing at this point
            return tokenlist.SingleOrDefault();
        }

        IScriptToken ParseStatementBlock(string data, ref int index, IVariableProvider variables, bool methodblock=false) {
            SkipWhitespaces(data, ref index);

            variables = new VariableContext(variables);
            List<IScriptToken> statements = new List<IScriptToken>();
            while (index < data.Length) {
                if (index < data.Length && data[index] == '}') {
                    ++index;
                    break;
                }

                int tokenstart = index;
                IScriptToken token = Parse(data, ref index, variables, true);
                if (index == tokenstart)
                    throw new ScriptParserException("Unable to parse code");

                statements.Add(token);
                SkipWhitespaces(data, ref index);
            }

            if (statements.Count == 0)
                return null;
            if (statements.Count == 1) {
                return statements.Single();
            }


            for (int i = 0; i < statements.Count;++i)
            {
                // skip empty statements
                if(statements[i]==null)
                    continue;
                
                if (statements[i] is ControlToken control) {
                    if (i + 1 >= statements.Count)
                        throw new ScriptParserException("If statement without execution block detected");
                    control.Body = statements[i + 1];
                    statements.RemoveAt(i + 1);

                    if (control is Case @case) {
                        if (!(statements[i - 1] is Switch @switch))
                            throw new ScriptParserException("Case without connected switch statement found");
                        @switch.AddCase(@case);
                        statements.RemoveAt(i);
                        --i;
                    }
                    else if(control is Else @else)
                    {
                        if (!(statements[i - 1] is If @if))
                            throw new ScriptParserException("Else without connected if statement found");
                        @if.Else = @else.Body;
                        statements.RemoveAt(i);
                        --i;
                    }
                }
            }

            return new StatementBlock(statements.ToArray(), (IVariableContext) variables, methodblock);
        }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <param name="variables">variables valid for this script (flagged as read-only)</param>
        /// <returns>script which can get executed</returns>
        public IScript Parse(string data, params Variable[] variables) {
            VariableProvider variablecontext = new VariableProvider(globalvariables, variables);

            int index = 0;
            return new Script(ParseStatementBlock(data, ref index, variablecontext, true));
        }

        /// <inheritdoc />
        public event Action<TokenType, int, int> TokenParsed;
    }
}