using Pooshit.Scripting.Extensions;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// evaluates one of two branches based on a condition, i.e. <c>cond ? a : b</c>
/// </summary>
public class ConditionalToken : ScriptToken {

    /// <summary>
    /// creates a new <see cref="ConditionalToken"/>
    /// </summary>
    /// <param name="condition">expression whose boolean value selects the branch</param>
    /// <param name="whenTrue">expression evaluated when condition is true</param>
    /// <param name="whenFalse">expression evaluated when condition is false</param>
    internal ConditionalToken(IScriptToken condition, IScriptToken whenTrue, IScriptToken whenFalse) {
        Condition = condition;
        WhenTrue = whenTrue;
        WhenFalse = whenFalse;
    }

    /// <summary>
    /// condition determining which branch to evaluate
    /// </summary>
    public IScriptToken Condition { get; }

    /// <summary>
    /// branch evaluated when <see cref="Condition"/> is true
    /// </summary>
    public IScriptToken WhenTrue { get; }

    /// <summary>
    /// branch evaluated when <see cref="Condition"/> is false
    /// </summary>
    public IScriptToken WhenFalse { get; }

    /// <inheritdoc />
    public override string Literal => "?:";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        return Condition.Execute(context).ToBoolean() ? WhenTrue.Execute(context) : WhenFalse.Execute(context);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Condition} ? {WhenTrue} : {WhenFalse}";
    }
}
