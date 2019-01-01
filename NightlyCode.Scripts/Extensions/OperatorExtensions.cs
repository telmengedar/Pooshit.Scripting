using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Operations.Values;

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
                case Operator.Not:
                case Operator.Negate:
                    return 0;
                case Operator.Division:
                case Operator.Multiplication:
                    return 1;
                case Operator.Modulo:
                    return 2;
                case Operator.Addition:
                case Operator.Subtraction:
                    return 3;
                case Operator.Less:
                case Operator.LessOrEqual:
                case Operator.Greater:
                case Operator.GreaterOrEqual:
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.Matches:
                case Operator.NotMatches:
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
                case Operator.LogicAnd:
                    return 9;
                case Operator.LogicOr:
                    return 10;
                case Operator.LogicXor:
                    return 11;
                case Operator.Assignment:
                    return 12;
                default:
                    throw new ScriptException("Unsupported operator");
            }
        }

        public static Operator ParseOperator(this string data) {
            switch (data) {
            case "~":
                return Operator.Negate;
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
            default:
                throw new ScriptException($"Unsupported operator type '{data}'");
            }
        }

        public static string ToStringExt(this Operator @operator) {
            switch (@operator) {
            case Operator.Not:
                return "!";
            case Operator.Negate:
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