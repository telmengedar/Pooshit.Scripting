namespace Pooshit.Scripting.Tokens;

/// <summary>
/// evaluates a member access and short-circuits to null when the receiver is null, i.e. <c>a?.b</c>
/// </summary>
/// <remarks>
/// C#-faithful semantics: the <c>?.</c> guards only its immediate receiver. <c>a?.b.c</c> is
/// equivalent to <c>a == null ? null : a.b.c</c> — if <c>a</c> is non-null but <c>a.b</c> is null,
/// accessing <c>.c</c> still throws. Use <c>a?.b?.c</c> to guard each receiver independently.
/// </remarks>
public class ConditionalAccess : ScriptToken {

    internal ConditionalAccess(IScriptToken receiver, ReceiverPlaceholder placeholder, IScriptToken continuation) {
        Receiver = receiver;
        Placeholder = placeholder;
        Continuation = continuation;
    }

    /// <summary>
    /// expression evaluated once to produce the receiver; null causes the whole expression to return null
    /// </summary>
    public IScriptToken Receiver { get; }

    /// <summary>
    /// placeholder that is bound to the evaluated receiver value in the child scope
    /// </summary>
    public ReceiverPlaceholder Placeholder { get; }

    /// <summary>
    /// member-access chain evaluated against the receiver when the receiver is non-null
    /// </summary>
    public IScriptToken Continuation { get; internal set; }

    /// <inheritdoc />
    public override string Literal => "?.";

    /// <inheritdoc />
    protected override object ExecuteToken(ScriptContext context) {
        object receiverValue = Receiver.Execute(context);
        if (receiverValue == null)
            return null;
        ScriptContext childContext = new(context);
        childContext.Arguments[ReceiverPlaceholder.ReservedName] = receiverValue;
        return Continuation.Execute(childContext);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Receiver}?{Continuation}";
}
