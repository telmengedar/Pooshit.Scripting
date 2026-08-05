namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// thrown when a script exceeds the configured step limit (<see cref="ScriptLimits.MaxSteps"/>); a
    /// "step" is an engine checkpoint (one statement, one loop iteration, one lambda invocation, one
    /// enumerated element), not an instruction count
    /// </summary>
    public class ScriptStepLimitExceededException : ScriptException {

        /// <summary>
        /// creates a new <see cref="ScriptStepLimitExceededException"/>
        /// </summary>
        /// <param name="limit">configured step limit that was exceeded</param>
        public ScriptStepLimitExceededException(long limit)
            : base($"Script exceeded the configured step limit of {limit}") {
            Limit = limit;
        }

        /// <summary>
        /// configured step limit that was exceeded
        /// </summary>
        public long Limit { get; }
    }
}
