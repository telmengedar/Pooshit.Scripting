namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// base for exceptions the engine itself raises to stop a script's execution — a step-limit overrun, an
    /// execution timeout, or a recursion-depth breach — as distinct from <see cref="ScriptRuntimeException"/>,
    /// which reports the script's own misbehavior; every catch-all dispatch wrapper in the engine rethrows
    /// this base unconditionally, ahead of its catch-all, so an abort can never be downgraded into ordinary,
    /// catchable script control flow
    /// </summary>
    public abstract class ScriptAbortException : ScriptException {

        /// <summary>
        /// creates a new <see cref="ScriptAbortException"/>
        /// </summary>
        /// <param name="message">error message</param>
        protected ScriptAbortException(string message) : base(message) {
        }
    }
}
