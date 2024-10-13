using System.Collections.Generic;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Visitors {

    /// <summary>
    /// extracts parameters from a script which are not provided by the script itself
    /// </summary>
    public class ParameterExtractor : ScriptVisitor {
        readonly HashSet<string> existing = new HashSet<string>();
        readonly HashSet<ParameterInfo> parameters = new HashSet<ParameterInfo>();

        /// <summary>
        /// detected parameters
        /// </summary>
        public IEnumerable<ParameterInfo> Parameters => parameters;

        /// <inheritdoc />
        public override void Visit(IScript script) {
            existing.Clear();
            parameters.Clear();
            base.Visit(script);
        }

        /// <inheritdoc />
        public override void VisitScriptParameter(ScriptParameter parameter) {
            if(existing.Contains(parameter.Variable.Name))
                return;
            parameters.Add(new ParameterInfo(parameter.Variable.Name, parameter.DefaultValue != null));
            existing.Add(parameter.Variable.Name);
        }
    }
}