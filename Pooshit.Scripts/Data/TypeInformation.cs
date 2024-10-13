using System;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Data {

    /// <summary>
    /// type related information methods
    /// </summary>
    public static class TypeInformation {

        /// <summary>
        /// get precision value of a type
        /// </summary>
        /// <param name="type">type to analyse</param>
        /// <returns>precision value used to determine conversion target</returns>
        public static int GetTypeValue(Type type) {
            switch (Type.GetTypeCode(type)) {
            case TypeCode.Boolean:
                return 0;
            case TypeCode.Byte:
                return 1;
            case TypeCode.Char:
                return 10;
            case TypeCode.DateTime:
                return -1;
            case TypeCode.DBNull:
                return -1;
            case TypeCode.Decimal:
                return 7;
            case TypeCode.Double:
                return 6;
            case TypeCode.Empty:
                return -1;
            case TypeCode.Int16:
                return 2;
            case TypeCode.Int32:
                return 3;
            case TypeCode.Int64:
                return 4;
            case TypeCode.Object:
                return -1;
            case TypeCode.SByte:
                return 1;
            case TypeCode.Single:
                return 5;
            case TypeCode.String:
                return 11;
            case TypeCode.UInt16:
                return 2;
            case TypeCode.UInt32:
                return 3;
            case TypeCode.UInt64:
                return 4;
            default:
                return 9;
            }
        }

        /// <summary>
        /// determines the type to use for operations
        /// </summary>
        /// <param name="lhs">left hand type</param>
        /// <param name="rhs">right hand type</param>
        /// <returns>type to use for operations</returns>
        public static Type DetermineTargetType(Type lhs, Type rhs) {
            int lhsvalue = GetTypeValue(lhs);
            int rhsvalue = GetTypeValue(rhs);
            if (lhsvalue == -1 || rhsvalue == -1)
                return typeof(object);

            return rhsvalue > lhsvalue ? rhs : lhs;
        }

        /// <summary>
        /// converts operators for an operation
        /// </summary>
        /// <param name="lhs">left hand side operand</param>
        /// <param name="rhs">right hand side operands</param>
        public static void ConvertOperands(ref object lhs, ref object rhs) {
            if (lhs != null && rhs != null && lhs.GetType() != rhs.GetType() /*&& CanConvert(lhs.GetType()) && CanConvert(rhs.GetType())*/) {
                Type targettype = DetermineTargetType(lhs.GetType(), rhs.GetType());
                if (targettype == typeof(object))
                    return;

                lhs = Converter.Convert(lhs, targettype);
                rhs = Converter.Convert(rhs, targettype);
            }
        }
    }
}