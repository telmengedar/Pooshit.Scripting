using System.Threading;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting;

/// <summary>
/// context for script execution
/// </summary>
public class ScriptContext {

    /// <summary>
    /// creates a new <see cref="ScriptContext"/>
    /// </summary>
    /// <param name="context">context to base this context on</param>
    public ScriptContext(ScriptContext context)
        : this(new VariableProvider(context.Arguments), context.TypeProvider, context.CancellationToken) {
        Limits = context.Limits;
        StepBudget = context.StepBudget;
    }

    /// <summary>
    /// creates a new <see cref="ScriptContext"/>
    /// </summary>
    /// <param name="arguments">arguments provided at runtime</param>
    /// <param name="typeprovider">access to available types</param>
    public ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider) {
        Arguments = arguments;
        TypeProvider = typeprovider;
        Limits = ScriptLimits.None;
    }

    /// <summary>
    /// creates a new <see cref="ScriptContext"/>
    /// </summary>
    /// <param name="arguments">arguments provided at runtime</param>
    /// <param name="typeprovider">access to available types</param>
    /// <param name="cancellationToken">cancellation token used to abort script execution (optional)</param>
    public ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider, CancellationToken cancellationToken)
        : this(arguments, typeprovider) {
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// creates a new <see cref="ScriptContext"/> at the engine execution boundary, with explicit guards
    /// </summary>
    /// <param name="arguments">arguments provided at runtime</param>
    /// <param name="typeprovider">access to available types</param>
    /// <param name="cancellationToken">cancellation token used to abort script execution</param>
    /// <param name="limits">execution guards configured for this execution; <c>null</c> is treated as <see cref="ScriptLimits.None"/></param>
    /// <param name="stepBudget">step budget backing <see cref="Limits"/>.<see cref="ScriptLimits.MaxSteps"/>, or <c>null</c> when unconfigured</param>
    internal ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider, CancellationToken cancellationToken, ScriptLimits limits, StepBudget stepBudget)
        : this(arguments, typeprovider, cancellationToken) {
        Limits = limits ?? ScriptLimits.None;
        StepBudget = stepBudget;
    }

    /// <summary>
    /// arguments provided at runtime
    /// </summary>
    public IVariableProvider Arguments { get; }

    /// <summary>
    /// provider for known type information
    /// </summary>
    public ITypeProvider TypeProvider { get; set; }

    /// <summary>
    /// cancellation token used to abort script execution
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// execution guards configured for the running script; never <c>null</c>, defaults to <see cref="ScriptLimits.None"/>
    /// </summary>
    public ScriptLimits Limits { get; private set; }

    /// <summary>
    /// step budget tracking consumed engine checkpoints, or <c>null</c> when no step limit is configured
    /// </summary>
    internal StepBudget StepBudget { get; private set; }

    /// <summary>
    /// checkpoint called at every engine-controlled loop iteration, statement and callback invocation;
    /// throws when the cancellation token has been cancelled or the configured step budget is exhausted
    /// </summary>
    public void Guard() {
        CancellationToken.ThrowIfCancellationRequested();
        StepBudget?.Consume();
    }
}