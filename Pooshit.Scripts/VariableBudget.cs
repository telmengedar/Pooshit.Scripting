using System;
using System.Collections.Generic;
using System.Threading;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Pooshit.Scripting;

/// <summary>
/// tracks a script's own variable usage against configured entry-count and byte-footprint ceilings
/// </summary>
class VariableBudget {

    internal const int MinMeasureInterval = 256;

    readonly IVariableProvider root;
    readonly long? maxEntries;
    readonly long? maxBytes;

    long producedSinceLastPass;
    long ticks;
    long nextMeasureAt;
    long allocatedAtLastPass;

    /// <summary>
    /// creates a new <see cref="VariableBudget"/>
    /// </summary>
    /// <param name="root">host-supplied root scope excluded from every measurement walk</param>
    /// <param name="maxEntries">maximum number of live variable entries allowed, or <c>null</c> for no entry-count ceiling</param>
    /// <param name="maxBytes">maximum approximated footprint in bytes allowed, or <c>null</c> for no footprint ceiling</param>
    public VariableBudget(IVariableProvider root, long? maxEntries, long? maxBytes) {
        this.root = root;
        this.maxEntries = maxEntries;
        this.maxBytes = maxBytes;
        nextMeasureAt = MinMeasureInterval;
        if (maxBytes.HasValue)
            allocatedAtLastPass = CurrentAllocatedBytes();
    }

#if NET8_0_OR_GREATER
    static long CurrentAllocatedBytes() => GC.GetTotalAllocatedBytes(false);
#else
    static long CurrentAllocatedBytes() => GC.GetTotalMemory(false);
#endif

    /// <summary>
    /// charges a produced value against the configured byte ceiling
    /// </summary>
    /// <param name="value">value produced by the operation or assignment</param>
    /// <param name="scope">current scope, walked if the accrued charge forces an immediate measurement pass</param>
    public void ChargeProducedValue(object value, IVariableProvider scope) {
        if (!maxBytes.HasValue)
            return;

        long size = VariableSizer.Size(value);
        if (size > maxBytes.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Bytes, maxBytes.Value, size);

        if (Interlocked.Add(ref producedSinceLastPass, size) >= maxBytes.Value)
            Measure(scope);
    }

    /// <summary>
    /// charges the byte cost of a capacity a script-reachable operation is about to allocate, before the
    /// allocation runs, throwing on a breach instead of letting the allocation happen (design §8.8.5)
    /// </summary>
    /// <param name="projectedBytes">bytes the pending capacity ctor/method/setter is about to allocate</param>
    public void ChargePreAllocation(long projectedBytes) {
        if (!maxBytes.HasValue || projectedBytes <= 0)
            return;

        if (projectedBytes > maxBytes.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Bytes, maxBytes.Value, projectedBytes);

        long projectedTotal = Interlocked.Read(ref producedSinceLastPass) + projectedBytes;
        if (projectedTotal > maxBytes.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Bytes, maxBytes.Value, projectedTotal);

        Interlocked.Add(ref producedSinceLastPass, projectedBytes);
    }

    /// <summary>
    /// samples variable usage at the configured tick cadence or once allocation since the last pass has grown by <c>maxBytes</c>, throwing on a breach
    /// </summary>
    /// <param name="scope">current scope to measure from</param>
    public void Observe(IVariableProvider scope) {
        long tick = Interlocked.Increment(ref ticks);
        if (tick >= Interlocked.Read(ref nextMeasureAt)) {
            Measure(scope);
            return;
        }

        if (maxBytes.HasValue && CurrentAllocatedBytes() - Interlocked.Read(ref allocatedAtLastPass) >= maxBytes.Value)
            Measure(scope);
    }

    void Measure(IVariableProvider scope) {
        long entries = 0;
        long bytes = 0;
        long units = 0;

        try {
            for (VariableProvider provider = scope as VariableProvider;
                 provider != null && !ReferenceEquals(provider, root);
                 provider = provider.Parent as VariableProvider) {
                foreach (KeyValuePair<string, object> entry in provider.LocalValues) {
                    entries++;
                    units++;
                    if (maxBytes.HasValue)
                        bytes += VariableSizer.Size(entry.Value, ref units);
                }
            }
        }
        catch (InvalidOperationException) {
            Interlocked.Exchange(ref nextMeasureAt, Interlocked.Read(ref ticks) + MinMeasureInterval);
            if (maxBytes.HasValue)
                Interlocked.Exchange(ref allocatedAtLastPass, CurrentAllocatedBytes());
            return;
        }

        Interlocked.Exchange(ref producedSinceLastPass, 0);
        Interlocked.Exchange(ref nextMeasureAt, Interlocked.Read(ref ticks) + Math.Max(MinMeasureInterval, units));
        if (maxBytes.HasValue)
            Interlocked.Exchange(ref allocatedAtLastPass, CurrentAllocatedBytes());

        if (maxEntries.HasValue && entries > maxEntries.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Entries, maxEntries.Value, entries);
        if (maxBytes.HasValue && bytes > maxBytes.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Bytes, maxBytes.Value, bytes);
    }
}
