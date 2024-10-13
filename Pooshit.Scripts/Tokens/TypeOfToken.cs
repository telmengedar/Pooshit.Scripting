using System.Collections.Generic;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// extracts type of an expression
/// </summary>
public class TypeOfToken : ScriptToken, IParameterContainer {
    readonly IScriptToken token;

    /// <summary>
    /// creates a new <see cref="TypeOfToken"/>
    /// </summary>
    /// <param name="token">token to be analysed</param>
    public TypeOfToken(IScriptToken token) {
        this.token = token;
    }

    /// <inheritdoc />
    public override string Literal => "typeof";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        object result = token.Execute(context);
        return result?.GetType();
    }

    /// <inheritdoc />
    public IEnumerable<IScriptToken> Parameters {
        get { yield return token; }
    }

    /// <inheritdoc />
    public bool ParametersOptional => false;
}