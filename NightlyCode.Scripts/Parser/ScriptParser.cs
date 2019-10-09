using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// determines whether meta tokens are included in parsed script
        /// </summary>
        public bool MetatokensEnabled { get; set; }

        /// <inheritdoc />
        public IVariableProvider GlobalVariables => globalvariables;

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
            operatortree.Add("=>", Operator.Lambda);
        }

        int SkipWhitespaces(string data, ref int index) {
            int newlinecount = 0;
            while (index < data.Length && char.IsWhiteSpace(data[index])) {
                if (data[index] == '\n')
                    ++newlinecount;
                ++index;
            }

            return newlinecount;
        }

        IScriptToken ParseSingleControlParameter(string data, ref int index, IVariableContext variables) {
            int start = index;
            IScriptToken[] parameters = ParseControlParameters(null, data, ref index, variables);
            if (parameters.Length == 0)
                throw new ScriptParserException(start, index, "A parameter was expected");
            if(parameters.Length>1)
                throw new ScriptParserException(start, index, "Only one parameter was expected");
            return parameters[0];
        }

        IScriptToken TryParseSingleParameter(string data, ref int index, IVariableContext variables) {
            return TryParseControlParameters(data, ref index, variables).SingleOrDefault();
        }

        IScriptToken[] TryParseControlParameters(string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (index >= data.Length || data[index] != '(')
                return new IScriptToken[0];
            return ParseControlParameters(null, data, ref index, variables);
        }

        IScriptToken[] ParseControlParameters(IScriptToken parent, string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (index >= data.Length || data[index] != '(')
                throw new ScriptParserException(index, index, "Expected parameters for control statement");

            ++index;
            return ParseParameters(parent, data, ref index, variables);
        }

        void ForeachTokenParsed(TokenType type, int start, int end) {
            if (type != TokenType.Variable && type != TokenType.Parameter)
                throw new ScriptParserException(start, end, "First token has to be iterator variable");

            DefaultTokenParsed(TokenType.Variable, start, end);
            tokenparsed = DefaultTokenParsed;
        }

        IScriptToken AnalyseToken(string token, string data, int start, ref int index, IVariableContext variables, bool first) {
            if (token.Length == 0)
                throw new ScriptParserException(start, index, "token expected");

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
                    IScriptToken[] parameters;
                    switch (token) {
                    case "if":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new If(ParseSingleControlParameter(data, ref index, variables));
                    case "else":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Else();
                    case "for":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, variables);
                        if (parameters.Length != 3)
                            throw new ScriptParserException(start, index, "for loop needs 3 tokens as parameters");
                        return new For(parameters[0], parameters[1], parameters[2]);
                    case "foreach":
                        TokenParsed?.Invoke(TokenType.Control, start, index);

                        // overwrite token type event
                        tokenparsed = ForeachTokenParsed;
                        parameters = ParseControlParameters(null, data, ref index, variables);
                        if (parameters.Length < 2)
                            throw new ScriptParserException(start, index, "foreach needs an iterator and a collection");
                        if (parameters.Length > 2)
                            throw new ScriptParserException(start, index, "foreach must only specify an iterator and a collection");
                        if (!(parameters[0] is ScriptVariable foreachvariable))
                            throw new ScriptParserException(start, index, "foreach iterator must be a variable");

                        Foreach foreachloop = new Foreach(foreachvariable, parameters[1]);

                        // iterator variable is provided by foreach loop itself
                        foreachloop.Iterator.IsResolved = true;
                        variables.SetVariable(foreachloop.Iterator.Name, null);

                        // reset token type event just in case code is malformed
                        tokenparsed = DefaultTokenParsed;
                        return foreachloop;
                    case "while":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new While(ParseSingleControlParameter(data, ref index, variables));
                    case "switch":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Switch(ParseSingleControlParameter(data, ref index, variables));
                    case "case":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Case(ParseControlParameters(null, data, ref index, variables));
                    case "default":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Case();
                    case "return":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Return(TryParseSingleParameter(data, ref index, variables));
                    case "throw":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, variables);
                        if (parameters.Length < 1)
                            throw new ScriptParserException(start, index, "No exception message provided");
                        if (parameters.Length > 2)
                            throw new ScriptParserException(start, index, "Too many arguments for exception throw");
                        return new Throw(parameters[0], parameters.Length > 1 ? parameters[1] : null);
                    case "break":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Break(TryParseSingleParameter(data, ref index, variables));
                    case "continue":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Continue(TryParseSingleParameter(data, ref index, variables));
                    case "using":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Using(ParseControlParameters(null, data, ref index, variables));
                    case "try":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Try();
                    case "catch":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Catch();
                    case "wait":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Wait(ParseSingleControlParameter(data, ref index, variables));
                    }
                }

                if (ImportsEnabled && token == "import") {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    if (MethodResolver == null)
                        throw new ScriptParserException(start, index, "Import statement is unavailable since no method resolver is set.");
                    return new Import(MethodResolver, ParseControlParameters(null, data, ref index, variables));
                }
            }

            //SkipWhitespaces(data, ref index);
            if (TypeCastsEnabled) {
                if (supportedcasts.ContainsKey(token) && index < data.Length && data[index] == '(') {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new TypeCast(token, supportedcasts[token], ParseControlParameters(null, data, ref index, variables).Single());
                }
            }

            if (TypeInstanceProvidersEnabled && token == "new") {
                TokenParsed?.Invoke(TokenType.Control, start, index);
                start = index;
                string type = ParseName(data, ref index);
                TokenParsed?.Invoke(TokenType.Type, start, index);
                ITypeInstanceProvider typeprovider = Types.GetType(type.ToLower());
                if (typeprovider == null)
                    throw new ScriptParserException(start, index, $"Unknown type {type}");

                // setting a parent to non null forces the parser to interpret '{' as statement block and not as dictionary
                return new NewInstance(type, typeprovider, ParseControlParameters(typeprovider is TaskProvider ? new ParserToken("task") : null, data, ref index, variables));
            }

            if (ControlTokensEnabled) {
                switch (token) {
                case "ref":
                    IAssignableToken reference = ParseSingleControlParameter(data, ref index, variables) as IAssignableToken;
                    if(reference==null)
                        throw new ScriptParserException(start, index, $"ref can only be used with an {nameof(IAssignableToken)}");
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Reference(reference);
                case "await":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
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
            if (Peek(code, codeindex, out int foundindex) == '=' && Peek(code, foundindex+1)  != '=') {
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

            int dotcount = numberdata.Count(digit => digit == '.');

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
                throw new ScriptParserException(start, index, "Character literal not terminated");
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

            throw new ScriptParserException(start, index, "Literal not terminated");
        }

        IScriptToken ParseStringInterpolation(IScriptToken parent, string data, ref int index, IVariableProvider variables) {
            List<IScriptToken> tokens = new List<IScriptToken>();

            int start = index - 1;
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];

                switch(character) {
                case '"':
                    ++index;
                    tokens.Add(new ScriptValue(literal.ToString()));
                    TokenParsed?.Invoke(TokenType.Literal, start, index);
                    return new StringInterpolation(tokens.ToArray());
                case '{':
                    ++index;
                    if (Peek(data, index) == '{') {
                        literal.Append('{');
                    }
                    else {
                        TokenParsed?.Invoke(TokenType.Literal, start, index);
                        tokens.Add(new ScriptValue(literal.ToString()));
                        literal.Length = 0;
                        tokens.Add(ParseStatementBlock(parent, data, ref index, variables));
                        start = index;

                        // for loop automatically increases index, but index should remain here
                        --index;
                    }
                    break;
                case '\\':
                    ++index;
                    literal.Append(ParseSpecialCharacter(data[index]));
                    break;
                default:
                    literal.Append(character);
                    break;
                }
            }

            throw new ScriptParserException(start, index, "Literal not terminated");
        }

        IScriptToken ParseMethodCall(IScriptToken host, string methodname, string data, int start, ref int index, IVariableContext variables) {
            TokenParsed?.Invoke(TokenType.Method, start, index);
            return new ScriptMethod(Extensions, host, methodname, ParseParameters(null, data, ref index, variables));
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

            throw new ScriptParserException(start, index, "Member name expected");
        }

        IScriptToken[] ParseArray(IScriptToken parent, string data, ref int index, IVariableContext variables) {
            int newlines = 0;
            SkipWhitespaces(data, ref index);
            int start = index;
            List<IScriptToken> array = new List<IScriptToken>();
            for (; index < data.Length;)
            {
                char character = data[index];
                switch (character)
                {
                case '[':
                    ++index;
                    array.Add(new ScriptArray(ParseArray(null, data, ref index, variables)));
                    break;
                case ']':
                    ++index;
                    return array.ToArray();
                case ',':
                    ++index;
                    break;
                default:
                    IScriptToken element = Parse(parent, data, ref index, ref newlines, variables);
                    if (element == null)
                        throw new ScriptParserException(start, index, "Invalid array specification");
                    array.Add(element);
                    break;
                }
            }

            throw new ScriptParserException(start, index, "Array not terminated");
        }

        IScriptToken[] ParseParameters(IScriptToken parent, string data, ref int index, IVariableContext variables) {
            int newlines = 0;
            int start = index;
            List<IScriptToken> parameters = new List<IScriptToken>();
            for(; index < data.Length;) {
                char character = data[index];
                switch(character) {
                    /*case '[':
                        ++index;
                        parameters.Add(new ScriptArray(ParseArray(null, data, ref index, variables)));
                        break;*/
                    case ')':
                    case ']':
                        ++index;
                        return parameters.ToArray();
                    case ',':
                        ++index;
                        break;
                    default:
                        parameters.Add(Parse(parent, data, ref index, ref newlines, variables));
                        break;
                }
            }

            throw new ScriptParserException(start, index, "Parameter list not terminated");
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
                throw new ScriptParserException(startoperator, index, "Operator expected but nothing found");

            TokenParsed?.Invoke(TokenType.Operator, startoperator, index);
            switch (node.Operator) {
                case Operator.Increment:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Increment(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Increment(false);
                    else
                        throw new ScriptParserException(startoperator, index, "Increment without connected operand detected");
                case Operator.Decrement:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Decrement(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Decrement(false);
                    else
                        throw new ScriptParserException(startoperator, index, "Increment without connected operand detected");
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
                case Operator.Lambda:
                    return new LambdaToken();
                default:
                    throw new ScriptParserException(startoperator, index, $"Unsupported operator '{node.Operator}'");
            }
        }

        Comment ParseMultiLineComment(string data, ref int index) {
            StringBuilder builder=new StringBuilder();
            while (index < data.Length - 1 && (data[index] != '*' || data[index + 1] != '/'))
                builder.Append(data[index++]);

            index += 2;
            return new Comment(builder.ToString());
        }

        Comment ParseSingleLineComment(string data, ref int index) {
            StringBuilder builder=new StringBuilder();
            while (index < data.Length && data[index] != '\n' && data[index]!='\r')
                builder.Append(data[index++]);

            ++index;
            return new Comment(builder.ToString(), false);
        }

        IScriptToken ParseBlock(IScriptToken parent, string data, ref int index, IVariableContext variables) {
            int newlines = 0;
            int start = index;
            IScriptToken block = Parse(parent, data, ref index, ref newlines, variables);
            while (index < data.Length) {
                if (char.IsWhiteSpace(data[index]))
                {
                    ++index;
                    continue;
                }

                break;
            }

            if (data[index] != ')')
                throw new ScriptParserException(start, index, "Block not terminated");

            ++index;
            return new ArithmeticBlock(block);
        }

        char Peek(string data, int index) {
            return Peek(data, index, out int peekindex);
        }

        char Peek(string data, int index, out int peenindex) {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;

            if (index < data.Length) {
                peenindex = index;
                return data[index];
            }

            peenindex = -1;
            return '\0';
        }

        IScriptToken Parse(IScriptToken parent, string data, ref int index, ref int newlines, IVariableContext variables, bool startofstatement=false) {
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
                        if (data[index] == '/' && index<data.Length-1 && (data[index+1] == '/' || data[index+1] == '*')) {
                            if (tokenlist.Count==0) {
                                index += 2;
                                Comment comment;
                                switch (data[index - 1]) {
                                    case '/':
                                        comment = ParseSingleLineComment(data, ref index);
                                        break;
                                    case '*':
                                        comment = ParseMultiLineComment(data, ref index);
                                        break;
                                    default:
                                        // can actually never happen
                                        throw new ScriptParserException(parsestart, index, "Nani");
                                }

                                newlines = 0;
                                TokenParsed?.Invoke(TokenType.Comment, parsestart, index);
                                if(MetatokensEnabled)
                                    tokenlist.Add(comment);
                            }
                            done = true;
                            break;
                        }

                        IOperator @operator = ParseOperator(parsestart, data, ref index);
                        if (@operator == null) {
                            // this most likely means the operator was actually a comment
                            done = true;
                            break;
                        }

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
                        if (Peek(data, index) == '\"') {
                            ++index;
                            tokenlist.Add(ParseStringInterpolation(parent, data, ref index, variables));
                        }
                        else {
                            string variablename = ParseName(data, ref index);
                            ScriptVariable parsedvariable = AnalyseVariable(variablename, variables, data, ref index);
                            tokenlist.Add(parsedvariable);
                            tokenparsed(parsedvariable.IsResolved?TokenType.Variable:TokenType.Parameter, starttoken, index);
                        }
                        concat = false;
                        break;
                    case '(':
                        ++index;
                        if (tokenlist.Count > 0 && tokenlist.Last() is ScriptVariable variable) {
                            tokenlist[tokenlist.Count - 1] = ParseMethodCall(variable, "invoke", data, starttoken, ref index, variables);
                        }
                        else {
                            tokenlist.Add(ParseBlock(parent, data, ref index, variables));
                            concat = false;
                        }

                        break;
                    case '[':
                        ++index;
                        if (tokenlist.Count==0||tokenlist[tokenlist.Count-1] is IOperator)
                            tokenlist.Add(new ScriptArray(ParseArray(parent, data, ref index, variables)));
                        else tokenlist[tokenlist.Count-1] = new ScriptIndexer(tokenlist[tokenlist.Count - 1], ParseParameters(parent, data, ref index, variables));
                        concat = false;
                        break;
                    case '{':
                        ++index;
                        IOperator op = tokenlist.LastOrDefault() as IOperator;
                        if ((parent is null || op!=null) && op?.Operator!=Operator.Lambda) {
                            tokenlist.Add(ParseDictionary(parent, data, ref index, variables));
                        }
                        else {
                            StatementBlock statementblock = ParseStatementBlock(parent, data, ref index, variables);
                            if (!statementblock.Children.Any())
                                throw new ScriptParserException(starttoken, index, "Empty statement block detected");
                            tokenlist.Add(statementblock);
                        }

                        concat = false;
                        break;
                    //case ')':
                    case ',':
                    case ']':
                        done = true;
                        break;
                    case ';':
                        if (tokenlist.Count == 0)
                            tokenlist.Add(new StatementBlock(new IScriptToken[0]));
                        ++index;
                        done = true;
                        break;
                    default:
                        if (!concat) {
                            done = true;
                            break;
                        }
                        
                        IScriptToken token = ParseToken(data, ref index, variables, startofstatement);
                        if (token is IStatementContainer || token is ParserToken || token is Return)
                            return token;
                        tokenlist.Add(token);
                        concat = false;
                        break;
                }
                if (index == starttoken && !done)
                    throw new ScriptParserException(starttoken, index, "Unable to parse code");

                if(!done)
                    newlines = SkipWhitespaces(data, ref index);
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
                                // TODO: provide indices if this can actually happen
                                throw new ScriptParserException(parsestart, index, "Posttoken at beginning of tokenlist detected");

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
                        if(operatorindex.Index==0)
                            throw new ScriptParserException(parsestart, index, "Left hand side operand expected");
                        if(operatorindex.Index>=tokenlist.Count-1)
                            throw new ScriptParserException(parsestart, index, "Right hand side operand expected");

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

        IScriptToken ParseDictionary(IScriptToken parent, string data, ref int index, IVariableContext variables) {
            int newlines = 0;
            DictionaryToken dictionary = new DictionaryToken();
            while (Peek(data, index) != '}') {
                IScriptToken key = Parse(parent, data, ref index, ref newlines, variables);
                IScriptToken value = null;
                if (Peek(data, index) == ':') {
                    ++index;
                    value = Parse(parent, data, ref index, ref newlines, variables);
                }

                dictionary.Add(key, value);
                if (Peek(data, index) == ',')
                    ++index;
            }

            // eat '{'
            ++index;
            return dictionary;
        }

        StatementBlock ParseStatementBlock(IScriptToken parent, string data, ref int index, IVariableProvider variables, bool methodblock=false) {
            SkipWhitespaces(data, ref index);

            VariableContext blockvariables = new VariableContext(variables);
            List<IScriptToken> statements = new List<IScriptToken>();

            int start = index;
            bool terminated = false;
            while (index < data.Length) {
                int newlines = 0;
                if (index < data.Length && data[index] == '}') {
                    ++index;
                    terminated = true;
                    break;
                }

                int tokenstart = index;
                if (statements.LastOrDefault() is Catch)
                    blockvariables.SetVariable("exception", null);

                IScriptToken token;
                try {
                    token = Parse(statements.LastOrDefault() is IStatementContainer control ? control : parent, data, ref index, ref newlines, blockvariables, true);
                }
                catch (ScriptParserException e) {
                    if (e.StartIndex == -1 || e.EndIndex == -1)
                        throw new ScriptParserException(tokenstart, index, e.Message, e);
                    throw;
                }

                if (index == tokenstart)
                    throw new ScriptParserException(tokenstart, index, "Unable to parse code");

                if (token != null) {
                    if (statements.Count >= 2 && statements[statements.Count - 2] is Catch)
                        blockvariables.RemoveVariable("exception");

                    if (token is Comment comment && newlines == 0 && statements.Count > 0 && statements.Last() is ICommentContainer container) {
                        container.AddComment(comment);
                    }
                    else statements.Add(token);
                }

                if (MetatokensEnabled && newlines > 1)
                    statements.Add(new NewLine());
                newlines = SkipWhitespaces(data, ref index);
                if (MetatokensEnabled && newlines > 1)
                    statements.Add(new NewLine());
            }

            if (!terminated && !methodblock)
                throw new ScriptParserException(start, index, "Unterminated Statementblock");

            if (statements.Count <= 1)
                return new StatementBlock(statements.ToArray(), methodblock);


            for (int i = 0; i < statements.Count; ++i) {
                // skip empty statements
                if (statements[i] == null)
                    continue;

                if (statements[i] is ControlToken control)
                    FetchBlock(control, statements, i + 1, start, index);
            }

            return new StatementBlock(statements.ToArray(), methodblock);
        }

        IScriptToken FetchMetatokens(List<IScriptToken> result, List<IScriptToken> tokens, int index) {
            result.Clear();
            if (index >= tokens.Count)
                return null;
            IScriptToken block = tokens[index];
            while (block is Comment || block is NewLine) {
                result.Add(block);
                tokens.RemoveAt(index);
                if (index >= tokens.Count)
                    return null;

                block = tokens[index];
            }

            return block;
        }

        void FetchBlock(ControlToken control, List<IScriptToken> tokens, int index, int parserstart, int parserindex) {
            if (index >= tokens.Count)
                throw new ScriptParserException(parserstart, parserindex, "Statement block expected");

            if (control is Switch @switch) {
                while (index < tokens.Count && (tokens[index] is Case || tokens[index] is Comment || tokens[index] is NewLine)) {
                    if (tokens[index] is Case @case) {
                        FetchBlock(@case, tokens, index + 1, parserstart, parserindex);
                        @switch.AddCase(@case);
                    }
                    else if (tokens[index] is Comment @comment) {
                        if (index < tokens.Count - 1 && tokens[index + 1] is Case @innercase) {
                            innercase.AddComment(comment);
                        }
                    }
                    tokens.RemoveAt(index);
                }
                return;
            }

            List<IScriptToken> metatokens=new List<IScriptToken>();
            IScriptToken block = FetchMetatokens(metatokens, tokens, index);

            if (block is ControlToken subcontrol)
                FetchBlock(subcontrol, tokens, index + 1, parserstart, parserindex);

            if (metatokens.Count > 0) {
                if (block is StatementBlock statementblock) {
                    control.Body = new StatementBlock(metatokens.Concat(statementblock.Children).ToArray());
                }
                else control.Body = new StatementBlock(metatokens.Concat(new[] {block}).ToArray());
            }
            else control.Body = block;

            tokens.RemoveAt(index);

            if (control is If @if) {
                block=FetchMetatokens(metatokens, tokens, index);
                if (block is Else @else) {
                    FetchBlock(@else, tokens, index + 1, parserstart, parserindex);
                    @if.Else = @else.Body;
                    tokens.RemoveAt(index);
                }
                else {
                    for (int i = metatokens.Count - 1; i >= 0; --i)
                        tokens.Insert(index, metatokens[i]);
                }
            }
            else if (control is Try @try) {
                block=FetchMetatokens(metatokens, tokens, index);
                if (block is Catch @catch) {
                    FetchBlock(@catch, tokens, index + 1, parserstart, parserindex);
                    @try.Catch = @catch.Body;
                    tokens.RemoveAt(index);
                }
                else {
                    for (int i = metatokens.Count - 1; i >= 0; --i)
                        tokens.Insert(index, metatokens[i]);
                }
            }
        }

        /// <inheritdoc />
        public IScript Parse(string data, params Variable[] variables) {
            VariableProvider variablecontext = new VariableProvider(globalvariables, variables);

            int index = 0;
            return new Script(ParseStatementBlock(null, data, ref index, variablecontext, true), variablecontext);
        }

        /// <inheritdoc />
        public Task<IScript> ParseAsync(string data, params Variable[] variables) {
            return Task.Run(() => Parse(data, variables));
        }

        /// <inheritdoc />
        public event Action<TokenType, int, int> TokenParsed;
    }
}