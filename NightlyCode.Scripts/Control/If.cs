using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// statement execution a body when a condition is met
    /// </summary>
    class If : IControlToken {
        readonly IScriptToken condition;

        /// <summary>
        /// creates a new <see cref="If"/> statement
        /// </summary>
        /// <param name="parameters">condition statement has to match to execute body</param>
        public If(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptParserException("Expected exactly one condition for 'if' statement");
            condition = parameters[0];
        }

        /// <inheritdoc />
        public object Execute() {
            if (condition.Execute().ToBoolean())
                return Body.Execute();
            return Else?.Execute();
        }

        /// <summary>
        /// body to execute if condition is met
        /// </summary>
        public IScriptToken Body { get; set; }

        /// <summary>
        /// body to execute when condition is not met
        /// </summary>
        public IScriptToken Else { get; set; }

        public override string ToString() {
            if (Else != null)
                return $"if({condition}) {Body} else {Else}";
            return $"if({condition}) {Body}";
        }
    }
}