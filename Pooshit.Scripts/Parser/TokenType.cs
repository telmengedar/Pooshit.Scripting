namespace Pooshit.Scripting.Parser {

    /// <summary>
    /// token types known to parser
    /// </summary>
    public enum TokenType {

        /// <summary>
        /// string type
        /// </summary>
        Literal,

        /// <summary>
        /// number
        /// </summary>
        Number,

        /// <summary>
        /// variable value
        /// </summary>
        Variable,

        /// <summary>
        /// control specifier
        /// </summary>
        Control,

        /// <summary>
        /// registered type
        /// </summary>
        Type,

        /// <summary>
        /// method call
        /// </summary>
        Method,

        /// <summary>
        /// property
        /// </summary>
        Property,

        /// <summary>
        /// operator
        /// </summary>
        Operator,

        /// <summary>
        /// code comment
        /// </summary>
        Comment,

        /// <summary>
        /// variable which has to get provided to the script
        /// </summary>
        Parameter
    }
}