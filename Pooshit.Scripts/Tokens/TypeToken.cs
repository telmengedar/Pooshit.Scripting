using System;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// token used to provide type information
/// </summary>
public class TypeToken : ScriptToken {
    readonly Type type;

    /// <summary>
    /// creates a new <see cref="TypeToken"/>
    /// </summary>
    /// <param name="type">type provided by token</param>
    public TypeToken(Type type) {
        this.type = type;
    }

    /// <inheritdoc />
    public override string Literal => type.Name.ToLower();

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        return type;
    }
}