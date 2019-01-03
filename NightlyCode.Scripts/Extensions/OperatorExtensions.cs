using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for comparator tokens
    /// </summary>
    public static class OperatorExtensions {

        /// <summary>
        /// get number used to sort operator by priority
        /// </summary>
        /// <param name="operator">operator to sort</param>
        /// <returns>number used in sorting algorithm</returns>
        public static int GetOrderNumber(this Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Increment:
                case Operator.Decrement:
                    return 0;
                case Operator.Not:
                case Operator.Complement:
                case Operator.Negate:
                    return 1;
                case Operator.Division:
                case Operator.Multiplication:
                    return 2;
                case Operator.Modulo:
                    return 3;
                case Operator.Addition:
                case Operator.Subtraction:
                    return 4;
                case Operator.BitwiseAnd:
                    return 5;
                case Operator.BitwiseOr:
                    return 6;
                case Operator.BitwiseXor:
                    return 7;
                case Operator.ShiftLeft:
                case Operator.ShiftRight:
                    return 8;
                case Operator.Less:
                case Operator.LessOrEqual:
                case Operator.Greater:
                case Operator.GreaterOrEqual:
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.Matches:
                case Operator.NotMatches:
                    return 9;
                case Operator.LogicAnd:
                    return 10;
                case Operator.LogicOr:
                    return 11;
                case Operator.LogicXor:
                    return 12;
                case Operator.Assignment:
                case Operator.AddAssign:
                case Operator.SubAssign:
                case Operator.DivAssign:
                case Operator.MulAssign:
                case Operator.ModAssign:
                case Operator.ShlAssign:
                case Operator.ShrAssign:
                case Operator.AndAssign:
                case Operator.OrAssign:
                case Operator.XorAssign:
                    return 13;
                default:
                    throw new ScriptException("Unsupported operator");
            }
        }

        /// <summary>
        /// parses an operator from string
        /// </summary>
        /// <param name="data">string to parse</param>
        /// <returns>parsed operator</returns>
        public static Operator ParseOperator(this string data) {
            switch (data) {
            case "~":
                return Operator.Complement;
            case "!":
                return Operator.Not;
            case "=":
                return Operator.Assignment;
            case "==":
                return Operator.Equal;
            case "!=":
            case "<>":
                return Operator.NotEqual;
            case "<":
                return Operator.Less;
            case "<=":
                return Operator.LessOrEqual;
            case ">":
                return Operator.Greater;
            case ">=":
                return Operator.GreaterOrEqual;
            case "~~":
                return Operator.Matches;
            case "!~":
                return Operator.NotMatches;
            case "+":
                return Operator.Addition;
            case "-":
                return Operator.Subtraction;
            case "/":
                return Operator.Division;
            case "*":
                return Operator.Multiplication;
            case "%":
                return Operator.Modulo;
            case "&":
                return Operator.BitwiseAnd;
            case "|":
                return Operator.BitwiseOr;
            case "^":
                return Operator.BitwiseXor;
            case "<<":
                return Operator.ShiftLeft;
            case ">>":
                return Operator.ShiftRight;
            case "&&":
                return Operator.LogicAnd;
            case "||":
                return Operator.LogicOr;
            case "^^":
                return Operator.LogicXor;
            case "+=":
                return Operator.AddAssign;
            case "-=":
                return Operator.SubAssign;
            case "/=":
                return Operator.DivAssign;
            case "*=":
                return Operator.MulAssign;
            case "%=":
                return Operator.ModAssign;
            case "<<=":
                return Operator.ShlAssign;
            case ">>=":
                return Operator.ShrAssign;
            case "&=":
                return Operator.AndAssign;
            case "|=":
                return Operator.OrAssign;
            case "^=":
                return Operator.XorAssign;
            case "++":
                return Operator.Increment;
            case "--":
                return Operator.Decrement;
            default:
                throw new ScriptException($"Unsupported operator type '{data}'");
            }
        }

        public static string ToStringExt(this Operator @operator) {
            switch (@operator) {
            case Operator.Not:
                return "!";
            case Operator.Complement:
                return "~";
            case Operator.Modulo:
                return "%";
            case Operator.Division:
                return "/";
            case Operator.Multiplication:
                return "*";
            case Operator.Addition:
                return "+";
            case Operator.Subtraction:
                return "-";
            case Operator.Less:
                return "<";
            case Operator.LessOrEqual:
                return "<=";
            case Operator.Greater:
                return ">";
            case Operator.GreaterOrEqual:
                return ">=";
            case Operator.Equal:
                return "==";
            case Operator.NotEqual:
                return "!=";
            case Operator.BitwiseAnd:
                return "&";
            case Operator.BitwiseOr:
                return "|";
            case Operator.BitwiseXor:
                return "^";
            case Operator.LogicAnd:
                return "&&";
            case Operator.LogicOr:
                return "||";
            case Operator.LogicXor:
                return "^^";
            case Operator.Assignment:
                return "=";
            default:
                throw new ScriptException($"Unsupported operator {@operator}");
            }
        }
    }
}