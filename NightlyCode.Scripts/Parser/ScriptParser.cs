using System;
using System.Collections;
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
using NightlyCode.Scripting.Parser.Resolvers;
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Tokens;
using Switch = NightlyCode.Scripting.Control.Switch;

// TODO: the parent parameter is only used to determine whether to parse for a dictionary. There should be better ways to handle that

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser : IScriptParser {
        readonly OperatorTree operatortree = new OperatorTree();
        readonly Dictionary<string, Type> supportedcasts = new Dictionary<string, Type>();

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        public ScriptParser() {
            InitializeOperators();
            MethodCallResolver = new MethodResolver(Extensions);
            Types = new TypeProvider(MethodCallResolver);

            Types.AddType<List<object>>("list");
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
            Types.AddType<object>("object");
            Types.AddType<IDictionary>("dictionary");


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

        /// <summary>
        /// access to extensions available to script environment
        /// </summary>
        public IExtensionProvider Extensions { get; } = new ExtensionProvider();

        /// <summary>
        /// access to types which can be created using 'new' keyword
        /// </summary>
        public ITypeProvider Types { get; }

        /// <inheritdoc />
        public IImportProvider ImportProvider { get; set; }

        /// <summary>
        /// resolves methods to call for <see cref="ScriptMethod"/>s
        /// </summary>
        public IMethodResolver MethodCallResolver { get; }

        /// <summary>
        /// tree containing all supported operators
        /// </summary>
        public OperatorTree OperatorTree => operatortree;

        /// <summary>
        /// determines whether to allow single quotes to specify strings
        /// </summary>
        /// <remarks>
        /// single quotes are usually used for characters, but to allow behavior
        /// like js this option can be used
        /// </remarks>
        public bool AllowSingleQuotesForStrings { get; set; }

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
            while(index < data.Length && char.IsWhiteSpace(data[index])) {
                if(data[index] == '\n') {
                    ++newlinecount;
                    ++linenumber;
                }

                ++index;
            }

            return newlinecount;
        }

        IScriptToken ParseSingleControlParameter(ref string data, ref int index, ref int linenumber) {
            int start = index;
            int startline = linenumber;
            IScriptToken[] parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
            if(parameters.Length == 0)
                throw new ScriptParserException(start, index, startline, "A parameter was expected");
            if(parameters.Length > 1)
                throw new ScriptParserException(start, index, startline, "Only one parameter was expected");
            return parameters[0];
        }

        IScriptToken TryParseSingleParameter(ref string data, ref int index, ref int linenumber) {
            return TryParseControlParameters(ref data, ref index, ref linenumber).SingleOrDefault();
        }

        IScriptToken[] TryParseControlParameters(ref string data, ref int index, ref int linenumber) {
            SkipWhitespaces(data, ref index, ref linenumber);
            if(index >= data.Length || data[index] != '(')
                return new IScriptToken[0];
            return ParseControlParameters(null, ref data, ref index, ref linenumber);
        }

        IScriptToken[] ParseControlParameters(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            SkipWhitespaces(data, ref index, ref linenumber);
            if(index >= data.Length || data[index] != '(')
                throw new ScriptParserException(index, index, linenumber, "Expected parameters for control statement");

            ++index;
            return ParseParameters(parent, ref data, ref index, ref linenumber);
        }

        IScriptToken AnalyseToken(string token, ref string data, int start, ref int index, ref int linenumber, bool first) {
            if(token.Length == 0)
                throw new ScriptParserException(start, index, linenumber, "token expected");

            int startline = linenumber;

            if(char.IsDigit(token[0])) {
                try {
                    ScriptValue number = ParseNumber(token);
                    number.LineNumber = startline;
                    number.TextIndex = start;
                    number.TokenLength = index - start;
                    return number;
                }
                catch(Exception) {
                    return new ScriptValue(token) {
                        LineNumber = startline,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                }
            }

            if(first) {
                if(ControlTokensEnabled) {
                    IScriptToken[] parameters;
                    switch(token) {
                    case "if":
                        return new If(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "else":
                        return new Else {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "for":
                        parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
                        if(parameters.Length != 3)
                            throw new ScriptParserException(start, index, startline, "for loop needs 3 tokens as parameters");
                        return new For(parameters[0], parameters[1], parameters[2]) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "foreach":
                        parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
                        if(parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "foreach needs an iterator and a collection");
                        if(parameters.Length > 2)
                            throw new ScriptParserException(start, index, startline, "foreach must only specify an iterator and a collection");
                        if(!(parameters[0] is ScriptVariable foreachvariable))
                            throw new ScriptParserException(start, index, startline, "foreach iterator must be a variable");

                        Foreach foreachloop = new Foreach(foreachvariable, parameters[1]) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };

                        return foreachloop;
                    case "while":
                        return new While(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "switch":
                        return new Switch(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "case":
                        return new Case(ParseControlParameters(null, ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "default":
                        return new Case {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "return":
                        return new Return(TryParseSingleParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "throw":
                        parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
                        if(parameters.Length < 1)
                            throw new ScriptParserException(start, index, startline, "No exception message provided");
                        if(parameters.Length > 2)
                            throw new ScriptParserException(start, index, startline, "Too many arguments for exception throw");
                        return new Throw(parameters[0], parameters.Length > 1 ? parameters[1] : null) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "break":
                        return new Break(TryParseSingleParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "continue":
                        return new Continue(TryParseSingleParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "using":
                        return new Using(ParseControlParameters(null, ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "try":
                        return new Try {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "catch":
                        return new Catch {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "wait":
                        return new Wait(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "cast":
                        parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
                        if(parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "Type cast needs a value and a target type as parameters");
                        if(parameters.Length > 3)
                            throw new ScriptParserException(start, index, startline, "Too many parameters for a type cast");
                        if(!(parameters[1] is ScriptValue typevalue))
                            throw new ScriptParserException(start, index, startline, "Second parameter must be a constant specifying the type to cast to");

                        string casttypename = typevalue.Value.ToString();
                        if(casttypename.EndsWith("[]"))
                            casttypename = casttypename.Substring(0, casttypename.Length - 2);

                        if(!Types.HasType(casttypename) && !casttypename.Contains(','))
                            throw new ScriptParserException(start, index, startline, $"Unknown cast target type '{typevalue.Value}'");

                        return new ExpliciteTypeCast(Types, parameters[0], typevalue, parameters.Length == 3 ? parameters[2] : null) {
                            LineNumber = startline,
                            TextIndex = start,
                            TokenLength = index - start
                        };
                    case "parameter":
                        parameters = ParseControlParameters(null, ref data, ref index, ref linenumber);
                        if(parameters.Length < 2)
                            throw new ScriptParserException(start, index, startline, "Parameter specification needs a variable and a parameter type");
                        if(parameters.Length > 3)
                            throw new ScriptParserException(start, index, startline, "Too many parameters for a parameter specification");
                        if(!(parameters[0] is ScriptVariable))
                            throw new ScriptParserException(start, index, startline, "First argument must be a variable");

                        if (parameters[1] is ScriptValue parametertypename) {
                            
                            string typename = parametertypename.Value.ToString();
                            Type parameterstringtype = Types.DetermineType(typename);

                            return new ScriptParameter(Types, (ScriptVariable)parameters[0], new TypeToken(parameterstringtype), parameters.Length > 2 ? parameters[2] : null) {
                                LineNumber = startline,
                                TextIndex = start,
                                TokenLength = index - start
                            };
                        }

                        if (parameters[1] is TypeToken parametertype) {
                            return new ScriptParameter(Types, (ScriptVariable)parameters[0], parametertype, parameters.Length > 2 ? parameters[2] : null) {
                                LineNumber = startline,
                                TextIndex = start,
                                TokenLength = index - start
                            };
                        }

                        throw new ScriptParserException(start, index, startline, "Second argument must be a type name");
                    }
                }

                if(ImportsEnabled && token == "import") {
                    if(ImportProvider == null)
                        throw new ScriptParserException(start, index, startline, "Import statement is unavailable since no method resolver is set.");
                    return new Import(ImportProvider, ParseControlParameters(null, ref data, ref index, ref linenumber)) {
                        LineNumber = linenumber,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                }
            }

            //SkipWhitespaces(data, ref index);
            if(TypeCastsEnabled) {
                if(supportedcasts.ContainsKey(token) && index < data.Length && data[index] == '(') {
                    return new ImpliciteTypeCast(token, supportedcasts[token], ParseControlParameters(null, ref data, ref index, ref linenumber).Single()) {
                        LineNumber = startline,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                }
            }

            if(TypeInstanceProvidersEnabled && token == "new") {
                int typestart = index;
                string type = ParseName(data, ref index, ref linenumber);
                ITypeInstanceProvider typeprovider = Types.GetType(type.ToLower());
                if(typeprovider == null)
                    throw new ScriptParserException(typestart, index, startline, $"Unknown type {type}");

                // setting a parent to non null forces the parser to interpret '{' as statement block and not as dictionary
                SkipWhitespaces(data, ref index, ref linenumber);

                IScriptToken initializer = null;
                if (data[index] == '{') {
                    ++index;
                    initializer = ParseDictionary(null, ref data, ref index, ref linenumber);
                }

                IScriptToken[] constructorparameters = initializer != null ? new IScriptToken[0] : ParseControlParameters(null, ref data, ref index, ref linenumber);

                return new NewInstance(type, typeprovider.ProvidedType, typeprovider, constructorparameters) {
                    LineNumber = startline,
                    TextIndex = start,
                    TokenLength = index - start,
                    Initializer = initializer
                };
            }

            if(ControlTokensEnabled) {
                switch(token) {
                case "ref":
                    IAssignableToken reference = ParseSingleControlParameter(ref data, ref index, ref linenumber) as IAssignableToken;
                    if(reference == null)
                        throw new ScriptParserException(start, index, startline, $"ref can only be used with an {nameof(IAssignableToken)}");
                    return new Reference(reference) {
                        LineNumber = startline,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                case "await":
                    return new Await(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                        LineNumber = startline,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                case "typeof":
                    return new TypeOfToken(ParseSingleControlParameter(ref data, ref index, ref linenumber)) {
                        LineNumber = startline,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                }
            }

            switch(token) {
            case "true":
                return new ScriptValue(true) {
                    LineNumber = startline,
                    TextIndex = start,
                    TokenLength = index - start
                };
            case "false":
                return new ScriptValue(false) {
                    LineNumber = startline,
                    TextIndex = start,
                    TokenLength = index - start
                };
            case "null":
                return new ScriptValue(null) {
                    LineNumber = startline,
                    TextIndex = start,
                    TokenLength = index - start
                };
            }

            if (Types.HasType(token)) {
                if (index <= data.Length - 2 && data[index] == '[' && data[index + 1] == ']') {
                    index += 2;
                    return new TypeToken(Types.GetType(token).ProvidedType.MakeArrayType());
                }

                return new TypeToken(Types.GetType(token).ProvidedType);
            }

            return AnalyseVariable(token, data, ref index, start, startline);
        }

        ScriptVariable AnalyseVariable(string name, string code, ref int codeindex, int starttoken, int linenumber) {

            // check if an assignment is following
            /*if(!isresolved && (Peek(code, codeindex, out int foundindex) == '=' && Peek(code, foundindex + 1) != '=')) {
                resolved.Add(name);
                isresolved = true;
            }*/

            ScriptVariable variable = new ScriptVariable(name) {
                LineNumber = linenumber,
                TextIndex = starttoken,
                TokenLength = codeindex - starttoken,
                //IsResolved = isresolved
            };

            return variable;
        }

        IScriptToken ParseToken(ref string data, ref int index, ref int linenumber, bool startofstatement) {
            SkipWhitespaces(data, ref index, ref linenumber);
            int start = index;
            bool parsenumber = false;
            if(index < data.Length) {
                char character = data[index];
                if(char.IsDigit(character) || character == '.' || character == '-')
                    parsenumber = true;

                if(character == '"') {
                    ++index;
                    return ParseLiteral(data, ref index, linenumber);
                }

                if(character == '\'') {
                    ++index;
                    if (AllowSingleQuotesForStrings)
                        return ParseLiteralSingleQuotes(data, ref index, linenumber);
                    return ParseCharacter(data, ref index, linenumber);
                }
            }

            StringBuilder tokenname = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];

                if(char.IsLetterOrDigit(character) || character == '_' || (parsenumber && character == '.'))
                    tokenname.Append(character);
                else if(character == '"' || character == '\\') {
                    ++index;
                    tokenname.Append(ParseSpecialCharacter(data[index]));
                }
                else
                    return AnalyseToken(tokenname.ToString(), ref data, start, ref index, ref linenumber, startofstatement);
            }

            if(tokenname.Length > 0)
                return AnalyseToken(tokenname.ToString(), ref data, start, ref index, ref linenumber, startofstatement);
            return new ScriptValue(null);
        }

        string ParseName(string data, ref int index, ref int linenumber) {
            SkipWhitespaces(data, ref index, ref linenumber);
            StringBuilder tokenname = new StringBuilder();

            for(; index < data.Length; ++index) {
                char character = data[index];
                if(char.IsLetterOrDigit(character) || character == '_')
                    tokenname.Append(character);
                else
                    return tokenname.ToString();
            }

            if(tokenname.Length > 0)
                return tokenname.ToString();
            return null;
        }

        ScriptValue ParseNumber(string data) {
            string numberdata = data.ToLower();
            if(numberdata.StartsWith("0x")) {
                if(numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(0, numberdata.Length - 2), 16));
                if(numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(0, numberdata.Length - 1), 16));
                if(numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(0, numberdata.Length - 2), 16));
                if(numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(0, numberdata.Length - 1), 16));
                if(numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(0, numberdata.Length - 2), 16));
                if(numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(0, numberdata.Length - 1), 16));
                if(numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(0, numberdata.Length - 1), 16));
                return new ScriptValue(Convert.ToInt32(numberdata, 16));
            }

            if(numberdata.StartsWith("0o")) {
                if(numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(2, numberdata.Length - 4), 8));
                if(numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(2, numberdata.Length - 3), 8));
                if(numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(2, numberdata.Length - 4), 8));
                if(numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(2, numberdata.Length - 3), 8));
                if(numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(2, numberdata.Length - 4), 8));
                if(numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(2, numberdata.Length - 3), 8));
                if(numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(2, numberdata.Length - 3), 8));
                return new ScriptValue(Convert.ToInt32(numberdata.Substring(2), 8));
            }

            if(numberdata.StartsWith("0b")) {
                if(numberdata.EndsWith("ul"))
                    return new ScriptValue(Convert.ToUInt64(numberdata.Substring(2, numberdata.Length - 4), 2));
                if(numberdata.EndsWith("l"))
                    return new ScriptValue(Convert.ToInt64(numberdata.Substring(2, numberdata.Length - 3), 2));
                if(numberdata.EndsWith("us"))
                    return new ScriptValue(Convert.ToUInt16(numberdata.Substring(2, numberdata.Length - 4), 2));
                if(numberdata.EndsWith("s"))
                    return new ScriptValue(Convert.ToInt16(numberdata.Substring(2, numberdata.Length - 3), 2));
                if(numberdata.EndsWith("sb"))
                    return new ScriptValue(Convert.ToSByte(numberdata.Substring(2, numberdata.Length - 4), 2));
                if(numberdata.EndsWith("b"))
                    return new ScriptValue(Convert.ToByte(numberdata.Substring(2, numberdata.Length - 3), 2));
                if(numberdata.EndsWith("u"))
                    return new ScriptValue(Convert.ToUInt32(numberdata.Substring(2, numberdata.Length - 3), 2));
                return new ScriptValue(Convert.ToInt32(numberdata.Substring(2), 2));
            }

            if(numberdata.EndsWith("ul"))
                return new ScriptValue(Convert.ToUInt64(numberdata.Substring(0, numberdata.Length - 2), 10));
            if(numberdata.EndsWith("l"))
                return new ScriptValue(Convert.ToInt64(numberdata.Substring(0, numberdata.Length - 1), 10));
            if(numberdata.EndsWith("us"))
                return new ScriptValue(Convert.ToUInt16(numberdata.Substring(0, numberdata.Length - 2), 10));
            if(numberdata.EndsWith("s"))
                return new ScriptValue(Convert.ToInt16(numberdata.Substring(0, numberdata.Length - 1), 10));
            if(numberdata.EndsWith("sb"))
                return new ScriptValue(Convert.ToSByte(numberdata.Substring(0, numberdata.Length - 2), 10));
            if(numberdata.EndsWith("b"))
                return new ScriptValue(Convert.ToByte(numberdata.Substring(0, numberdata.Length - 1), 10));
            if(numberdata.EndsWith("u"))
                return new ScriptValue(Convert.ToUInt32(numberdata.Substring(0, numberdata.Length - 1), 10));

            int dotcount = numberdata.Count(digit => digit == '.');

            switch(dotcount) {
            case 0:
                return new ScriptValue(int.Parse(numberdata));
            case 1:
                if(numberdata.EndsWith("d"))
                    return new ScriptValue(decimal.Parse(numberdata.Substring(0, numberdata.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture));
                if(numberdata.EndsWith("f"))
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
            if(character == '\\') {
                ++index;
                character = ParseSpecialCharacter(data[index]);
            }

            ++index;
            if(data[index] != '\'')
                throw new ScriptParserException(start, index, linenumber, "Character literal not terminated");
            ++index;
            return new ScriptValue(character) {
                LineNumber = linenumber,
                TextIndex = start,
                TokenLength = index - start
            };
        }

        IScriptToken ParseLiteralSingleQuotes(string data, ref int index, int linenumber) {
            int start = index - 1;
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                switch(character) {
                    case '\'':
                        ++index;
                        return new ScriptValue(literal.ToString()) {
                            LineNumber = linenumber,
                            TextIndex = start,
                            TokenLength = index - start
                        };
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

        IScriptToken ParseLiteral(string data, ref int index, int linenumber) {
            int start = index - 1;
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                switch(character) {
                case '"':
                    ++index;
                    return new ScriptValue(literal.ToString()) {
                        LineNumber = linenumber,
                        TextIndex = start,
                        TokenLength = index - start
                    };
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

        IScriptToken ParseStringInterpolation(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            List<IScriptToken> tokens = new List<IScriptToken>();

            int start = index - 1;
            StringBuilder literal = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];

                switch(character) {
                case '"':
                    ++index;
                    tokens.Add(new ScriptValue(literal.ToString()) {
                        LineNumber = linenumber,
                        TextIndex = start,
                        TokenLength = index - start
                    });
                    return new StringInterpolation(tokens.ToArray());
                case '{':
                    ++index;
                    if(Peek(data, index) == '{') {
                        literal.Append('{');
                    }
                    else {
                        tokens.Add(new ScriptValue(literal.ToString()) {
                            LineNumber = linenumber,
                            TextIndex = start,
                            TokenLength = index - start
                        });
                        literal.Length = 0;
                        int blockstart = index - 1;
                        int blockline = linenumber;
                        StatementBlock block = ParseStatementBlock(parent, ref data, ref index, ref linenumber);
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

        bool CheckWhetherGenericArguments(ref string data, int index) {
            bool anycharacter = false;
            while (index < data.Length) {
                if (data[index] == '>') {
                    return anycharacter;
                }

                if (!char.IsLetterOrDigit(data[index]) && !char.IsWhiteSpace(data[index]) && data[index] != ',') 
                    return false;

                anycharacter = true;
                ++index;
            }

            return false;
        }

        IScriptToken ParseFormatString(ref string data, ref int index) {
            StringBuilder membername = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                if(char.IsLetterOrDigit(character) || character == '#' || character=='.') {
                    membername.Append(character);
                    continue;
                }

                break;
            }

            return new ScriptValue(membername.ToString());
        }
        
        IScriptToken ParseMember(IScriptToken host, ref string data, ref int index, ref int linenumber) {
            int start = index;
            StringBuilder membername = new StringBuilder();
            for(; index < data.Length; ++index) {
                char character = data[index];
                if(char.IsLetterOrDigit(character) || character == '_') {
                    membername.Append(character);
                    continue;
                }

                break;
            }

            IScriptToken[] genericparameters=null;
            while(index < data.Length) {
                switch(data[index]) {
                case '(':
                    ++index;
                    IScriptToken[] parameters = ParseTokenList(null, ref data, ref index, ref linenumber, true, ')');
                    return new ScriptMethod(MethodCallResolver, host, membername.ToString(), parameters, genericparameters) {
                        LineNumber = linenumber,
                        TextIndex = start,
                        TokenLength = index - start
                    };
                case '<':
                    if (CheckWhetherGenericArguments(ref data, index+1)) {
                        ++index;
                        genericparameters = ParseTokenList(null, ref data, ref index, ref linenumber, false, '>');
                        continue;
                    }

                    break;
                }

                break;
            }

            if(membername.Length > 0) {
                return new ScriptMember(host, membername.ToString()) {
                    LineNumber = linenumber,
                    TextIndex = start,
                    TokenLength = index - start
                };
            }

            throw new ScriptParserException(start, index, linenumber, "Member name expected");
        }

        IScriptToken[] ParseArray(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            int newlines = 0;
            SkipWhitespaces(data, ref index, ref linenumber);
            int start = index;
            List<IScriptToken> array = new List<IScriptToken>();
            for(; index < data.Length;) {
                char character = data[index];
                switch(character) {
                case '[':
                    ++index;
                    array.Add(new ScriptArray(ParseArray(null, ref data, ref index, ref linenumber)));
                    break;
                case ']':
                    ++index;
                    return array.ToArray();
                case ',':
                    ++index;
                    break;
                default:
                    IScriptToken element = Parse(parent, ref data, ref index, ref newlines, ref linenumber);
                    if(element == null)
                        throw new ScriptParserException(start, index, linenumber, "Invalid array specification");
                    array.Add(element);
                    break;
                }
            }

            throw new ScriptParserException(start, index, linenumber, "Array not terminated");
        }

        IScriptToken[] ParseTokenList(IScriptToken parent, ref string data, ref int index, ref int linenumber, bool scanforoperations, char terminator, char delimiter = ',') {
            int newlines = 0;
            int start = index;
            List<IScriptToken> parameters = new List<IScriptToken>();
            for(; index < data.Length;) {
                char character = data[index];
                if (character == terminator) {
                    ++index;
                    return parameters.ToArray();
                }

                if (character == delimiter) {
                    ++index;
                    continue;
                }

                if (scanforoperations)
                    parameters.Add(Parse(parent, ref data, ref index, ref newlines, ref linenumber));
                else parameters.Add(ParseSingle(parent, ref data, ref index, ref linenumber));
            }

            throw new ScriptParserException(start, index, linenumber, $"Expected '{terminator}' to end the parameter list.");
        }

        IScriptToken[] ParseParameters(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
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
                    parameters.Add(Parse(parent, ref data, ref index, ref newlines, ref linenumber));
                    break;
                }
            }

            throw new ScriptParserException(start, index, linenumber, "Parameter list not terminated");
        }


        IOperator ParseOperator(int parsestart, ref string data, ref int index, int linenumber) {
            int startoperator = index;

            OperatorNode node = operatortree.Root;
            bool done = false;
            while(index < data.Length) {
                char character = data[index];
                switch(character) {
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

                if(done)
                    break;

                OperatorNode current = node[character];
                if (current == null) {
                    // probably two operators chained without space (eg. x+-y)
                    --index;
                    break;
                }

                node = current;
                if(!node.HasChildren)
                    break;
            }

            if(node == null)
                throw new ScriptParserException(startoperator, index, linenumber, "Operator expected but nothing found");

            switch(node.Operator) {
            case Operator.Increment:
                if(index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                    return new Increment(true);
                else if(index < data.Length && !char.IsWhiteSpace(data[index]))
                    return new Increment(false);
                else
                    throw new ScriptParserException(startoperator, index, linenumber, "Increment without connected operand detected");
            case Operator.Decrement:
                if(index - parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                    return new Decrement(true);
                else if(index < data.Length && !char.IsWhiteSpace(data[index]))
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

            StringBuilder builder = new StringBuilder();
            while(index < data.Length - 1 && (data[index] != '*' || data[index + 1] != '/')) {
                if(data[index] == '\n')
                    ++linenumber;
                builder.Append(data[index++]);
            }

            index += 2;
            return new Comment(builder.ToString().Trim(), startlinenumber, startindex);
        }

        Comment ParseSingleLineComment(string data, ref int index, int linenumber) {
            int startindex = index;
            StringBuilder builder = new StringBuilder();
            while(index < data.Length && data[index] != '\n' && data[index] != '\r')
                builder.Append(data[index++]);

            ++index;
            return new Comment(builder.ToString(), linenumber, startindex, false);
        }

        IScriptToken ParseBlock(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            int newlines = 0;
            int start = index;
            IScriptToken block = Parse(parent, ref data, ref index, ref newlines, ref linenumber);
            while(index < data.Length) {
                if(char.IsWhiteSpace(data[index])) {
                    ++index;
                    continue;
                }

                break;
            }

            if(data[index] != ')')
                throw new ScriptParserException(start, index, linenumber, "Block not terminated");

            ++index;
            return new ArithmeticBlock(block);
        }

        char Peek(string data, int index) {
            return Peek(data, index, out int peekindex);
        }

        char Peek(string data, int index, out int peekindex) {
            while(index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;

            if(index < data.Length) {
                peekindex = index;
                return data[index];
            }

            peekindex = -1;
            return '\0';
        }

        IScriptToken ParseSingle(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            SkipWhitespaces(data, ref index, ref linenumber);
            int starttoken = index;
            IScriptToken token;

            switch (data[index]) {
            case '$':
                ++index;
                if(Peek(data, index) == '\"') {
                    ++index;
                    token= ParseStringInterpolation(parent, ref data, ref index, ref linenumber);
                }
                else {
                    string variablename = ParseName(data, ref index, ref linenumber);
                    ScriptVariable parsedvariable = AnalyseVariable(variablename, data, ref index, starttoken, linenumber);
                    token= parsedvariable;
                }

                break;
            default:
                token= ParseToken(ref data, ref index, ref linenumber, false);
                break;
            }

            while (data[index] == '.') {
                ++index;
                token = ParseMember(token, ref data, ref index, ref linenumber);
            }

            return token;
        }

        IScriptToken Parse(IScriptToken parent, ref string data, ref int index, ref int newlines, ref int linenumber, bool startofstatement = false, bool suppressformat=false) {
            List<IScriptToken> tokenlist = new List<IScriptToken>();
            List<OperatorIndex> indexlist = new List<OperatorIndex>();

            bool concat = true;
            bool done = false;
            int parsestart = index;
            SkipWhitespaces(data, ref index, ref linenumber);
            while(index < data.Length && !done) {
                int starttoken = index;
                switch(data[index]) {
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
                    if(data[index] == '/' && index < data.Length - 1 && (data[index + 1] == '/' || data[index + 1] == '*')) {
                        if(tokenlist.Count == 0) {
                            index += 2;
                            Comment comment;
                            if(data[index - 1] == '/')
                                comment = ParseSingleLineComment(data, ref index, linenumber);
                            else
                                comment = ParseMultiLineComment(data, ref index, ref linenumber);

                            newlines = 0;
                            if(MetatokensEnabled) {
                                comment.LineNumber = linenumber;
                                comment.TextIndex = parsestart;
                                comment.TokenLength = index - parsestart;
                                tokenlist.Add(comment);
                            }
                        }
                        done = true;
                        break;
                    }

                    IOperator @operator = ParseOperator(parsestart, ref data, ref index, linenumber);
                    if(@operator == null) {
                        // this most likely means the operator was actually a comment
                        done = true;
                        break;
                    }

                    if((@operator.Operator == Operator.Increment || @operator.Operator == Operator.Decrement) && !((IUnaryToken)@operator).IsPostToken && !concat) {
                        index -= 2;
                        done = true;
                        break;
                    }

                    if(@operator.Operator == Operator.Subtraction && (tokenlist.Count == 0 || tokenlist[tokenlist.Count - 1] is IOperator))
                        @operator = new Negate();

                    if(@operator is ScriptToken codetoken) {
                        codetoken.LineNumber = linenumber;
                        codetoken.TextIndex = starttoken;
                        codetoken.TokenLength = index - starttoken;
                    }

                    indexlist.Add(new OperatorIndex(tokenlist.Count, @operator));
                    tokenlist.Add(@operator);
                    if(!(@operator is IUnaryToken))
                        concat = true;
                    break;
                case ':':
                    if (suppressformat) {
                        // in a context where formatting is not allowed (like dictionary keys)
                        done = true;
                        break;
                    }
                    
                    if (tokenlist.Count == 0)
                        throw new ScriptParserException(starttoken, index, linenumber, "Formatter needs a token to format");
                    if (tokenlist.Last() is IOperator)
                        throw new ScriptParserException(starttoken, index, linenumber, "Unable to format operators");

                    ++index;
                    IScriptToken format = ParseFormatString(ref data, ref index);
                    tokenlist[tokenlist.Count - 1] = new ScriptMethod(MethodCallResolver, tokenlist.Last(), "ToString", new[] {format, new ScriptValue(CultureInfo.InvariantCulture)});
                    concat = false;
                    break;
                case '.':
                    ++index;
                    tokenlist[tokenlist.Count - 1] = ParseMember(tokenlist[tokenlist.Count - 1], ref data, ref index, ref linenumber);
                    concat = false;
                    break;
                case '$':
                    if(!concat) {
                        done = true;
                        break;
                    }

                    ++index;
                    if(Peek(data, index) == '\"') {
                        ++index;
                        tokenlist.Add(ParseStringInterpolation(parent, ref data, ref index, ref linenumber));
                    }
                    else {
                        string variablename = ParseName(data, ref index, ref linenumber);
                        ScriptVariable parsedvariable = AnalyseVariable(variablename, data, ref index, starttoken, linenumber);
                        tokenlist.Add(parsedvariable);
                    }
                    concat = false;
                    break;
                case '(':
                    ++index;
                    if(tokenlist.Count > 0) {
                        if (tokenlist.Last() is ScriptVariable variable) {
                            int methodline = linenumber;
                            IScriptToken[] parameters = ParseTokenList(null, ref data, ref index, ref linenumber, true, ')');
                            tokenlist[tokenlist.Count - 1] = new ScriptMethod(MethodCallResolver, variable, "invoke", parameters) {
                                LineNumber = methodline,
                                TextIndex = starttoken,
                                TokenLength = index - starttoken
                            };
                        }
                        else if (tokenlist.Last() is IOperator) {
                            tokenlist.Add(ParseBlock(parent, ref data, ref index, ref linenumber));
                            concat = false;
                        }
                        else throw new ScriptParserException(starttoken, index, linenumber, $"'{tokenlist.Last()}' is not a method");
                    }
                    else {
                        tokenlist.Add(ParseBlock(parent, ref data, ref index, ref linenumber));
                        concat = false;
                    }
                    break;
                case '[':
                    ++index;
                    if(tokenlist.Count == 0 || tokenlist[tokenlist.Count - 1] is IOperator)
                        tokenlist.Add(new ScriptArray(ParseArray(parent, ref data, ref index, ref linenumber)));
                    else {
                        int line = linenumber;
                        tokenlist[tokenlist.Count - 1] = new ScriptIndexer(tokenlist.Last(), ParseParameters(parent, ref data, ref index, ref linenumber)) {
                            LineNumber = line,
                            TextIndex = starttoken
                        };
                    }
                    concat = false;
                    break;
                case '{':
                    ++index;
                    IOperator op = tokenlist.LastOrDefault() as IOperator;
                    if((parent is null || op != null) && op?.Operator != Operator.Lambda) {
                        tokenlist.Add(ParseDictionary(/*parent*/null, ref data, ref index, ref linenumber));
                    }
                    else {
                        StatementBlock statementblock = ParseStatementBlock(parent, ref data, ref index, ref linenumber);
                        statementblock.TextIndex = starttoken;
                        statementblock.LineNumber = linenumber;

                        if(!statementblock.Children.Any())
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
                    if(tokenlist.Count == 0)
                        tokenlist.Add(new StatementBlock(new IScriptToken[0]) {
                            LineNumber = linenumber,
                            TextIndex = starttoken,
                            TokenLength = index - starttoken
                        });
                    ++index;
                    done = true;
                    break;
                default:
                    if(!concat) {
                        done = true;
                        break;
                    }

                    IScriptToken token = ParseToken(ref data, ref index, ref linenumber, startofstatement);
                    if(token is IStatementContainer || token is ParserToken || token is Return)
                        return token;
                    tokenlist.Add(token);
                    concat = false;
                    break;
                }
                if(index == starttoken && !done)
                    throw new ScriptParserException(starttoken, index, linenumber, "Unable to parse code");

                if(!done)
                    newlines = SkipWhitespaces(data, ref index, ref linenumber);
            }

            if(tokenlist.Count > 1) {
                indexlist.Sort((lhs, rhs) =>
                    lhs.Token.Operator.GetOrderNumber().CompareTo(rhs.Token.Operator.GetOrderNumber()));

                for(int i = 0; i < indexlist.Count; ++i) {
                    OperatorIndex operatorindex = indexlist[i];
                    if(operatorindex.Token is IUnaryToken unary) {
                        if(unary.IsPostToken) {
                            if(operatorindex.Index == 0)
                                // TODO: provide indices if this can actually happen
                                throw new ScriptParserException(parsestart, index, linenumber, "Posttoken at beginning of tokenlist detected");

                            unary.Operand = tokenlist[operatorindex.Index - 1];
                            tokenlist.RemoveAt(operatorindex.Index - 1);
                        }
                        else {
                            unary.Operand = tokenlist[operatorindex.Index + 1];
                            tokenlist.RemoveAt(operatorindex.Index + 1);
                            --operatorindex.Index;
                        }

                        for(int k = i; k < indexlist.Count; ++k)
                            if(indexlist[k].Index > operatorindex.Index)
                                --indexlist[k].Index;
                    }
                    else if(operatorindex.Token is IBinaryToken binary) {
                        if(operatorindex.Index == 0)
                            throw new ScriptParserException(parsestart, index, linenumber, "Left hand side operand expected");
                        if(operatorindex.Index >= tokenlist.Count - 1)
                            throw new ScriptParserException(parsestart, index, linenumber, "Right hand side operand expected");

                        binary.Lhs = tokenlist[operatorindex.Index - 1];
                        binary.Rhs = tokenlist[operatorindex.Index + 1];
                        tokenlist.RemoveAt(operatorindex.Index + 1);
                        tokenlist.RemoveAt(operatorindex.Index - 1);
                        --operatorindex.Index;
                        for(int k = i; k < indexlist.Count; ++k)
                            if(indexlist[k].Index > operatorindex.Index)
                                indexlist[k].Index = indexlist[k].Index - 2;
                    }
                }
            }

            // there has to be a single statement or nothing at this point
            return tokenlist.SingleOrDefault();
        }

        IScriptToken ParseDictionary(IScriptToken parent, ref string data, ref int index, ref int linenumber) {
            int newlines = 0;
            DictionaryToken dictionary = new DictionaryToken();
            while(Peek(data, index) != '}') {
                IScriptToken key = Parse(parent, ref data, ref index, ref newlines, ref linenumber, false, true);
                while(key == null || key is Comment) {
                    if(Peek(data, index) == '}') {
                        key = null;
                        break;
                    }

                    key = Parse(parent, ref data, ref index, ref newlines, ref linenumber, false, true);
                }

                if(key == null)
                    break;

                IScriptToken value = null;
                if(Peek(data, index) == ':') {
                    ++index;
                    value = Parse(parent, ref data, ref index, ref newlines, ref linenumber);
                }

                dictionary.Add(key, value);
                if(Peek(data, index) == ',')
                    ++index;
            }
            // eat '{'
            ++index;
            return dictionary;
        }

        StatementBlock ParseStatementBlock(IScriptToken parent, ref string data, ref int index, ref int linenumber, bool methodblock = false) {
            List<IScriptToken> statements = new List<IScriptToken>();

            if(MetatokensEnabled && SkipWhitespaces(data, ref index, ref linenumber) > 1)
                statements.Add(new NewLine());
            else
                SkipWhitespaces(data, ref index, ref linenumber);


            int start = index;
            int startline = linenumber;
            bool terminated = false;
            int newlinesafterlasttoken = 0;
            while(index < data.Length) {
                int newlines = 0;
                if(index < data.Length && data[index] == '}') {
                    ++index;
                    terminated = true;
                    break;
                }

                int tokenstart = index;

                IScriptToken token;
                try {
                    token = Parse(statements.LastOrDefault() is IStatementContainer control ? control : parent, ref data, ref index, ref newlines, ref linenumber, true);

                    if(!(token is Comment))
                        newlinesafterlasttoken = newlines;
                }
                catch(ScriptParserException e) {
                    if(e.StartIndex == -1 || e.EndIndex == -1)
                        throw new ScriptParserException(tokenstart, index, startline, e.Message, e);
                    throw;
                }

                if(index == tokenstart)
                    throw new ScriptParserException(tokenstart, index, linenumber, "Unable to parse code");

                if(token != null) {
                    if(token is Comment comment && newlinesafterlasttoken == 0 && statements.Count > 0 && !(statements.Last() is NewLine || statements.Last() is Comment))
                        comment.IsPostComment = true;

                    statements.Add(token);
                }

                if(MetatokensEnabled && newlines > 1)
                    statements.Add(new NewLine());

                newlines = SkipWhitespaces(data, ref index, ref linenumber);
                if(newlinesafterlasttoken == 0)
                    newlinesafterlasttoken = newlines;
                if(MetatokensEnabled && newlines > 1 && !(statements.Last() is NewLine))
                    statements.Add(new NewLine());
            }

            if(!terminated && !methodblock)
                throw new ScriptParserException(start, index, linenumber, "Unterminated Statementblock");

            if(statements.Count <= 1)
                return new StatementBlock(statements.ToArray(), methodblock) {
                    LineNumber = startline,
                    TextIndex = start,
                    TokenLength = index - start
                };


            for(int i = 0; i < statements.Count; ++i) {
                // skip empty statements
                if(statements[i] == null)
                    continue;

                if(statements[i] is ControlToken control)
                    FetchBlock(control, statements, i + 1, start, index, startline);
            }

            return new StatementBlock(statements.ToArray(), methodblock) {
                LineNumber = startline,
                TextIndex = start,
                TokenLength = index - start
            };
        }

        IScriptToken FetchMetatokens(List<IScriptToken> result, List<IScriptToken> tokens, int index) {
            result.Clear();
            if(index >= tokens.Count)
                return null;
            IScriptToken block = tokens[index];
            while(block is Comment || block is NewLine) {
                result.Add(block);
                tokens.RemoveAt(index);
                if(index >= tokens.Count)
                    return null;

                block = tokens[index];
            }

            return block;
        }

        void FetchBlock(ControlToken control, List<IScriptToken> tokens, int index, int parserstart, int parserindex, int linenumber) {
            if(index >= tokens.Count)
                throw new ScriptParserException(parserstart, parserindex, linenumber, "Statement block expected");

            if(control is Switch @switch) {
                while(index < tokens.Count && (tokens[index] is Case || tokens[index] is Comment || tokens[index] is NewLine)) {
                    if(tokens[index] is Case @case)
                        FetchBlock(@case, tokens, index + 1, parserstart, parserindex, linenumber);

                    @switch.AddChild(tokens[index]);
                    tokens.RemoveAt(index);
                }
                return;
            }

            List<IScriptToken> metatokens = new List<IScriptToken>();
            IScriptToken block = FetchMetatokens(metatokens, tokens, index);

            if(block is ControlToken subcontrol)
                FetchBlock(subcontrol, tokens, index + 1, parserstart, parserindex, linenumber);

            if(metatokens.Count > 0) {
                if(block is StatementBlock statementblock) {
                    control.Body = new StatementBlock(metatokens.Concat(statementblock.Children).ToArray()) {
                        LineNumber = linenumber,
                        TextIndex = parserstart,
                        TokenLength = index - parserstart
                    };
                }
                else
                    control.Body = new StatementBlock(metatokens.Concat(new[] { block }).ToArray()) {
                        LineNumber = linenumber,
                        TextIndex = parserstart,
                        TokenLength = index - parserstart
                    };
            }
            else
                control.Body = block;

            tokens.RemoveAt(index);

            if(control is If @if) {
                block = FetchMetatokens(metatokens, tokens, index);
                if(block is Else @else) {
                    FetchBlock(@else, tokens, index + 1, parserstart, parserindex, linenumber);
                    @if.Else = @else;
                    tokens.RemoveAt(index);
                }
                else {
                    for(int i = metatokens.Count - 1; i >= 0; --i)
                        tokens.Insert(index, metatokens[i]);
                }
            }
            else if(control is Try @try) {
                block = FetchMetatokens(metatokens, tokens, index);
                if(block is Catch @catch) {
                    FetchBlock(@catch, tokens, index + 1, parserstart, parserindex, linenumber);
                    @try.Catch = @catch;
                    tokens.RemoveAt(index);
                }
                else {
                    for(int i = metatokens.Count - 1; i >= 0; --i)
                        tokens.Insert(index, metatokens[i]);
                }
            }
        }

        /// <inheritdoc />
        public IScript Parse(string data) {
            int index = 0;
            int linenumber = 1;
            StatementBlock block = ParseStatementBlock(null, ref data, ref index, ref linenumber, true);
            block.TextIndex = -1;
            block.LineNumber = -1;
            block.TokenLength = -1;
            return new Script(block, Types);
        }

        /// <inheritdoc />
        public Task<IScript> ParseAsync(string data) {
            return Task.Run(() => Parse(data));
        }
    }
}