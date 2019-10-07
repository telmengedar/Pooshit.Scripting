using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// statement execution a body when a condition is met
    /// </summary>
    public class If : ControlToken, IParameterContainer {
        readonly IScriptToken condition;

        /// <summary>
        /// creates a new <see cref="If"/> statement
        /// </summary>
        /// <param name="condition">condition statement has to match to execute body</param>
        internal If(IScriptToken condition) {
            this.condition = condition;
        }

        /// <inheritdoc />
        public override string Literal => "if";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            if (condition.Execute(context).ToBoolean())
                return Body.Execute(context);
            return Else?.Execute(context);
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get { yield return condition; }
        }

        /// <summary>
        /// body to execute if condition is met
        /// </summary>
        public override IScriptToken Body { get; internal set; }

        /// <summary>
        /// body to execute when condition is not met
        /// </summary>
        public IScriptToken Else { get; internal set; }

        /// <inheritdoc />
        public override string ToString() {
            if (Else != null)
                return $"if({condition}) {Body} else {Else}";
            return $"if({condition}) {Body}";
        }
    }
}