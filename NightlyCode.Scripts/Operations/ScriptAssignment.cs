using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Operations {

    /// <summary>
    /// assignment in a script
    /// </summary>
    public class ScriptAssignment : IBinaryToken, IOperator, IAssignableToken {
        IAssignableToken lhs;

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

        /// <inheritdoc />
        public object Execute() {
            return lhs.Assign(Rhs);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            if (Rhs is IAssignableToken assignable)
                return assignable.Assign(token);
            throw new ScriptException("Can't assign value to non assignable token");
        }

        /// <inheritdoc />
        public IScriptToken Lhs {
            get => lhs;
            set {
                lhs=value as IAssignableToken;
                if (lhs == null)
                    throw new ScriptException("Left hand side of assignment has to be an assignable token");
            }
        }

        /// <inheritdoc />
        public IScriptToken Rhs { get; set; }

        public Operator Operator => Operator.Assignment;
    }
}