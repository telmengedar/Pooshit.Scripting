namespace Pooshit.Scripting.Errors {

    /// <summary>
    /// which variable-usage threshold a <see cref="ScriptVariableLimitExceededException"/> was raised for
    /// </summary>
    public enum VariableLimitKind {

        /// <summary>
        /// live variable entry count exceeded <see cref="ScriptLimits.MaxVariables"/>
        /// </summary>
        Entries,

        /// <summary>
        /// approximated variable footprint exceeded <see cref="ScriptLimits.MaxVariableBytes"/>
        /// </summary>
        Bytes
    }
}
