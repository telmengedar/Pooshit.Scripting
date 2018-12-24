using System;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for comparator tokens
    /// </summary>
    public static class OperatorExtensions {

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