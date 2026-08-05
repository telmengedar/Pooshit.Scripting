namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// thrown when a script's call depth exceeds the configured ceiling (<see cref="ScriptLimits.MaxDepth"/>);
    /// raised at the two constructs that recurse through the engine — lambda invocation and imported-script
    /// invocation — before the physical call stack would overflow, since a
    /// <see cref="System.StackOverflowException"/> cannot be caught and would otherwise terminate the process
    /// </summary>
    public class ScriptDepthLimitExceededException : ScriptAbortException {

        /// <summary>
        /// creates a new <see cref="ScriptDepthLimitExceededException"/>
        /// </summary>
        /// <param name="limit">configured depth limit that was exceeded</param>
        public ScriptDepthLimitExceededException(int limit)
            : base($"Script exceeded the configured recursion depth limit of {limit}") {
            Limit = limit;
        }

        /// <summary>
        /// configured depth limit that was exceeded
        /// </summary>
        public int Limit { get; }
    }
}
