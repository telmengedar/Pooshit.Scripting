using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// assignment in a script
    /// </summary>
    public class ScriptAssignment : IBinaryToken, IOperator {

        /// <summary>
        /// creates a new <see cref="ScriptAssignment"/>
        /// </summary>
        internal ScriptAssignment() { }

        /// <summary>
        /// creates a new <see cref="ScriptAssignment"/>
        /// </summary>
        /// <param name="lhs">target of assignment</param>
        /// <param name="rhs">value to assign</param>
        public ScriptAssignment(IScriptToken lhs, IScriptToken rhs) {
            Lhs = lhs;
            Rhs = rhs;
        }

        public override string ToString() {
            return $"{Lhs} = {Rhs}";
        }

        public object Execute() {
            return Lhs.Assign(Rhs);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            return Rhs.Assign(token);
        }

        public IScriptToken Lhs { get; set; }

        public IScriptToken Rhs { get; set; }
        public Operator Operator => Operator.Assignment;
    }
}