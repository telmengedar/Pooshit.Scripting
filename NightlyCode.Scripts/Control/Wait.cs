using System;
using System.Threading;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token which waits a specified timespan
    /// </summary>
    public class Wait : ScriptToken {
        readonly IScriptToken timetoken;

        /// <summary>
        /// creates a new <see cref="Wait"/>
        /// </summary>
        /// <param name="parameters">parameters for wait call (should be one parameter indicating the time to wait)</param>
        public Wait(IScriptToken[] parameters) {
            if (parameters.Length == 0)
                throw new ScriptParserException("wait needs a time to wait as argument");

            if (parameters.Length > 1)
                throw new ScriptParserException("too many arguments for wait call");

            timetoken = parameters[0];
        }

        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments) {
            object timeargument = timetoken.Execute(variables, arguments);
            if (timeargument == null)
                throw new ScriptRuntimeException("Specified waiting time was null");
            if (timeargument is TimeSpan timespan)
                Thread.Sleep(timespan);
            else {
                switch (Type.GetTypeCode(timeargument.GetType())) {
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    Thread.Sleep(Converter.Convert<int>(timeargument));
                    break;
                case TypeCode.String:
                    if(((string)timeargument).Contains(":"))
                        Thread.Sleep(Converter.Convert<TimeSpan>(timeargument));
                    else
                        Thread.Sleep(Converter.Convert<int>(timeargument));
                    break;
                default:
                    throw new ScriptRuntimeException("argument to wait can not be converted to a valid time");
                }
            }
            return null;
        }
    }
}