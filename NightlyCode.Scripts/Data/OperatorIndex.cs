using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Data {

    /// <summary>
    /// index of an operator in a token list
    /// </summary>
    class OperatorIndex {

        /// <summary>
        /// creates a new <see cref="OperatorIndex"/>
        /// </summary>
        /// <param name="index">index where operator is located</param>
        /// <param name="token">operator token</param>
        public OperatorIndex(int index, IOperator token) {
            Index = index;
            Token = token;
        }

        /// <summary>
        /// index where operator is located
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// operator token
        /// </summary>
        public IOperator Token { get; set; }
    }
}