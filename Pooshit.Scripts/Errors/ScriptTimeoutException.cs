using System;

namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// thrown when a script exceeds the configured execution timeout (<see cref="ScriptLimits.Timeout"/>)
    /// without the caller having requested cancellation itself; distinguishes "the script misbehaved"
    /// from "the host asked to stop" (<see cref="OperationCanceledException"/>)
    /// </summary>
    public class ScriptTimeoutException : ScriptException {

        /// <summary>
        /// creates a new <see cref="ScriptTimeoutException"/>
        /// </summary>
        /// <param name="timeout">configured timeout that elapsed</param>
        public ScriptTimeoutException(TimeSpan timeout)
            : base($"Script execution exceeded the configured timeout of {timeout}") {
            Timeout = timeout;
        }

        /// <summary>
        /// configured timeout that elapsed
        /// </summary>
        public TimeSpan Timeout { get; }
    }
}
