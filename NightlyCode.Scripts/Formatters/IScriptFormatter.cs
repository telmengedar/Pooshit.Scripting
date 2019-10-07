namespace NightlyCode.Scripting.Formatters {

    /// <summary>
    /// formats scripts for text editors
    /// </summary>
    public interface IScriptFormatter {

        /// <summary>
        /// converts a script into a readable string format
        /// </summary>
        /// <param name="script">script to format</param>
        /// <returns>formatted script string</returns>
        string FormatScript(IScript script);
    }
}