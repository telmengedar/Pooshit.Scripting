namespace Pooshit.Scripting.Data {

    /// <summary>
    /// variable data
    /// </summary>
    public class Variable {

        /// <summary>
        /// creates a new <see cref="Variable"/>
        /// </summary>
        /// <param name="name">name of variable</param>
        /// <param name="value">value value (optional)</param>
        public Variable(string name, object value=null) {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// name of variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// variable value
        /// </summary>
        public object Value { get; set; }
    }
}