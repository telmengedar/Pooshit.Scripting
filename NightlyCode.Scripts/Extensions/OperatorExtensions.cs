using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;

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
        public static int GetOrderNumber(this Operator @operator) {
            switch(@operator) {
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
                throw new ScriptParserException(-1, -1, -1, "Unsupported operator");
            }
        }

        /// <summary>
        /// determines whether an arbitrary value is equal to another
        /// </summary>
        /// <param name="value">value to compare</param>
        /// <param name="rhs">value to compare this value to for equality</param>
        /// <returns>true when the two values are considered to be equal, false otherwise</returns>
        public static bool IsEqual(this object value, object rhs) {
            if(value == null)
                return rhs == null;
            if(rhs == null)
                return false;

            if(value.GetType() == rhs.GetType())
                return value.Equals(rhs);

            return value.Equals(Converter.Convert(rhs, value.GetType(), true));
        }
    }
}