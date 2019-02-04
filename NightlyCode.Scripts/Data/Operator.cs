namespace NightlyCode.Scripting.Data {

    /// <summary>
    /// operator type
    /// </summary>
    /// <remarks>
    /// the order in this enumeration is used for operator priority
    /// </remarks>
    public enum Operator {

        /// <summary>
        /// Negates a boolean value
        /// </summary>
        Not,

        /// <summary>
        /// negates an arithmetic value
        /// </summary>
        Negate,

        /// <summary>
        /// Flips every bit of a numerical value
        /// </summary>
        Complement,

        /// <summary>
        /// Increments the token this operator is attached to by 1
        /// </summary>
        Increment,

        /// <summary>
        /// Decrements the token this operator is attached to by 1
        /// </summary>
        Decrement,

        /// <summary>
        /// Divides the left hand side value by the right hand side value
        /// </summary>
        Division,

        /// <summary>
        /// Multiplies the left hand side value by the right hand side value
        /// </summary>
        Multiplication,

        /// <summary>
        /// Computes the remainder when dividing the left hand side value by the right hand side value
        /// </summary>
        Modulo,

        /// <summary>
        /// Subtracts the right hand side value from the left hand side
        /// </summary>
        Subtraction,

        /// <summary>
        /// Adds left hand side and right hand side
        /// </summary>
        Addition,

        /// <summary>
        /// Determines whether the left hand side value is less than the right hand side value
        /// </summary>
        Less,

        /// <summary>
        /// Determines whether the left hand side value is less than or equal to the right hand side value
        /// </summary>
        LessOrEqual,

        /// <summary>
        /// Determines whether the left hand side value is greater than the right hand side value
        /// </summary>
        Greater,

        /// <summary>
        /// Determines whether the left hand side value is greater than or equal to the right hand side value
        /// </summary>
        GreaterOrEqual,

        /// <summary>
        /// Determines whether the left hand side is equal to the right hand side
        /// </summary>
        Equal,

        /// <summary>
        /// Not Equals|Determines whether the left hand side is not equal to the right hand side
        /// </summary>
        NotEqual,

        /// <summary>
        /// Determines whether the left hand side value matches the regex pattern on the right hand side
        /// </summary>
        Matches,

        /// <summary>
        /// Determines whether the left hand side value does not match the regex pattern of the right hand side
        /// </summary>
        NotMatches,

        /// <summary>
        /// Applies a bitwise and between left hand side and right hand side
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// Applies a bitwise or between left hand side and right hand side
        /// </summary>
        BitwiseOr,

        /// <summary>
        /// Applies a bitwise xor between left hand side and right hand side
        /// </summary>
        BitwiseXor,

        /// <summary>
        /// Shifts left the bits of the left hand side value by the number of steps specified by the right hand side value
        /// </summary>
        ShiftLeft,

        /// <summary>
        /// Shifts right the bits of the left hand side value by the number of steps specified by the right hand side value
        /// </summary>
        ShiftRight,

        /// <summary>
        /// Rolls left the bits of the left hand side value by the number of steps specified by the right hand side value
        /// </summary>
        RolLeft,

        /// <summary>
        /// Rolls right the bits of the left hand side value by the number of steps specified by the right hand side value
        /// </summary>
        RolRight,

        /// <summary>
        /// Computes the logical and between the left hand side boolean and the right hand side boolean
        /// </summary>
        LogicAnd,

        /// <summary>
        /// Computes the logical or between the left hand side boolean and the right hand side boolean
        /// </summary>
        LogicOr,

        /// <summary>
        /// Computes the logical xor between the left hand side boolean and the right hand side boolean
        /// </summary>
        LogicXor,

        /// <summary>
        /// Assigns the value of the right hand side expression to the left hand side token
        /// </summary>
        Assignment,

        /// <summary>
        /// Adds the right hand side to the left hand side storing the result in the left hand side
        /// </summary>
        AddAssign,

        /// <summary>
        /// Subtracts the right hand side from the left hand side storing the result in the left hand side
        /// </summary>
        SubAssign,

        /// <summary>
        /// Divides the left hand side by the right hand side storing the result in the left hand side
        /// </summary>
        DivAssign,

        /// <summary>
        /// Multiplicates the right hand side with the left hand side storing the result in the left hand side
        /// </summary>
        MulAssign,

        /// <summary>
        /// Computes the remainder when dividing the left hand side by the right hand side storing the result in the left hand side
        /// </summary>
        ModAssign,

        /// <summary>
        /// Shifts left the left hand side by the number of steps indicated by the right hand side storing the result in the left hand side
        /// </summary>
        ShlAssign,

        /// <summary>
        /// Shifts right the left hand side by the number of steps indicated by the right hand side storing the result in the left hand side
        /// </summary>
        ShrAssign,

        /// <summary>
        /// Applies a bitwise and between left hand side and right hand side storing the result in the left hand side
        /// </summary>
        AndAssign,

        /// <summary>
        /// Applies a bitwise or between left hand side and right hand side storing the result in the left hand side
        /// </summary>
        OrAssign,

        /// <summary>
        /// Applies a bitwise xor between left hand side and right hand side storing the result in the left hand side
        /// </summary>
        XorAssign,

        /// <summary>
        /// marks all characters until the next linebreak as a comment
        /// </summary>
        SingleLineComment,

        /// <summary>
        /// marks all characters until a multiline comment terminator as a comment
        /// </summary>
        MultilineComment
    }
}