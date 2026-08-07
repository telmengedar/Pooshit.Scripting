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
        DepthBudget = context.DepthBudget;
        VariableBudget = context.VariableBudget;
    }

    /// <summary>
    /// creates a new <see cref="ScriptContext"/> based on <paramref name="context"/>, exactly like the copy
    /// constructor, but substituting <paramref name="depthBudget"/> for the inherited one
    /// </summary>
    /// <param name="context">context to base this context on</param>
    /// <param name="depthBudget">depth budget to use instead of <paramref name="context"/>'s own</param>
    internal ScriptContext(ScriptContext context, DepthBudget depthBudget)
        : this(new VariableProvider(context.Arguments), context.TypeProvider, context.CancellationToken) {
        Limits = context.Limits;
        StepBudget = context.StepBudget;
        DepthBudget = depthBudget;
        VariableBudget = context.VariableBudget;
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
    /// <param name="depthBudget">depth budget backing <see cref="Limits"/>.<see cref="ScriptLimits.MaxDepth"/>, or <c>null</c> when unconfigured</param>
    /// <param name="variableBudget">variable budget backing <see cref="Limits"/>.<see cref="ScriptLimits.MaxVariables"/>/<see cref="ScriptLimits.MaxVariableBytes"/>, or <c>null</c> when unconfigured</param>
    internal ScriptContext(IVariableProvider arguments, ITypeProvider typeprovider, CancellationToken cancellationToken, ScriptLimits limits, StepBudget stepBudget, DepthBudget depthBudget, VariableBudget variableBudget)
        : this(arguments, typeprovider, cancellationToken) {
        Limits = limits ?? ScriptLimits.None;
        StepBudget = stepBudget;
        DepthBudget = depthBudget;
        VariableBudget = variableBudget;
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
    /// depth budget tracking call depth through lambda invocation and imported-script invocation, or
    /// <c>null</c> when no depth limit is configured
    /// </summary>
    internal DepthBudget DepthBudget { get; private set; }

    /// <summary>
    /// variable budget tracking a script's own variable usage, or <c>null</c> when neither
    /// <see cref="ScriptLimits.MaxVariables"/> nor <see cref="ScriptLimits.MaxVariableBytes"/> is configured
    /// </summary>
    internal VariableBudget VariableBudget { get; private set; }

    /// <summary>
    /// checkpoint called at every engine-controlled loop iteration, statement and callback invocation, enforcing the configured step, depth and variable budgets
    /// </summary>
    public void Guard() {
        CancellationToken.ThrowIfCancellationRequested();
        StepBudget?.Consume();
        DepthBudget?.CheckBreached();
        VariableBudget?.Observe(Arguments);
    }
}