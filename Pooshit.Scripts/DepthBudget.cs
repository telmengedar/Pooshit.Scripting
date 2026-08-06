using System.Threading;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting;

/// <summary>
/// tracks the current call depth of one logical execution against a configured ceiling; shared by reference
/// across every <see cref="ScriptContext"/> derived from the same script execution, including an imported
/// script sharing the caller's physical call stack and contexts used concurrently by <c>task.run</c> lambdas,
/// so depth is entered and left with an interlocked increment/decrement, mirroring <see cref="StepBudget.Consume"/>
/// </summary>
class DepthBudget {

    int depth;
    int breached;

    /// <summary>
    /// creates a new <see cref="DepthBudget"/>
    /// </summary>
    /// <param name="limit">maximum call depth allowed before execution is aborted</param>
    public DepthBudget(int limit) {
        Limit = limit;
    }

    /// <summary>
    /// maximum call depth allowed before execution is aborted
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// enters one level of call depth, throwing when the configured limit would be exceeded. Must be called
    /// as the first statement inside a <c>try</c> whose <c>finally</c> unconditionally calls <see cref="Exit"/>,
    /// so a breach here is still paired with a matching exit as the exception unwinds through the caller's
    /// own <c>finally</c> blocks, leaving no depth leaked behind
    /// </summary>
    public void Enter() {
        if (Interlocked.Increment(ref depth) > Limit) {
            Interlocked.Exchange(ref breached, 1);
            throw new ScriptDepthLimitExceededException(Limit);
        }
    }

    /// <summary>
    /// leaves one level of call depth previously entered with <see cref="Enter"/>
    /// </summary>
    public void Exit() => Interlocked.Decrement(ref depth);

    /// <summary>
    /// re-raises the original breach if this budget has ever exceeded its <see cref="Limit"/>, even when the
    /// original <see cref="ScriptDepthLimitExceededException"/> did not reach the host — eg. because a
    /// dispatch wrapper's passthrough filter did not recognise the shape it arrived in and a script
    /// <c>catch</c> swallowed it. Mirrors <see cref="StepBudget"/>'s monotonic property: once breached, always
    /// breached from this point on, even though the depth count itself keeps rising and falling normally with
    /// <see cref="Enter"/> and <see cref="Exit"/> — only the breached state latches, not the count. Called
    /// from <see cref="ScriptContext.Guard"/>, so the very next checkpoint anywhere downstream that shares
    /// <em>this same instance</em> re-raises.
    /// </summary>
    /// <remarks>
    /// This latch is <strong>not</strong> what closes the known swallow route where <c>Task.WaitAll</c> wraps
    /// a faulted task's exception in <see cref="System.AggregateException"/> before
    /// <c>MethodOperations.CallMethod</c>'s reflection call re-wraps that in
    /// <see cref="System.Reflection.TargetInvocationException"/> — that route is closed unconditionally by
    /// <c>MethodOperations.IsAbortOrCancellation</c> unwrapping the aggregate. This latch remains in place as
    /// insurance against a still-undiscovered swallow route on a genuinely shared budget: mirroring
    /// <see cref="StepBudget"/>'s monotonic property is the right invariant to hold uniformly across abort
    /// types, and the cost is one cheap null-conditional check on an already-existing checkpoint.
    /// </remarks>
    public void CheckBreached() {
        if (Volatile.Read(ref breached) != 0)
            throw new ScriptDepthLimitExceededException(Limit);
    }
}
