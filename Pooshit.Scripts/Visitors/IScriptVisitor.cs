namespace NightlyCode.Scripting.Visitors {

    /// <summary>
    /// visits every token of a script
    /// </summary>
    public interface IScriptVisitor {

        /// <summary>
        /// traverses a script
        /// </summary>
        /// <param name="script"></param>
        void Visit(IScript script);
    }
}