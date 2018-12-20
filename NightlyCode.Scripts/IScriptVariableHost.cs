namespace NightlyCode.Scripting {

    /// <summary>
    /// host for script variables
    /// </summary>
    public interface IScriptVariableHost {

        /// <summary>
        /// get value of a variable
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <returns>variable value</returns>
        object GetVariable(string name);

        /// <summary>
        /// set a value of a variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value to set</param>
        void SetVariable(string name, object value);
    }
}