using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// evaluates a value and jumps to matching cases
    /// </summary>
    public class Switch : IScriptToken {
        readonly IScriptToken condition;
        readonly List<Case> cases=new List<Case>();

        /// <summary>
        /// creates a new <see cref="Switch"/> statement
        /// </summary>
        /// <param name="parameters">parameters which contain value to evaluate</param>
        public Switch(IScriptToken[] parameters) {
            if (parameters.Length != 1)
                throw new ScriptParserException("Switch statement needs exactly one value parameter");
            condition = parameters[0];
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

        public object Execute() {
            object value = condition.Execute();
            Case @case = cases.FirstOrDefault(c => c.Matches(value));
            if (@case == null)
                return Default?.Execute();
            return @case.Execute();
        }

        public override string ToString() {
            if (Default != null)
                return $"switch({condition}) {string.Join(" ", cases)} default {Default.Body}";
            return $"switch({condition}) {string.Join(" ", cases)}";
        }
    }
}