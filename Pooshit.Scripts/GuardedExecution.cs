using System;
using System.Threading;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting;

/// <summary>
/// bundles the context and bookkeeping needed to run a script under its configured <see cref="ScriptLimits"/>.
/// Realises the execution timeout as a linked, deadline-armed <see cref="CancellationTokenSource"/> rather
/// than a new observation mechanism, so every checkpoint the engine establishes already honors it for free;
/// converts a deadline-only cancellation into a <see cref="ScriptTimeoutException"/> while leaving a genuine
/// caller cancellation untouched
/// </summary>
sealed class GuardedExecution : IDisposable {
    readonly CancellationTokenSource linkedSource;
    readonly CancellationToken callerToken;
    readonly TimeSpan? timeout;

    GuardedExecution(ScriptContext context, CancellationToken executionToken, CancellationToken callerToken, TimeSpan? timeout, CancellationTokenSource linkedSource) {
        Context = context;
        ExecutionToken = executionToken;
        this.callerToken = callerToken;
        this.timeout = timeout;
        this.linkedSource = linkedSource;
    }

    /// <summary>
    /// context to execute the script with
    /// </summary>
    public ScriptContext Context { get; }

    /// <summary>
    /// token the worker must be run under: the linked, deadline-armed token when a timeout is configured,
    /// the caller's own token otherwise. Passing this exact token to <see cref="System.Threading.Tasks.Task.Run(Action,CancellationToken)"/>
    /// is what lets the framework classify a cancelled worker as <c>Canceled</c> rather than <c>Faulted</c>
    /// </summary>
    public CancellationToken ExecutionToken { get; }

    /// <summary>
    /// prepares a guarded execution for the given variables and caller token, applying <paramref name="limits"/>
    /// </summary>
    /// <param name="variables">arguments provided at runtime</param>
    /// <param name="typeprovider">access to available types</param>
    /// <param name="callertoken">token supplied by the caller</param>
    /// <param name="limits">execution guards configured on the parser that produced this script</param>
    /// <param name="inheritedDepthBudget">depth budget inherited from the caller of an imported script, used as-is instead of allocating a fresh one, or <c>null</c> for a top-level execution</param>
    /// <returns>a guarded execution ready to run; must be disposed once the run completes</returns>
    public static GuardedExecution Prepare(IVariableProvider variables, ITypeProvider typeprovider, CancellationToken callertoken, ScriptLimits limits, DepthBudget inheritedDepthBudget = null) {
        StepBudget stepbudget = limits.MaxSteps.HasValue ? new StepBudget(limits.MaxSteps.Value) : null;
        DepthBudget depthbudget = inheritedDepthBudget ?? (limits.MaxDepth.HasValue ? new DepthBudget(limits.MaxDepth.Value) : null);

        if (!limits.Timeout.HasValue) {
            ScriptContext context = new(variables, typeprovider, callertoken, limits, stepbudget, depthbudget);
            return new GuardedExecution(context, callertoken, callertoken, null, null);
        }

        CancellationTokenSource linkedsource = CancellationTokenSource.CreateLinkedTokenSource(callertoken);
        linkedsource.CancelAfter(limits.Timeout.Value);
        ScriptContext timeoutcontext = new(variables, typeprovider, linkedsource.Token, limits, stepbudget, depthbudget);
        return new GuardedExecution(timeoutcontext, linkedsource.Token, callertoken, limits.Timeout, linkedsource);
    }

    /// <summary>
    /// converts a cancellation caught while running the script into the correct outcome: rethrows the
    /// original exception when the caller's own token was cancelled, or throws a <see cref="ScriptTimeoutException"/>
    /// when the configured deadline fired on its own
    /// </summary>
    /// <param name="exception">exception caught while running the script</param>
    /// <returns>never returns normally; always throws</returns>
    public object Convert(OperationCanceledException exception) {
        if (timeout.HasValue && !callerToken.IsCancellationRequested)
            throw new ScriptTimeoutException(timeout.Value);
        throw exception;
    }

    /// <inheritdoc />
    public void Dispose() => linkedSource?.Dispose();
}
