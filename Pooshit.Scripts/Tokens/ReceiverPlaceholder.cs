namespace Pooshit.Scripting.Tokens;

/// <summary>
/// leaf token that stands for the evaluated receiver value inside a <see cref="ConditionalAccess"/> continuation
/// </summary>
/// <remarks>
/// The placeholder is bound to the receiver value in a child <see cref="ScriptContext"/> and is not
/// dispatchable by <see cref="Visitors.ScriptVisitor"/>, preventing visitor tooling from treating it
/// as a user variable.
/// </remarks>
public class ReceiverPlaceholder : ScriptToken {

    /// <summary>
    /// reserved name used to bind the receiver value in a child context; starts with '?' so it
    /// cannot be produced by the variable parser
    /// </summary>
    internal const string ReservedName = "?receiver";

    internal ReceiverPlaceholder() {
    }

    /// <inheritdoc />
    public override string Literal => "";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        return context.Arguments.GetVariable(ReservedName);
    }
}
