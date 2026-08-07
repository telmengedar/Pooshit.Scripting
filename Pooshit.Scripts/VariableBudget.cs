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
    }

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
    /// samples variable usage at the configured cadence, throwing on a breach
    /// </summary>
    /// <param name="scope">current scope to measure from</param>
    public void Observe(IVariableProvider scope) {
        long tick = Interlocked.Increment(ref ticks);
        if (tick < Interlocked.Read(ref nextMeasureAt))
            return;

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
            return;
        }

        Interlocked.Exchange(ref producedSinceLastPass, 0);
        Interlocked.Exchange(ref nextMeasureAt, Interlocked.Read(ref ticks) + Math.Max(MinMeasureInterval, units));

        if (maxEntries.HasValue && entries > maxEntries.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Entries, maxEntries.Value, entries);
        if (maxBytes.HasValue && bytes > maxBytes.Value)
            throw new ScriptVariableLimitExceededException(VariableLimitKind.Bytes, maxBytes.Value, bytes);
    }
}
