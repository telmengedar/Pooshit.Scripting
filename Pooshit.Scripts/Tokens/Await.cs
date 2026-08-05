using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting.Tokens;

/// <summary>
/// await token used to await async tasks
/// </summary>
public class Await : ScriptToken, IParameterContainer {
    readonly IScriptToken token;

    /// <summary>
    /// creates a new <see cref="Await"/> token
    /// </summary>
    /// <param name="token">token resulting in task to be awaited</param>
    public Await(IScriptToken token) {
        this.token = token;
    }

    /// <summary>
    /// task to be awaited
    /// </summary>
    public IScriptToken Task => token;

    /// <inheritdoc />
    public override string Literal => "await";

    /// <inheritdoc />
    /// <remarks>
    /// A cancellation of <see cref="ScriptContext.CancellationToken"/> surfaces from <see cref="Task.Wait(System.Threading.CancellationToken)"/>
    /// as a plain <see cref="OperationCanceledException"/> (not wrapped in an <see cref="AggregateException"/>)
    /// and flows past the unwrap block below untouched. The host owns the awaited task itself and it keeps
    /// running — the engine never created it and must not attempt to cancel it.
    /// </remarks>
    protected override object ExecuteToken(ScriptContext context) {
        object result = token.Execute(context);
        if (!(result is Task task))
            throw new ScriptRuntimeException("Only tasks can get awaited", this);

        if (task.Status == TaskStatus.Created)
            task.Start();

        try {
            task.Wait(context.CancellationToken);
        }
        catch (AggregateException e) {
            Exception unwrapped = e;
            while (unwrapped is AggregateException agg && agg.InnerException != null)
                unwrapped = agg.InnerException;
            throw unwrapped;
        }


        if (!task.GetType().IsGenericType)
            return null;

        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    /// <inheritdoc />
    public IEnumerable<IScriptToken> Parameters {
        get { yield return token; }
    }

    /// <inheritdoc />
    public bool ParametersOptional => false;

    /// <inheritdoc />
    public override string ToString() {
        return $"await({Task})";
    }
}