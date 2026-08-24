using System;
using System.Threading;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// runs a parse on a dedicated background thread with a hard wait bound
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
            IScript result = null;
            Exception fault = null;
            Thread worker = new(() => {
                try {
                    result = parser.Parse(source);
                }
                catch (Exception ex) {
                    fault = ex;
                }
            }) {IsBackground = true};

            worker.Start();
            bool completed = worker.Join(bound);
            script = completed ? result : null;
            error = completed ? fault : null;
            return completed;
        }
    }
}
