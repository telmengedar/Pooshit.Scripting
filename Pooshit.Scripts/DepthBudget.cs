using System.Threading;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting;

/// <summary>
/// tracks the current call depth against a configured ceiling; shared by reference across every
/// <see cref="ScriptContext"/> derived from the same script execution, so depth is entered and left with an
/// interlocked increment/decrement
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
    /// enters one level of call depth, throwing when the configured limit would be exceeded; must be paired
    /// with <see cref="Exit"/> in a <c>finally</c>
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
    /// re-raises the original breach if this budget has ever exceeded its <see cref="Limit"/>, even after a
    /// swallowed <see cref="ScriptDepthLimitExceededException"/> let the count recover
    /// </summary>
    public void CheckBreached() {
        if (Volatile.Read(ref breached) != 0)
            throw new ScriptDepthLimitExceededException(Limit);
    }

    /// <summary>
    /// clears a previous breach latch for a new run sharing this budget; the physical <see cref="depth"/> count is untouched
    /// </summary>
    internal void ResetBreach() => Interlocked.Exchange(ref breached, 0);
}
