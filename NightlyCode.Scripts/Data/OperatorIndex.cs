using NightlyCode.Scripting.Operations;

namespace NightlyCode.Scripting.Data {

    /// <summary>
    /// index of an operator in a token list
    /// </summary>
    internal class OperatorIndex {
        public OperatorIndex(int index, IOperator token) {
            Index = index;
            Token = token;
        }
        public int Index { get; set; }

        public IOperator Token { get; set; }
    }
}