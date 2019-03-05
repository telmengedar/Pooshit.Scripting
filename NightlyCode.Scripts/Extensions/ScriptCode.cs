namespace NightlyCode.Scripting.Extensions {

    /// <summary>
    /// helper method for script code
    /// </summary>
    public static class ScriptCode {

        /// <summary>
        /// creates scriptcode by joining the lines to a single script
        /// </summary>
        /// <param name="lines">script code lines</param>
        /// <returns>script code</returns>
        public static string Create(params string[] lines) {
            return string.Join("\n", lines);
        }
    }
}