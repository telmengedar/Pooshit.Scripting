using System;
using System.Diagnostics;
using System.Threading;

namespace Pooshit.Scripting;

/// <summary>
/// enforces a script's configured <see cref="ScriptLimits.Timeout"/> from the executing thread itself,
/// checked at every <see cref="ScriptContext.Guard"/> checkpoint instead of relying solely on a
/// thread-pool-scheduled callback; shared by reference across every <see cref="ScriptContext"/> derived
/// from the same script execution
/// </summary>
sealed class DeadlineGuard {
    readonly long deadline;
    readonly CancellationTokenSource source;

    /// <summary>
    /// creates a new <see cref="DeadlineGuard"/>
    /// </summary>
    /// <param name="timeout">duration from now until the deadline elapses</param>
    /// <param name="source">token source cancelled once the deadline elapses</param>
    public DeadlineGuard(TimeSpan timeout, CancellationTokenSource source) {
        deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        this.source = source;
    }

    /// <summary>
    /// cancels <see cref="source"/> once the deadline has elapsed; a no-op before then
    /// </summary>
    public void Check() {
        if (Stopwatch.GetTimestamp() >= deadline)
            source.Cancel();
    }
}
