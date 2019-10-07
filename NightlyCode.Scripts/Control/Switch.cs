using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Control.Internal;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// evaluates a value and jumps to matching cases
    /// </summary>
    public class Switch : ControlToken, IParameterContainer {
        readonly IScriptToken condition;
        readonly List<Case> cases=new List<Case>();

        readonly FuncStatementBlock body;

        /// <summary>
        /// creates a new <see cref="Switch"/> statement
        /// </summary>
        /// <param name="condition">value to evaluate</param>
        internal Switch(IScriptToken condition) {
            this.condition = condition;
            body = new FuncStatementBlock(BodyFunc);
        }

        IEnumerable<IScriptToken> BodyFunc() {
            foreach (Case @case in cases)
                yield return @case;
            if (Default != null)
                yield return Default;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get { yield return condition; }
        }

        /// <summary>
        /// cases for switch branch
        /// </summary>
        public IEnumerable<Case> Cases => cases;

        /// <summary>
        /// default case executed if no other case matches
        /// </summary>
        public Case Default { get; set; }

        /// <summary>
        /// adds a case to the switch statement
        /// </summary>
        /// <param name="case">case to add</param>
        public void AddCase(Case @case) {
            if (@case.IsDefault)
                Default = @case;
            else cases.Add(@case);
        }

        /// <inheritdoc />
        public override string Literal => "switch";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context)
        {
            object value = condition.Execute(context);
            Case @case = cases.FirstOrDefault(c => c.Matches(value, context));
            if (@case == null)
                return Default?.Execute(context);
            return @case.Execute(context);
        }

        /// <inheritdoc />
        public override string ToString() {
            if (Default != null)
                return $"switch({condition}) {string.Join(" ", cases)} default {Default.Body}";
            return $"switch({condition}) {string.Join(" ", cases)}";
        }

        /// <summary>
        /// a body is not used for switch statements
        /// </summary>
        public override IScriptToken Body {
            get => body;
            internal set => throw new NotSupportedException();
        }
    }
}