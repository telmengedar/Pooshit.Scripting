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
using Switch = NightlyCode.Scripting.Control.Switch;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser : IScriptParser {
        readonly OperatorTree operatortree = new OperatorTree();
        readonly IVariableProvider globalvariables;
        readonly Dictionary<string, Type> supportedcasts=new Dictionary<string, Type>();

        Action<TokenType, int, int, int> tokenparsed;

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="globalvariables">provider for global variables</param>
        public ScriptParser(IVariableProvider globalvariables) {
            InitializeOperators();
            this.globalvariables = globalvariables;
            Types.AddType<List<object>>("list");
            Types.AddType("task", new TaskProvider());
            Types.AddType<bool>("bool");
            Types.AddType<byte>("byte");
            Types.AddType<sbyte>("sbyte");
            Types.AddType<char>("char");
            Types.AddType<string>("string");
            Types.AddType<short>("short");
            Types.AddType<int>("int");
            Types.AddType<ushort>("ushort");
            Types.AddType<uint>("uint");
            Types.AddType<ulong>("ulong");
            Types.AddType<long>("long");
            Types.AddType<float>("float");
            Types.AddType<double>("double");
            Types.AddType<decimal>("decimal");


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

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="variables">global variables of script parser</param>
        public ScriptParser(params Variable[] variables)
            : this(new VariableProvider(null, variables.Concat(new[] {new Variable("task", new TaskMethodProvider())}).ToArray())) {
        }

        void DefaultTokenParsed(TokenType type, int start, int end, int linenumber) {
            TokenParsed?.Invoke(type, start, end);
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

        int SkipWhitespaces(string data, ref int index, ref int linenumber) {
            int newlinecount = 0;
            while (index < data.Length && char.IsWhiteSpace(data[index])) {
                if (data[index] == '\n') {
                    ++newlinecount;
                    ++linenumber;
                }

                ++index;
            }

            return newlinecount;
        }

        IScriptToken ParseSingleControlParameter(string data, ref int index, ref int linenumber, IVariableContext variables) {
            int start = index;
            int startline = linenumber;
            IScriptToken[] parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
            if (parameters.Length == 0)
                throw new ScriptParserException(start, index, startline, "A parameter was expected");
            if (parameters.Length > 1)
                throw new ScriptParserException(start, index, startline, "Only one parameter was expected");
            return parameters[0];
        }

        IScriptToken TryParseSingleParameter(string data, ref int index, ref int linenumber, IVariableContext variables) {
            return TryParseControlParameters(data, ref index, ref linenumber, variables).SingleOrDefault();
        }

        IScriptToken[] TryParseControlParameters(string data, ref int index, ref int linenumber, IVariableContext variables) {
            SkipWhitespaces(data, ref index, ref linenumber);
            if (index >= data.Length || data[index] != '(')
                return new IScriptToken[0];
            return ParseControlParameters(null, data, ref index, ref linenumber, variables);
        }

        IScriptToken[] ParseControlParameters(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableContext variables) {
            SkipWhitespaces(data, ref index, ref linenumber);
            if (index >= data.Length || data[index] != '(')
                throw new ScriptParserException(index, index, linenumber, "Expected parameters for control statement");

            ++index;
            return ParseParameters(parent, data, ref index, ref linenumber, variables);
        }

        void ForeachTokenParsed(TokenType type, int start, int end, int linenumber) {
            if (type != TokenType.Variable && type != TokenType.Parameter)
                throw new ScriptParserException(start, end, linenumber, "First token has to be iterator variable");

            DefaultTokenParsed(TokenType.Variable, start, end, linenumber);
            tokenparsed = DefaultTokenParsed;
        }

        IScriptToken AnalyseToken(string token, string data, int start, ref int index, ref int linenumber, IVariableContext variables, bool first) {
            if (token.Length == 0)
                throw new ScriptParserException(start, index, linenumber, "token expected");

            int startline = linenumber;

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
                        return new If(ParseSingleControlParameter(data, ref index, ref linenumber, variables));
                    case "else":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Else();
                    case "for":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
                        if (parameters.Length != 3)
                            throw new ScriptParserException(start, index, startline, "for loop needs 3 tokens as parameters");
                        return new For(parameters[0], parameters[1], parameters[2]);
                    case "foreach":
                        TokenParsed?.Invoke(TokenType.Control, start, index);

                        // overwrite token type event
                        tokenparsed = ForeachTokenParsed;
                        parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
                        if (parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "foreach needs an iterator and a collection");
                        if (parameters.Length > 2)
                            throw new ScriptParserException(start, index, startline, "foreach must only specify an iterator and a collection");
                        if (!(parameters[0] is ScriptVariable foreachvariable))
                            throw new ScriptParserException(start, index, startline, "foreach iterator must be a variable");

                        Foreach foreachloop = new Foreach(foreachvariable, parameters[1]);

                        // iterator variable is provided by foreach loop itself
                        foreachloop.Iterator.IsResolved = true;
                        variables.SetVariable(foreachloop.Iterator.Name, null);

                        // reset token type event just in case code is malformed
                        tokenparsed = DefaultTokenParsed;
                        return foreachloop;
                    case "while":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new While(ParseSingleControlParameter(data, ref index, ref linenumber, variables));
                    case "switch":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Switch(ParseSingleControlParameter(data, ref index, ref linenumber, variables));
                    case "case":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Case(ParseControlParameters(null, data, ref index, ref linenumber, variables));
                    case "default":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Case();
                    case "return":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Return(TryParseSingleParameter(data, ref index, ref linenumber, variables));
                    case "throw":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
                        if (parameters.Length < 1)
                            throw new ScriptParserException(start, index, startline, "No exception message provided");
                        if (parameters.Length > 2)
                            throw new ScriptParserException(start, index, startline, "Too many arguments for exception throw");
                        return new Throw(parameters[0], parameters.Length > 1 ? parameters[1] : null) {
                            LineNumber = startline,
                            TextIndex = start
                        };
                    case "break":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Break(TryParseSingleParameter(data, ref index, ref linenumber, variables));
                    case "continue":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Continue(TryParseSingleParameter(data, ref index, ref linenumber, variables));
                    case "using":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Using(ParseControlParameters(null, data, ref index, ref linenumber, variables)) {
                            LineNumber = startline,
                            TextIndex = start
                        };
                    case "try":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Try {
                            LineNumber = startline,
                            TextIndex = start
                        };
                    case "catch":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Catch();
                    case "wait":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        return new Wait(ParseSingleControlParameter(data, ref index, ref linenumber, variables)) {
                            LineNumber = startline,
                            TextIndex = start
                        };
                    case "cast":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
                        if (parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "Type cast needs a value and a target type as parameters");
                        if (parameters.Length > 3)
                            throw new ScriptParserException(start, index, startline, "Too many parameters for a type cast");
                        if (!(parameters[1] is ScriptValue typevalue))
                            throw new ScriptParserException(start, index, startline, "Second parameter must be a constant specifying the type to cast to");

                        string casttypename = typevalue.Value.ToString();
                        if (casttypename.EndsWith("[]"))
                            casttypename = casttypename.Substring(0, casttypename.Length - 2);

                        if (!Types.HasType(casttypename) && !casttypename.Contains(','))
                            throw new ScriptParserException(start, index, startline, $"Unknown cast target type '{typevalue.Value}'");

                        return new ExpliciteTypeCast(Types, parameters[0], typevalue, parameters.Length == 3 ? parameters[2] : null);
                    case "parameter":
                        TokenParsed?.Invoke(TokenType.Control, start, index);
                        parameters = ParseControlParameters(null, data, ref index, ref linenumber, variables);
                        if (parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "Parameter specification needs a variable and a parameter type");
                        if (parameters.Length > 3)
                            throw new ScriptParserException(start, index, startline, "Too many parameters for a parameter specification");
                        if (!(parameters[0] is ScriptVariable))
                            throw new ScriptParserException(start, index, startline, "First argument must be a variable");
                        if (!(parameters[1] is ScriptValue parametertype))
                            throw new ScriptParserException(start, index, startline, "Second argument must be a type name");
                        string typename = parametertype.Value.ToString();
                        if (typename.EndsWith("[]"))
                            typename = typename.Substring(0, typename.Length - 2);

                        if (!Types.HasType(typename) && !typename.Contains(','))
                            throw new ScriptParserException(start, index, startline, $"Unknown parameter type '{parametertype.Value}'");

                        return new ScriptParameter(Types, (ScriptVariable) parameters[0], parametertype, parameters.Length > 2 ? parameters[2] : null);
                    }
                }

                if (ImportsEnabled && token == "import") {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    if (MethodResolver == null)
                        throw new ScriptParserException(start, index, startline, "Import statement is unavailable since no method resolver is set.");
                    return new Import(MethodResolver, ParseControlParameters(null, data, ref index, ref linenumber, variables)) {
                        LineNumber = linenumber,
                        TextIndex = start
                    };
                }
            }

            //SkipWhitespaces(data, ref index);
            if (TypeCastsEnabled) {
                if (supportedcasts.ContainsKey(token) && index < data.Length && data[index] == '(') {
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new ImpliciteTypeCast(token, supportedcasts[token], ParseControlParameters(null, data, ref index, ref linenumber, variables).Single()) {
                        LineNumber = startline,
                        TextIndex = start
                    };
                }
            }

            if (TypeInstanceProvidersEnabled && token == "new") {
                TokenParsed?.Invoke(TokenType.Control, start, index);
                start = index;
                string type = ParseName(data, ref index, ref linenumber);
                TokenParsed?.Invoke(TokenType.Type, start, index);
                ITypeInstanceProvider typeprovider = Types.GetType(type.ToLower());
                if (typeprovider == null)
                    throw new ScriptParserException(start, index, startline, $"Unknown type {type}");

                // setting a parent to non null forces the parser to interpret '{' as statement block and not as dictionary
                return new NewInstance(type, typeprovider.ProvidedType, typeprovider, ParseControlParameters(typeprovider is TaskProvider ? new ParserToken("task") : null, data, ref index, ref linenumber, variables)) {
                    LineNumber = startline,
                    TextIndex = start
                };
            }

            if (ControlTokensEnabled) {
                switch (token) {
                case "ref":
                    IAssignableToken reference = ParseSingleControlParameter(data, ref index, ref linenumber, variables) as IAssignableToken;
                    if(reference==null)
                        throw new ScriptParserException(start, index, startline, $"ref can only be used with an {nameof(IAssignableToken)}");
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Reference(reference);
                case "await":
                    TokenParsed?.Invoke(TokenType.Control, start, index);
                    return new Await(ParseSingleControlParameter(data, ref index, ref linenumber, variables));
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

            ScriptVariable variable = AnalyseVariable(token, variables, data, ref index, start, startline);
            tokenparsed(variable.IsResolved ? TokenType.Variable : TokenType.Parameter, start, index, startline);
            return variable;
        }

        ScriptVariable AnalyseVariable(string name, IVariableContext variables, string code, ref int codeindex, int starttoken, int linenumber) {
            ScriptVariable variable = new ScriptVariable(name) {
                LineNumber = linenumber,
                TextIndex = starttoken
            };

            variable.IsResolved = variables.GetProvider(variable.Name) != null;

            // check if an assignment is following
            if (Peek(code, codeindex, out int foundindex) == '=' && Peek(code, foundindex + 1) != '=') {
                variable.IsResolved = true;
                variables.SetVariable(variable.Name, null);
            }

            return variable;
        }

        IScriptToken ParseToken(string data, ref int index, ref int linenumber, IVariableContext variables, bool startofstatement) {
            SkipWhitespaces(data, ref index, ref linenumber);
            int start = index;
            bool parsenumber = false;
            if (index < data.Length) {
                char character = data[index];
                if (char.IsDigit(character) || character == '.' || character == '-')
                    parsenumber = true;

                if (character == '"')
                {
                    ++index;
                    return ParseLiteral(data, ref index, linenumber);
                }

                if (character == '\'') {
                    ++index;
                    return ParseCharacter(data, ref index, linenumber);
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
                else return AnalyseToken(tokenname.ToString(), data, start, ref index, ref linenumber, variables, startofstatement);
            }

            if(tokenname.Length > 0)
                return AnalyseToken(tokenname.ToString(), data, start, ref index, ref linenumber, variables, startofstatement);
            return new ScriptValue(null);
        }

        string ParseName(string data, ref int index, ref int linenumber)
        {
            SkipWhitespaces(data, ref index, ref linenumber);
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

        IScriptToken ParseCharacter(string data, ref int index, int linenumber) {
            int start = index - 1;
            char character = data[index];
            if (character == '\\') {
                ++index;
                character = ParseSpecialCharacter(data[index]);
            }

            ++index;
            if (data[index] != '\'')
                throw new ScriptParserException(start, index, linenumber, "Character literal not terminated");
            ++index;
            TokenParsed?.Invoke(TokenType.Literal, start, index);
            return new ScriptValue(character);
        }

        IScriptToken ParseLiteral(string data, ref int index, int linenumber) {
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

            throw new ScriptParserException(start, index, linenumber, "Literal not terminated");
        }

        IScriptToken ParseStringInterpolation(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableProvider variables) {
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
                        int blockstart = index - 1;
                        int blockline = linenumber;
                        StatementBlock block = ParseStatementBlock(parent, data, ref index, ref linenumber, variables);
                        block.TextIndex = blockstart;
                        block.LineNumber = blockline;
                        tokens.Add(block);
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

            throw new ScriptParserException(start, index, linenumber, "Literal not terminated");
        }

        IScriptToken ParseMethodCall(IScriptToken host, string methodname, string data, int start, ref int index, ref int linenumber, IVariableContext variables) {
            TokenParsed?.Invoke(TokenType.Method, start, index);
            int methodline = linenumber;
            return new ScriptMethod(Extensions, host, methodname, ParseParameters(null, data, ref index, ref linenumber, variables)) {
                LineNumber = methodline,
                TextIndex = (host is ICodePositionToken position) ? position.TextIndex : start
            };
        }

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, ref int linenumber, IVariableContext variables) {
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
                    return ParseMethodCall(host, membername.ToString(), data, start, ref index, ref linenumber, variables);
                }
            }

            if (membername.Length > 0) {
                TokenParsed?.Invoke(TokenType.Property, start, index);
                return new ScriptMember(host, membername.ToString()) {
                    LineNumber = linenumber,
                    TextIndex = (host is ICodePositionToken position) ? position.TextIndex : start
                };
            }

            throw new ScriptParserException(start, index,linenumber, "Member name expected");
        }

        IScriptToken[] ParseArray(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableContext variables) {
            int newlines = 0;
            SkipWhitespaces(data, ref index, ref linenumber);
            int start = index;
            List<IScriptToken> array = new List<IScriptToken>();
            for (; index < data.Length;)
            {
                char character = data[index];
                switch (character)
                {
                case '[':
                    ++index;
                    array.Add(new ScriptArray(ParseArray(null, data, ref index, ref linenumber, variables)));
                    break;
                case ']':
                    ++index;
                    return array.ToArray();
                case ',':
                    ++index;
                    break;
                default:
                    IScriptToken element = Parse(parent, data, ref index, ref newlines, ref linenumber, variables);
                    if (element == null)
                        throw new ScriptParserException(start, index,linenumber, "Invalid array specification");
                    array.Add(element);
                    break;
                }
            }

            throw new ScriptParserException(start, index,linenumber, "Array not terminated");
        }

        IScriptToken[] ParseParameters(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableContext variables) {
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
                        parameters.Add(Parse(parent, data, ref index, ref newlines, ref linenumber, variables));
                        break;
                }
            }

            throw new ScriptParserException(start, index,linenumber, "Parameter list not terminated");
        }


        IOperator ParseOperator(int parsestart,string data, ref int index, int linenumber) {
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
                throw new ScriptParserException(startoperator, index, linenumber, "Operator expected but nothing found");

            TokenParsed?.Invoke(TokenType.Operator, startoperator, index);
            switch (node.Operator) {
                case Operator.Increment:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Increment(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Increment(false);
                    else
                        throw new ScriptParserException(startoperator, index, linenumber, "Increment without connected operand detected");
                case Operator.Decrement:
                    if (index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Decrement(true);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Decrement(false);
                    else
                        throw new ScriptParserException(startoperator, index, linenumber, "Increment without connected operand detected");
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
                    throw new ScriptParserException(startoperator, index, linenumber, $"Unsupported operator '{node.Operator}'");
            }
        }

        Comment ParseMultiLineComment(string data, ref int index, ref int linenumber) {
            int startlinenumber = linenumber;
            int startindex = index;

            StringBuilder builder=new StringBuilder();
            while (index < data.Length - 1 && (data[index] != '*' || data[index + 1] != '/')) {
                if (data[index] == '\n')
                    ++linenumber;
                builder.Append(data[index++]);
            }

            index += 2;
            return new Comment(builder.ToString().Trim(), startlinenumber, startindex);
        }

        Comment ParseSingleLineComment(string data, ref int index, int linenumber) {
            int startindex = index;
            StringBuilder builder=new StringBuilder();
            while (index < data.Length && data[index] != '\n' && data[index]!='\r')
                builder.Append(data[index++]);

            ++index;
            return new Comment(builder.ToString(), linenumber, startindex, false);
        }

        IScriptToken ParseBlock(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableContext variables) {
            int newlines = 0;
            int start = index;
            IScriptToken block = Parse(parent, data, ref index, ref newlines, ref linenumber, variables);
            while (index < data.Length) {
                if (char.IsWhiteSpace(data[index]))
                {
                    ++index;
                    continue;
                }

                break;
            }

            if (data[index] != ')')
                throw new ScriptParserException(start, index, linenumber, "Block not terminated");

            ++index;
            return new ArithmeticBlock(block);
        }

        char Peek(string data, int index) {
            return Peek(data, index, out int peekindex);
        }

        char Peek(string data, int index, out int peekindex) {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;

            if (index < data.Length) {
                peekindex = index;
                return data[index];
            }

            peekindex = -1;
            return '\0';
        }

        IScriptToken Parse(IScriptToken parent, string data, ref int index, ref int newlines, ref int linenumber, IVariableContext variables, bool startofstatement=false) {
            List<IScriptToken> tokenlist=new List<IScriptToken>();
            List<OperatorIndex> indexlist=new List<OperatorIndex>();

            bool concat = true;
            bool done = false;
            int parsestart = index;
            SkipWhitespaces(data, ref index, ref linenumber);
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
                                if(data[index - 1]=='/')
                                    comment = ParseSingleLineComment(data, ref index, linenumber);
                                else comment = ParseMultiLineComment(data, ref index, ref linenumber);

                                newlines = 0;
                                TokenParsed?.Invoke(TokenType.Comment, parsestart, index);
                                if(MetatokensEnabled)
                                    tokenlist.Add(comment);
                            }
                            done = true;
                            break;
                        }

                        IOperator @operator = ParseOperator(parsestart, data, ref index, linenumber);
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

                        if (@operator is ScriptToken codetoken) {
                            codetoken.LineNumber = linenumber;
                            codetoken.TextIndex = starttoken;
                        }

                        indexlist.Add(new OperatorIndex(tokenlist.Count, @operator));
                        tokenlist.Add(@operator);
                        if (!(@operator is IUnaryToken))
                            concat = true;
                        break;
                    case '.':
                        ++index;
                        tokenlist[tokenlist.Count-1]=ParseMember(tokenlist[tokenlist.Count - 1], data, ref index, ref linenumber, variables);
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
                            tokenlist.Add(ParseStringInterpolation(parent, data, ref index, ref linenumber, variables));
                        }
                        else {
                            string variablename = ParseName(data, ref index, ref linenumber);
                            ScriptVariable parsedvariable = AnalyseVariable(variablename, variables, data, ref index, starttoken, linenumber);
                            tokenlist.Add(parsedvariable);
                            tokenparsed(parsedvariable.IsResolved?TokenType.Variable:TokenType.Parameter, starttoken, index, linenumber);
                        }
                        concat = false;
                        break;
                    case '(':
                        ++index;
                        if (tokenlist.Count > 0 && tokenlist.Last() is ScriptVariable variable) {
                            tokenlist[tokenlist.Count - 1] = ParseMethodCall(variable, "invoke", data, starttoken, ref index, ref linenumber, variables);
                        }
                        else {
                            tokenlist.Add(ParseBlock(parent, data, ref index, ref linenumber, variables));
                            concat = false;
                        }

                        break;
                    case '[':
                        ++index;
                        if (tokenlist.Count==0||tokenlist[tokenlist.Count-1] is IOperator)
                            tokenlist.Add(new ScriptArray(ParseArray(parent, data, ref index, ref linenumber, variables)));
                        else {
                            int line = linenumber;
                            tokenlist[tokenlist.Count - 1] = new ScriptIndexer(tokenlist.Last(), ParseParameters(parent, data, ref index, ref linenumber, variables)) {
                                LineNumber = line,
                                TextIndex = starttoken
                            };
                        }
                        concat = false;
                        break;
                    case '{':
                        ++index;
                        IOperator op = tokenlist.LastOrDefault() as IOperator;
                        if ((parent is null || op!=null) && op?.Operator!=Operator.Lambda) {
                            tokenlist.Add(ParseDictionary(parent, data, ref index, ref linenumber, variables));
                        }
                        else {
                            StatementBlock statementblock = ParseStatementBlock(parent, data, ref index, ref linenumber, variables);
                            statementblock.TextIndex = starttoken;
                            statementblock.LineNumber = linenumber;

                            if (!statementblock.Children.Any())
                                throw new ScriptParserException(starttoken, index, linenumber, "Empty statement block detected");
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
                            tokenlist.Add(new StatementBlock(new IScriptToken[0]) {
                                LineNumber = linenumber,
                                TextIndex = starttoken
                            });
                        ++index;
                        done = true;
                        break;
                    default:
                        if (!concat) {
                            done = true;
                            break;
                        }
                        
                        IScriptToken token = ParseToken(data, ref index, ref linenumber, variables, startofstatement);
                        if (token is IStatementContainer || token is ParserToken || token is Return)
                            return token;
                        tokenlist.Add(token);
                        concat = false;
                        break;
                }
                if (index == starttoken && !done)
                    throw new ScriptParserException(starttoken, index, linenumber, "Unable to parse code");

                if(!done)
                    newlines = SkipWhitespaces(data, ref index, ref linenumber);
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
                                throw new ScriptParserException(parsestart, index, linenumber, "Posttoken at beginning of tokenlist detected");

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
                            throw new ScriptParserException(parsestart, index, linenumber, "Left hand side operand expected");
                        if(operatorindex.Index>=tokenlist.Count-1)
                            throw new ScriptParserException(parsestart, index, linenumber, "Right hand side operand expected");

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

        IScriptToken ParseDictionary(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableContext variables) {
            int newlines = 0;
            DictionaryToken dictionary = new DictionaryToken();
            while (Peek(data, index) != '}') {
                IScriptToken key = Parse(parent, data, ref index, ref newlines, ref linenumber, variables);
                IScriptToken value = null;
                if (Peek(data, index) == ':') {
                    ++index;
                    value = Parse(parent, data, ref index, ref newlines, ref linenumber, variables);
                }

                dictionary.Add(key, value);
                if (Peek(data, index) == ',')
                    ++index;
            }
            // eat '{'
            ++index;
            return dictionary;
        }

        StatementBlock ParseStatementBlock(IScriptToken parent, string data, ref int index, ref int linenumber, IVariableProvider variables, bool methodblock=false) {
            VariableContext blockvariables = new VariableContext(variables);
            List<IScriptToken> statements = new List<IScriptToken>();

            if (MetatokensEnabled && SkipWhitespaces(data, ref index, ref linenumber) > 1)
                statements.Add(new NewLine());
            else SkipWhitespaces(data, ref index, ref linenumber);

 
            int start = index;
            int startline = linenumber;
            bool terminated = false;
            int newlinesafterlasttoken = 0;
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
                    token = Parse(statements.LastOrDefault() is IStatementContainer control ? control : parent, data, ref index, ref newlines, ref linenumber, blockvariables, true);

                    if(!(token is Comment))
                        newlinesafterlasttoken = newlines;
                }
                catch (ScriptParserException e) {
                    if (e.StartIndex == -1 || e.EndIndex == -1)
                        throw new ScriptParserException(tokenstart, index, startline, e.Message, e);
                    throw;
                }

                if (index == tokenstart)
                    throw new ScriptParserException(tokenstart, index, linenumber, "Unable to parse code");

                if (token != null) {
                    if (statements.Count >= 2 && statements[statements.Count - 2] is Catch)
                        blockvariables.RemoveVariable("exception");

                    if (token is Comment comment && newlinesafterlasttoken == 0 && statements.Count > 0 && !(statements.Last() is NewLine || statements.Last() is Comment))
                        comment.IsPostComment = true;

                    statements.Add(token);
                }

                if (MetatokensEnabled && newlines > 1)
                    statements.Add(new NewLine());

                newlines = SkipWhitespaces(data, ref index, ref linenumber);
                if (newlinesafterlasttoken == 0)
                    newlinesafterlasttoken = newlines;
                if (MetatokensEnabled && newlines > 1 && !(statements.Last() is NewLine))
                    statements.Add(new NewLine());
            }

            if (!terminated && !methodblock)
                throw new ScriptParserException(start, index, linenumber, "Unterminated Statementblock");

            if (statements.Count <= 1)
                return new StatementBlock(statements.ToArray(), methodblock) {
                    LineNumber = startline,
                    TextIndex = start
                };


            for (int i = 0; i < statements.Count; ++i) {
                // skip empty statements
                if (statements[i] == null)
                    continue;

                if (statements[i] is ControlToken control)
                    FetchBlock(control, statements, i + 1, start, index, startline);
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

        void FetchBlock(ControlToken control, List<IScriptToken> tokens, int index, int parserstart, int parserindex, int linenumber) {
            if (index >= tokens.Count)
                throw new ScriptParserException(parserstart, parserindex, linenumber, "Statement block expected");

            if (control is Switch @switch) {
                while (index < tokens.Count && (tokens[index] is Case || tokens[index] is Comment || tokens[index] is NewLine)) {
                    if (tokens[index] is Case @case)
                        FetchBlock(@case, tokens, index + 1, parserstart, parserindex, linenumber);

                    @switch.AddChild(tokens[index]);
                    tokens.RemoveAt(index);
                }
                return;
            }

            List<IScriptToken> metatokens=new List<IScriptToken>();
            IScriptToken block = FetchMetatokens(metatokens, tokens, index);

            if (block is ControlToken subcontrol)
                FetchBlock(subcontrol, tokens, index + 1, parserstart, parserindex, linenumber);

            if (metatokens.Count > 0) {
                if (block is StatementBlock statementblock) {
                    control.Body = new StatementBlock(metatokens.Concat(statementblock.Children).ToArray()) {
                        LineNumber = linenumber,
                        TextIndex = parserstart
                    };
                }
                else
                    control.Body = new StatementBlock(metatokens.Concat(new[] {block}).ToArray()) {
                        LineNumber = linenumber,
                        TextIndex = parserstart
                    };
            }
            else control.Body = block;

            tokens.RemoveAt(index);

            if (control is If @if) {
                block=FetchMetatokens(metatokens, tokens, index);
                if (block is Else @else) {
                    FetchBlock(@else, tokens, index + 1, parserstart, parserindex, linenumber);
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
                    FetchBlock(@catch, tokens, index + 1, parserstart, parserindex, linenumber);
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
            int linenumber = 1;
            StatementBlock block = ParseStatementBlock(null, data, ref index, ref linenumber, variablecontext, true);
            block.TextIndex = -1;
            block.LineNumber = -1;
            return new Script(block, variablecontext);
        }

        /// <inheritdoc />
        public Task<IScript> ParseAsync(string data, params Variable[] variables) {
            return Task.Run(() => Parse(data, variables));
        }

        /// <inheritdoc />
        public event Action<TokenType, int, int> TokenParsed;
    }
}