using System;
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
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser : IScriptParser {
        readonly OperatorTree operatortree = new OperatorTree();
        readonly IVariableProvider globalvariables;
        readonly Dictionary<string, Type> supportedcasts=new Dictionary<string, Type>();

        Action<TokenType, int, int> tokenparsed;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="globalvariables">provider for global variables</param>
        public ScriptParser(IVariableProvider globalvariables) {
            InitializeOperators();
            this.globalvariables = globalvariables;
            Types.AddType<List<object>>("list");
            Types.AddType("task", new TaskProvider());
            supportedcasts["bool"] = typeof(bool);
            supportedcasts["char"] = typeof(char);
            supportedcasts["byte"] = typeof(byte);
            supportedcasts["sbyte"] = typeof(sbyte);
            supportedcasts["ushort"] = typeof(ushort);
            supportedcasts["short"] = typeof(short);
            supportedcasts["uint"] = typeof(uint);
            supportedcasts["int"] = typeof(int);
            supportedcasts["ulong"] = typeof(ulong);
            supportedcasts["long"] = typeof(long);
            supportedcasts["float"] = typeof(float);
            supportedcasts["double"] = typeof(double);
            supportedcasts["decimal"] = typeof(decimal);
            supportedcasts["string"] = typeof(string);
            tokenparsed = DefaultTokenParsed;
        }

        void DefaultTokenParsed(TokenType type, int start, int end) {
            TokenParsed?.Invoke(type, start, end);
        }

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="variables">global variables of script parser</param>
        public ScriptParser(params Variable[] variables)
            : this(new VariableProvider(null, variables.Concat(new[] {new Variable("task", new TaskMethodProvider())}).ToArray())) {
        }

        /// <summary>
        /// enables usage of new in scripts
        /// </summary>
        public bool TypeInstanceProvidersEnabled { get; set; } = true;

        /// <summary>
        /// enables usage of control tokens in scripts (if,for,while)
        /// </summary>
        public bool ControlTokensEnabled { get; set; } = true;

        /// <summary>
        /// enables type casts
        /// </summary>
        public bool TypeCastsEnabled { get; set; } = true;

        /// <summary>
        /// enables external method imports
        /// </summary>
        public bool ImportsEnabled { get; set; } = true;

        /// <summary>
        /// access to extensions available to script environment
        /// </summary>
        public IExtensionProvider Extensions { get; } = new ExtensionProvider();

        /// <summary>
        /// access to types which can be created using 'new' keyword
        /// </summary>
        public ITypeProvider Types { get; } = new TypeProvider();

        /// <summary>
        /// resolver which is used by 'import' statement to import methods
        /// </summary>
        public IExternalMethodProvider MethodResolver { get; set; }

        /// <summary>
        /// tree containing all supported operators
        /// </summary>
        public OperatorTree OperatorTree => operatortree;

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

        IScriptToken ParseSingleControlParameter(string data, ref int index, IVariableContext variables) {
            return ParseControlParameters(data, ref index, variables).Single();
        }

        IScriptToken TryParseSingleParameter(string data, ref int index, IVariableContext variables) {
            return TryParseControlParameters(data, ref index, variables).SingleOrDefault();
        }

        IScriptToken[] TryParseControlParameters(string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (index >= data.Length || data[index] != '(')
                return new IScriptToken[0];
            return ParseControlParameters(data, ref index, variables);
        }

        IScriptToken[] ParseControlParameters(string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (data[index] != '(')
                throw new ScriptParserException("Expected parameters for control statement");
            ++index;
            return ParseParameters(data, ref index, variables);
        }

        void ForeachTokenParsed(TokenType type, int start, int end) {
            if (type != TokenType.Variable && type != TokenType.Parameter)
                throw new ScriptParserException("First token has to be iterator variable");

            DefaultTokenParsed(TokenType.Variable, start, end);
            tokenparsed = DefaultTokenParsed;
        }

        IScriptToken AnalyseToken(string token, string data, int start, ref int index, IVariableContext variables, bool first) {
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
                if (ControlTokensEnabled) {
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

                        // overwrite token type event
                        tokenparsed = ForeachTokenParsed;
                        Foreach foreachloop= new Foreach(ParseControlParameters(data, ref index, variables));

                        // iterator variable is provided by foreach loop itself
                        foreachloop.Iterator.IsResolved = true;
                        variables.SetVariable(foreachloop.Iterator.Name, null);

                        // reset token type event just in case code is malformed
                        tokenparsed = DefaultTokenParsed;
                        return foreachloop;
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
                        return new Return(TryParseSingleParameter(data, ref index, variables));
                    case "throw":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Throw(ParseControlParameters(data, ref index, variables));
                    case "break":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Break(TryParseSingleParameter(data, ref index, variables));
                    case "continue":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Continue(TryParseSingleParameter(data, ref index, variables));
                    case "using":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Using(ParseControlParameters(data, ref index, variables));
                    case "try":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Try();
                    case "catch":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Catch();
                    case "wait":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Wait(ParseControlParameters(data, ref index, variables));
                    }
                }

                if (ImportsEnabled && token == "import") {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    if (MethodResolver == null)
                        throw new ScriptParserException("Import statement is unavailable since no method resolver is set.");
                    return new Import(MethodResolver, ParseSingleControlParameter(data, ref index, variables));
                }
            }

            SkipWhitespaces(data, ref index);
            if (TypeCastsEnabled) {
                if (supportedcasts.ContainsKey(token) && index < data.Length && data[index] == '(') {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new TypeCast(supportedcasts[token], ParseControlParameters(data, ref index, variables).Single());
                }
            }

            if (TypeInstanceProvidersEnabled && token == "new") {
                TokenParsed?.Invoke(TokenType.Control, start, index);
                start = index;
                string type = ParseName(data, ref index);
                TokenParsed?.Invoke(TokenType.Type, start, index);
                ITypeInstanceProvider typeprovider = Types.GetType(type.ToLower());
                return new NewInstance(typeprovider, ParseControlParameters(data, ref index, variables));
            }

            if (ControlTokensEnabled) {
                switch (token) {
                case "ref":
                    IAssignableToken reference = ParseSingleControlParameter(data, ref index, variables) as IAssignableToken;
                    if(reference==null)
                        throw new ScriptParserException($"ref can only be used with an {nameof(IAssignableToken)}");
                    return new Reference(reference);
                case "await":
                    return new Await(ParseSingleControlParameter(data, ref index, variables));
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
            }

            ScriptVariable variable = AnalyseVariable(token, variables, data, ref index);
            tokenparsed(variable.IsResolved ? TokenType.Variable : TokenType.Parameter, start, index);
            return variable;
        }

        ScriptVariable AnalyseVariable(string name, IVariableContext variables, string code, ref int codeindex) {
            ScriptVariable variable = new ScriptVariable(name);
            variable.IsResolved = variables.GetProvider(variable.Name) != null;

            // check if an assignment is following
            if (Peek(code, ref codeindex) == '=' && codeindex < code.Length - 1 && code[codeindex + 1] != '=') {
                variable.IsResolved = true;
                variables.SetVariable(variable.Name, null);
            }

            return variable;
        }

        IScriptToken ParseToken(string data, ref int index, IVariableContext variables, bool startofstatement) {
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

        IScriptToken ParseMethodCall(IScriptToken host, string methodname, string data, int start, ref int index, IVariableContext variables) {
            TokenParsed?.Invoke(TokenType.Method, start, index);
            return new ScriptMethod(Extensions, host, methodname, ParseParameters(data, ref index, variables));
        }

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, IVariableContext variables) {
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
                    return ParseMethodCall(host, membername.ToString(), data, start, ref index, variables);
                }
            }

            if (membername.Length > 0) {
                TokenParsed?.Invoke(TokenType.Property, start, index);
                return new ScriptMember(host, membername.ToString());
            }

            throw new ScriptParserException("Member name expected");
        }

        IScriptToken[] ParseArray(string data, ref int index, IVariableContext variables) {
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

        IScriptToken[] ParseParameters(string data, ref int index, IVariableContext variables) {
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
                throw new ScriptParserException("Block not terminated");

            ++index;
            return new ArithmeticBlock(block);
        }

        char Peek(string data, ref int index) {
            SkipWhitespaces(data, ref index);
            if (index < data.Length)
                return data[index];
            return '\0';
        }

        IScriptToken Parse(string data, ref int index, IVariableContext variables, bool startofstatement=false) {
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
                        string variablename = ParseName(data, ref index);
                        ScriptVariable parsedvariable = AnalyseVariable(variablename, variables, data, ref index);
                        tokenlist.Add(parsedvariable);
                        tokenparsed(parsedvariable.IsResolved?TokenType.Variable:TokenType.Parameter, starttoken, index);
                        concat = false;
                        break;
                    case '(':
                        ++index;
                        if (tokenlist.Count > 0 && tokenlist.Last() is ScriptVariable variable) {
                            tokenlist[tokenlist.Count - 1] = ParseMethodCall(variable, "invoke", data, starttoken, ref index, variables);
                        }
                        else {
                            //asd
                            tokenlist.Add(ParseBlock(data, ref index, variables));
                            concat = false;
                        }

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
                    //case ')':
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
                        if (token is IControlToken || token is ParserToken || token is Return)
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

        StatementBlock ParseStatementBlock(string data, ref int index, IVariableProvider variables, bool methodblock=false) {
            SkipWhitespaces(data, ref index);

            IVariableContext blockvariables = new VariableContext(variables);
            List<IScriptToken> statements = new List<IScriptToken>();
            while (index < data.Length) {
                if (index < data.Length && data[index] == '}') {
                    ++index;
                    break;
                }

                int tokenstart = index;
                IScriptToken token = Parse(data, ref index, blockvariables, true);
                if (index == tokenstart)
                    throw new ScriptParserException("Unable to parse code");

                statements.Add(token);
                SkipWhitespaces(data, ref index);
            }

            if (statements.Count == 0)
                return null;

            if (statements.Count == 1)
                return new StatementBlock(statements.ToArray(), methodblock);


            for (int i = 0; i < statements.Count; ++i) {
                // skip empty statements
                if (statements[i] == null)
                    continue;

                if (statements[i] is ControlToken control)
                    FetchBlock(control, statements, i + 1);
            }

            return new StatementBlock(statements.ToArray(), methodblock);
        }

        void FetchBlock(ControlToken control, List<IScriptToken> tokens, int index) {
            if (index >= tokens.Count)
                throw new ScriptParserException("Statement block expected");

            if (control is Switch @switch) {
                while (index < tokens.Count && tokens[index] is Case @case) {
                    FetchBlock(@case, tokens, index + 1);
                    @switch.AddCase(@case);
                    tokens.RemoveAt(index);
                }
                return;
            }

            IScriptToken block = tokens[index];
            if (block is ControlToken subcontrol)
                FetchBlock(subcontrol, tokens, index + 1);
            control.Body = block;
            tokens.RemoveAt(index);

            if (control is If @if) {
                if (index < tokens.Count && tokens[index] is Else @else) {
                    FetchBlock(@else, tokens, index + 1);
                    @if.Else = @else.Body;
                    tokens.RemoveAt(index);
                }
            }

            if (control is Try @try) {
                if (index < tokens.Count && tokens[index] is Catch @catch) {
                    FetchBlock(@catch, tokens, index + 1);
                    @try.Catch = @catch.Body;
                    tokens.RemoveAt(index);
                }
            }
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
            return new Script(ParseStatementBlock(data, ref index, variablecontext, true), variablecontext);
        }

        /// <inheritdoc />
        public event Action<TokenType, int, int> TokenParsed;
    }
}