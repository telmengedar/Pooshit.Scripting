using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Operations.Logic;
using NightlyCode.Scripting.Operations.OpAssign;
using NightlyCode.Scripting.Operations.Unary;
using NightlyCode.Scripting.Operations.Values;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting {

    /// <summary>
    /// parses scripts from string data
    /// </summary>
    public class ScriptParser {
        readonly VariableContext globalvariables = new VariableContext();

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        public ScriptParser(params Variable[] variables)
        : this(new ExtensionProvider(), variables)
        {
        }

        /// <summary>
        /// creates a new <see cref="ScriptParser"/>
        /// </summary>
        /// <param name="extensionprovider">pool containing hosts for members</param>
        /// <param name="variables">global variables of script parser</param>
        public ScriptParser(IExtensionProvider extensionprovider, params Variable[] variables) {
            foreach (Variable variable in variables)
                globalvariables[variable.Name] = variable.Value;
            globalvariables.IsReadOnly = true;
            Extensions = extensionprovider;
        }

        /// <summary>
        /// access to extensions available to script environment
        /// </summary>
        public IExtensionProvider Extensions { get; }

        void SkipWhitespaces(string data, ref int index) {
            while (index < data.Length && char.IsWhiteSpace(data[index]))
                ++index;
        }

        IScriptToken[] ParseControlParameters(string data, ref int index, IVariableContext variables) {
            SkipWhitespaces(data, ref index);
            if (data[index] != '(')
                throw new ScriptException("Expected parameters for control statement");
            ++index;
            return ParseParameters(data, ref index, variables);
        }

        IScriptToken AnalyseToken(string token, string data, ref int index, IVariableContext variables, bool first) {
            if (first) {
                switch (token) {
                case "if":
                    return new If(ParseControlParameters(data, ref index, variables));
                case "else":
                    return new Else();
                case "for":
                    return new For(ParseControlParameters(data, ref index, variables));
                case "foreach":
                    return new Foreach(ParseControlParameters(data, ref index, variables));
                case "while":
                    return new While(ParseControlParameters(data, ref index, variables));
                case "switch":
                    return new Switch(ParseControlParameters(data, ref index, variables));
                case "case":
                    return new Case(ParseControlParameters(data, ref index, variables));
                case "default":
                    return new Case();
                case "return":
                    return new Return(Parse(data, ref index, variables));
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
                    if (variables.ContainsVariable(token))
                        return new ScriptVariable(variables, token);
                    return new ScriptValue(token);
            }
        }

        IScriptToken ParseToken(string data, ref int index, IVariableContext variables, bool startofstatement) {
            SkipWhitespaces(data, ref index);

            if (index < data.Length) {
                char character = data[index];
                if (char.IsDigit(character) || character == '.' || character == '-')
                    return ParseNumber(data, ref index);
                if (character == '"')
                {
                    ++index;
                    return ParseLiteral(data, ref index);
                }
            }

            StringBuilder tokenname = new StringBuilder();
            for (; index < data.Length; ++index) {
                char character = data[index];

                if (char.IsLetterOrDigit(character) || character == '_')
                    tokenname.Append(character);
                else if (character == '"' || character == '\\') {
                    ++index;
                    tokenname.Append(ParseSpecialCharacter(data[index]));
                }
                else return AnalyseToken(tokenname.ToString().TrimEnd(' '), data, ref index, variables, startofstatement);
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
                    return new ScriptValue(int.Parse(literal.ToString()));
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

        IScriptToken ParseMember(IScriptToken host, string data, ref int index, IVariableContext variables) {
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
                    return new ScriptMethod(Extensions, host, membername.ToString(), ParseParameters(data, ref index, variables));
                }
            }

            if (membername.Length > 0)
                return new ScriptMember(host, membername.ToString());
            throw new ScriptException("Member name expected");
        }

        IScriptToken[] ParseArray(string data, ref int index, IVariableContext variables) {
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
                    array.Add(Parse(data, ref index, variables));
                    break;
                }
            }

            throw new ScriptException("Array not terminated");
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

            throw new ScriptException("Parameter list not terminated");
        }


        IOperator ParseOperator(int parsestart,string data, ref int index) {
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

            string operatorstring = token.ToString();
            switch (operatorstring)
            {
                case "++":
                case "--":
                    if (index-parsestart >= 3 && !char.IsWhiteSpace(data[index - 3]))
                        return new Postcrement(operatorstring == "++" ? 1 : -1);
                    else if (index < data.Length && !char.IsWhiteSpace(data[index]))
                        return new Precrement(operatorstring == "++" ? 1 : -1);
                    else
                        throw new ScriptException("Increment without connected operand detected");
            }

            Operator @operator = operatorstring.ParseOperator();
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
                case Operator.ShiftLeft:
                    return new ShiftLeft();
                case Operator.ShiftRight:
                    return new ShiftRight();
                case Operator.Not:
                    return new Not();
                case Operator.Complement:
                    return new Complement();
                case Operator.Assignment:
                    return new ScriptAssignment();
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
            return new ArithmeticBlock(block);
        }

        IScriptToken Parse(string data, ref int index, IVariableContext variables, bool startofstatement=false) {
            List<IScriptToken> tokenlist=new List<IScriptToken>();
            List<OperatorIndex> indexlist=new List<OperatorIndex>();

            bool concat = true;
            bool done = false;
            int parsestart = index;

            SkipWhitespaces(data, ref index);
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
                        IOperator @operator = ParseOperator(parsestart, data, ref index);
                        if (@operator.Operator == Operator.Precrement && !concat)
                        {
                            index -= 2;
                            done = true;
                            break;
                        }

                        if (@operator.Operator == Operator.Subtraction && (tokenlist.Count == 0 || tokenlist[tokenlist.Count - 1] is IOperator))
                            @operator = new Negate();

                        indexlist.Add(new OperatorIndex(tokenlist.Count, @operator));
                        tokenlist.Add(@operator);
                        if (@operator.Operator != Operator.Postcrement && @operator.Operator != Operator.Precrement && @operator.Operator != Operator.Negate)
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
                                throw new ScriptException("Posttoken at beginning of tokenlist detected");

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

            return tokenlist.Single();
        }

        IScriptToken ParseStatementBlock(string data, ref int index, IVariableContext variables, bool methodblock=false) {
            variables = new VariableContext(variables);
            List<IScriptToken> statements = new List<IScriptToken>();
            while (index < data.Length) {
                SkipWhitespaces(data, ref index);
                if (index < data.Length && data[index] == '}') {
                    ++index;
                    break;
                }

                IScriptToken token = Parse(data, ref index, variables, true);
                if (token != null)
                    statements.Add(token);
            }

            if (statements.Count == 0)
                return null;
            if (statements.Count == 1) {
                return statements.Single();
            }


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
                    else if(control is Else @else)
                    {
                        If @if = statements[i - 1] as If;
                        if (@if == null)
                            throw new ScriptException("Else without connected if statement found");
                        @if.Else = @else.Body;
                        statements.RemoveAt(i);
                        --i;
                    }
                }
            }

            return new StatementBlock(variables, statements.ToArray(), methodblock);
        }

        /// <summary>
        /// parses a script for execution
        /// </summary>
        /// <param name="data">data to parse</param>
        /// <param name="variables">variables valid for this script (flagged as read-only)</param>
        /// <returns>script which can get executed</returns>
        public IScriptToken Parse(string data, params Variable[] variables) {
            VariableContext variablecontext = new VariableContext(globalvariables, variables) {IsReadOnly = true};

            int index = 0;
            return ParseStatementBlock(data, ref index, variablecontext, true);
        }
    }
}