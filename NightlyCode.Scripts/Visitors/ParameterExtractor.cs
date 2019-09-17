using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Visitors {

    /// <summary>
    /// extracts parameters from a script which are not provided by the script itself
    /// </summary>
    public class ParameterExtractor : ScriptVisitor {
        readonly HashSet<string> parameters=new HashSet<string>();

        /// <summary>
        /// detected parameters
        /// </summary>
        public IEnumerable<string> Parameters => parameters;

        /// <inheritdoc />
        public override void Visit(IScript script) {
            parameters.Clear();
            base.Visit(script);
        }

        /// <inheritdoc />
        public override void VisitVariable(ScriptVariable variable) {
            base.VisitVariable(variable);
            if (!variable.IsResolved)
                parameters.Add(variable.Name);
        }
    }
}