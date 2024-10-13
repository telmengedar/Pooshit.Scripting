using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Operations;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// access to variable in script
/// </summary>
public class ScriptVariable : AssignableToken {

    /// <summary>
    /// creates a new <see cref="ScriptVariable"/>
    /// </summary>
    /// <param name="name">name of variable</param>
    internal ScriptVariable(string name) {
        Name = name;
    }

    /// <summary>
    /// name of variable
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string Literal => "$value";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        IVariableProvider provider = context.Arguments.GetProvider(Name);
        if(provider != null)
            return provider.GetVariable(Name);

        provider = context.Arguments.GetProvider(Name);
        if(provider == null)
            throw new ScriptRuntimeException($"Variable {Name} not declared", this);

        return provider.GetVariable(Name);
    }

    /// <inheritdoc />
    protected override object AssignToken(IScriptToken token, ScriptContext context) {
        IVariableProvider provider = context.Arguments.GetProvider(Name);
        if(provider == null)
            // auto declare variable in current scope if variable is not found
            provider = context.Arguments;

        object value = token.Execute(context);
        provider[Name] = value;
        return value;
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"${Name}";
    }
}