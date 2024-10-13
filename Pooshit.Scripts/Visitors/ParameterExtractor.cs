using System.Collections.Generic;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Visitors;

/// <summary>
/// extracts parameters from a script which are not provided by the script itself
/// </summary>
public class ParameterExtractor : ScriptVisitor {
    readonly HashSet<string> existing = [];
    readonly HashSet<ParameterInfo> parameters = [];

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
        parameters.Add(new(parameter.Variable.Name, parameter.DefaultValue != null));
        existing.Add(parameter.Variable.Name);
    }
}