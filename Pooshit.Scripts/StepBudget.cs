using System.Threading;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting;

/// <summary>
/// tracks the number of engine checkpoints consumed against a configured step limit; shared by reference
/// across every <see cref="ScriptContext"/> derived from the same script execution, including contexts
/// used concurrently by <c>task.run</c> lambdas, so consumption is counted with an interlocked increment
/// </summary>
class StepBudget {

    long consumed;

    /// <summary>
    /// creates a new <see cref="StepBudget"/>
    /// </summary>
    /// <param name="limit">maximum number of steps allowed before execution is aborted</param>
    public StepBudget(long limit) {
        Limit = limit;
    }

    /// <summary>
    /// maximum number of steps allowed before execution is aborted
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// consumes one step, throwing when the configured limit has been exceeded
    /// </summary>
    public void Consume() {
        if (Interlocked.Increment(ref consumed) > Limit)
            throw new ScriptStepLimitExceededException(Limit);
    }
}
