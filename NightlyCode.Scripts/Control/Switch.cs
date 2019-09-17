using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Control.Internal;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// evaluates a value and jumps to matching cases
    /// </summary>
    public class Switch : ControlToken {
        readonly IScriptToken condition;
        readonly List<Case> cases=new List<Case>();

        readonly FuncStatementBlock body;

        /// <summary>
        /// creates a new <see cref="Switch"/> statement
        /// </summary>
        /// <param name="parameters">parameters which contain value to evaluate</param>
        internal Switch(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptParserException("Switch statement needs exactly one value parameter");
            condition = parameters[0];

            body = new FuncStatementBlock(BodyFunc);
        }

        IEnumerable<IScriptToken> BodyFunc() {
            foreach (Case @case in cases)
                yield return @case;
            if (Default != null)
                yield return Default;
        }

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
        protected override object ExecuteToken(IVariableContext variables, IVariableProvider arguments)
        {
            object value = condition.Execute(variables, arguments);
            Case @case = cases.FirstOrDefault(c => c.Matches(value, variables, arguments));
            if (@case == null)
                return Default?.Execute(variables, arguments);
            return @case.Execute(variables, arguments);
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