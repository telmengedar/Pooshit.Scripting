using System;
using System.Threading;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;
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
        /// <param name="time">time to wait</param>
        public Wait(IScriptToken time) {
            timetoken = time;
        }

        /// <summary>
        /// time to wait
        /// </summary>
        public IScriptToken Time => timetoken;

        /// <inheritdoc />
        public override string Literal => "wait";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            object timeargument = timetoken.Execute(context);
            if (timeargument == null)
                throw new ScriptRuntimeException("Specified waiting time was null", this);

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
                    throw new ScriptRuntimeException("argument to wait can not be converted to a valid time", this);
                }
            }
            return null;
        }
    }
}