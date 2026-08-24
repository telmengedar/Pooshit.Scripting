using System;
using System.Threading.Tasks;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// runs a parse on a thread-pool worker with a hard wait bound, so a non-terminating parse
    /// (DiVoid #9341: <see cref="Pooshit.Scripting.Parser.ScriptParser"/>'s <c>ParseDictionary</c> spinning
    /// forever on an unterminated top-level '{') fails the assertion instead of hanging the whole test run.
    /// A spinning worker cannot be aborted once the bound is exceeded - .NET gives no supported way to kill
    /// a thread stuck in a tight managed loop - so the run is deliberately started via <see cref="Task.Run(Func{object})"/>
    /// (a background thread-pool thread) rather than a foreground <see cref="System.Threading.Thread"/>. A
    /// timed-out call leaks one spinning worker rather than blocking process/test-runner exit.
    /// </summary>
    static class BoundedParse {

        /// <summary>
        /// attempts to parse <paramref name="source"/>, waiting at most <paramref name="bound"/> for it to
        /// return
        /// </summary>
        /// <param name="parser">parser to use</param>
        /// <param name="source">script source to parse</param>
        /// <param name="bound">maximum time to wait for the parse call to return</param>
        /// <param name="script">the parsed script, when parsing completed and succeeded</param>
        /// <param name="error">the exception the parse call raised, when parsing completed and faulted</param>
        /// <returns>true when the parse call returned (successfully or with an exception) within <paramref name="bound"/>; false when the bound elapsed first</returns>
        public static bool TryParse(IScriptParser parser, string source, TimeSpan bound, out IScript script, out Exception error) {
            Task<IScript> task = Task.Run(() => parser.Parse(source));
            bool completed;
            try {
                // Task.Wait rethrows the task's own exception (wrapped in an AggregateException) when the
                // task finishes faulted within the bound - that still means it completed within the bound,
                // so it must not be mistaken for the bound having elapsed
                completed = task.Wait(bound);
            }
            catch (AggregateException) {
                completed = true;
            }

            script = completed && task.Status == TaskStatus.RanToCompletion ? task.Result : null;
            error = completed && task.IsFaulted ? task.Exception?.GetBaseException() : null;
            return completed;
        }
    }
}
