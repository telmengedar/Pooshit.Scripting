using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// extensions for comparator tokens
    /// </summary>
    static class OperatorExtensions {

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
                case Operator.RolLeft:
                case Operator.RolRight:
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
                    // this is only thrown when the script parser logic is actually broken so no indices need to be provided
                    throw new ScriptParserException(-1,-1,"Unsupported operator");
            }
        }
    }
}