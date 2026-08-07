namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// thrown when a script's variable usage exceeds a configured ceiling
    /// (<see cref="ScriptLimits.MaxVariables"/> or <see cref="ScriptLimits.MaxVariableBytes"/>)
    /// </summary>
    public class ScriptVariableLimitExceededException : ScriptAbortException {

        /// <summary>
        /// creates a new <see cref="ScriptVariableLimitExceededException"/>
        /// </summary>
        /// <param name="kind">which threshold was exceeded</param>
        /// <param name="limit">configured limit that was exceeded</param>
        /// <param name="measured">measured value at the time of the breach</param>
        public ScriptVariableLimitExceededException(VariableLimitKind kind, long limit, long measured)
            : base($"Script exceeded the configured variable {kind} limit of {limit} ({measured} measured)") {
            Kind = kind;
            Limit = limit;
            Measured = measured;
        }

        /// <summary>
        /// which threshold was exceeded
        /// </summary>
        public VariableLimitKind Kind { get; }

        /// <summary>
        /// configured limit that was exceeded
        /// </summary>
        public long Limit { get; }

        /// <summary>
        /// measured value at the time of the breach
        /// </summary>
        public long Measured { get; }
    }
}
