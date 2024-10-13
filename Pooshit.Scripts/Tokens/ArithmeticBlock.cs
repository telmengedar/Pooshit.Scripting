
namespace Pooshit.Scripting.Tokens;

/// <summary>
/// block containing an arithmetic evaluation
/// </summary>
public class ArithmeticBlock : ScriptToken {
    readonly IScriptToken inner;

    internal ArithmeticBlock(IScriptToken inner) {
        this.inner = inner;
    }

    /// <summary>
    /// inner statement block
    /// </summary>
    public IScriptToken InnerBlock => inner;

    /// <inheritdoc />
    public override string Literal => "(...)";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        return inner.Execute(context);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"({inner})";
    }
}