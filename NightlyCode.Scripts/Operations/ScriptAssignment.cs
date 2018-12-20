namespace NightlyCode.Scripting.Operations {
    public class ScriptAssignment : IScriptToken {
        readonly IScriptToken lhs;
        readonly IScriptToken rhs;

        /// <summary>
        /// creates a new <see cref="ScriptAssignment"/>
        /// </summary>
        /// <param name="lhs">target of assignment</param>
        /// <param name="rhs">value to assign</param>
        public ScriptAssignment(IScriptToken lhs, IScriptToken rhs) {
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public override string ToString() {
            return $"{lhs} = {rhs}";
        }

        public object Execute() {
            return lhs.Assign(rhs);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            return rhs.Assign(token);
        }
    }
}