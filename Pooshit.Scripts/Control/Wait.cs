using System;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// token which waits a specified timespan
    /// </summary>
    public class Wait : ScriptToken {

        /// <summary>
        /// largest single wait chunk used by <see cref="WaitInterruptible"/>
        /// </summary>
        /// <remarks>
        /// <see cref="System.Threading.WaitHandle.WaitOne(TimeSpan)"/> and
        /// <see cref="System.Threading.CancellationTokenSource.CancelAfter(TimeSpan)"/> reject a
        /// millisecond value of exactly <see cref="int.MaxValue"/> (reserved as a sentinel on some
        /// overloads) and treat -1 as "infinite", so a duration longer than this is chunked and re-armed
        /// until the requested time has fully elapsed.
        /// </remarks>
        static readonly TimeSpan maxchunk = TimeSpan.FromMilliseconds(int.MaxValue - 1);

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

        TimeSpan ConvertDuration(object timeargument) {
            if (timeargument is TimeSpan timespan)
                return timespan;

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
                return TimeSpan.FromMilliseconds(Converter.Convert<int>(timeargument));
            case TypeCode.String:
                return ((string)timeargument).Contains(":")
                           ? Converter.Convert<TimeSpan>(timeargument)
                           : TimeSpan.FromMilliseconds(Converter.Convert<int>(timeargument));
            default:
                throw new ScriptRuntimeException("argument to wait can not be converted to a valid time", this);
            }
        }

        /// <summary>
        /// blocks interruptibly for <paramref name="duration"/>, returning early and throwing when the
        /// context's cancellation token is signalled mid-wait. A never-signalled handle (<see cref="System.Threading.CancellationToken.None"/>)
        /// degrades this to an ordinary, uninterruptible sleep, identical to the previous behavior for
        /// hosts that never cancel
        /// </summary>
        /// <param name="context">execution context providing the cancellation token to observe</param>
        /// <param name="duration">duration to wait</param>
        void WaitInterruptible(ScriptContext context, TimeSpan duration) {
            TimeSpan remaining = duration;
            while (remaining > TimeSpan.Zero) {
                TimeSpan chunk = remaining > maxchunk ? maxchunk : remaining;
                if (context.CancellationToken.WaitHandle.WaitOne(chunk)) {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    return;
                }

                remaining -= chunk;
            }
        }

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            object timeargument = timetoken.Execute(context);
            if (timeargument == null)
                throw new ScriptRuntimeException("Specified waiting time was null", this);

            WaitInterruptible(context, ConvertDuration(timeargument));
            return null;
        }
    }
}
