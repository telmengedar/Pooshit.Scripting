# Architectural Document: A `VariableBudget` measurement cadence a doubling primitive cannot outrun

**Task:** DiVoid #7877 · **Found by:** round-8 adversarial enumeration #7868 (F1) · **Bypass it contributed to:** #7870 · **Guard model:** #7732 · **M4 pre-allocation charge:** #7736 §8.8 (`execution-guards-depth-memory.md`) · **Adjacent design in flight:** #7872 (`reflected-method-guard.md`, OQ-5 / residual 4).

**Load-bearing contracts:** Code Contracts **#114** (§0 principles, §4 comments, §13 testing, and the 2026-08-08 rulings on `<remarks>`, `[Description]`, `InternalsVisibleTo` and shared fixture state) and Design Contracts **#1136** (§1 KISS/DRY/YAGNI, §3 configurability, §4 less-is-better, §5 Pre-Design Checklist, answered in order as §14 of this document).

---

## 0. The operator's framing — verbatim

There is no user quote for this item; it was found by red-team enumeration, not reported. The operator's framing is the requirement:

> The cadence must not be beatable by construction. Any operation that can grow the live set super-linearly per charge currently outruns the checkpoint, and the next such primitive will not have a design in flight to catch it.

Two words in that sentence set the acceptance bar and are treated as binding throughout: **"by construction"** (the argument must be structural, not a measured margin) and **"the next such primitive"** (the design must not enumerate primitives).

---

## 1. Problem Statement

`VariableBudget.Observe` (`VariableBudget.cs:77`) is M3 — the sampled scope walk, the fourth line of `ScriptContext.Guard()`. It counts checkpoints in `ticks` and runs `Measure` when `ticks` reaches `nextMeasureAt`. `Measure` (`VariableBudget.cs:85`) walks the live scope chain, sums `VariableSizer.Size` into `bytes`, counts visited nodes into `units`, and then sets the next appointment:

```
VariableBudget.cs:108   nextMeasureAt = ticks + Math.Max(MinMeasureInterval, units)
```

**The interval to the next measurement grows with the size of the thing being measured.** That is a cost heuristic — a walk costing `O(units)` performed every `units` checkpoints is `O(1)` amortised per checkpoint — and as a cost heuristic it is correct. As a *safety* cadence it is unsound, because the quantity it defers on (`units`, the size already seen) is under the adversary's control and the growth that happens during the deferral is not bounded by it.

Formally: let `L` be the live-set size at a measurement. The next measurement is `L` checkpoints away. An operation that multiplies the live set by `f > 1` per checkpoint reaches `L · f^L` before the guard looks again. The overshoot is **exponential in the ceiling itself**, so no choice of the constant `MinMeasureInterval` and no cap on the interval changes the shape of the failure.

Measured instance (#7870, `master` @ `578a2f3`, bare parser, `MaxVariableBytes = 128 MiB`):

```
$l=new list(); $l.add(1); $i=0; while($i<26) { $l.addrange($l); $i=$i+1 } $l.count
→ COMPLETED -> 67108864 | managedLive=768MiB | no exception
```

`$l`'s `VariableSizer.Size` is `24 + 67108864 × 8` = **537 MB against a 128 MB ceiling, and the script returns success.** The linear-growth control to the same size throws correctly, so the ceiling was right and the cadence is what failed.

**One arithmetic fact fixes the shape of the defect and disposes of two of the three candidate directions before any design work.** `While.cs:28` guards once per iteration and `StatementBlock.cs:54` guards once per statement, so that payload's two-statement body costs ~3 checkpoints per iteration: **~80 checkpoints in total, against an initial `nextMeasureAt` of `MinMeasureInterval = 256`.** The measured bypass is not a case of the interval having grown too large — **the first measurement never happens at all.** Capping the interval, or removing the `units` term from it, would not have changed the outcome by a single byte.

### Success criterion

A script's retained footprint cannot exceed a small, fixed multiple of `MaxVariableBytes` without `ScriptVariableLimitExceededException(Kind = Bytes)` being raised, **for any growth rate whatsoever** — and the argument for that must be a property of the mechanism, not a measurement of how fast today's primitives happen to grow.

---

## 2. Scope & Non-Scope

### In scope

| | |
|---|---|
| The M3 sampling cadence in `VariableBudget.Observe` / `Measure` | the whole subject |
| The gate that keeps unconfigured hosts at zero cost | confirmed, §7 |
| One regression test that fails against today's `max(256, units)` and pins the cadence rather than an allow-list | §10 |
| The cost measurement John must take, and the result that falsifies this design | §8 |
| Composition with M1/M2/M4 and with #7872's charge sites | §9 |

### Explicitly out of scope

- **`AddRange` / `InsertRange` specifically.** #7870 had three independent causes; #7872 closes two of them and #7870 closes either way. This design must not be judged by whether it closes `AddRange` — it closes the class `AddRange` belonged to.
- **`VariableSizer`'s approximation.** The sizer's accuracy (opaque objects at a flat 64 bytes, `MaxSizeDepth = 4`, no identity tracking so aliases double-count) is unchanged and not re-litigated.
- **Line 108's `Math.Max(MinMeasureInterval, units)` term itself.** §5.3 decides deliberately to leave it, with the reasoning stated rather than assumed.
- **Peak allocation inside a single checkpoint.** `$s = $s + $s` doubles within one statement; M1 catches that exactly, and everything the engine cannot see inside one host call remains as #7732 documents it.
- **#7869 (parse recursion), #7871 (`RegexTimeout`), #7868 F6 (`MaxSteps`/`Timeout` defaults), the compiled path.** Different mechanisms, routed separately.
- **The pre-existing `[assembly: InternalsVisibleTo("Scripting.Tests")]`** in `Pooshit.Scripts/AssemblyInfo.cs`. It predates the 2026-08-08 ruling and is debt. This design does not remove it and — importantly — **does not add a test that depends on it** (§10.4).

---

## 3. Assumptions & Constraints

Every row verified against the working tree at `master` @ `578a2f3` unless marked.

| # | Assumption | Verified where |
|---|---|---|
| A1 | `VariableBudget` is allocated only when `MaxVariables` or `MaxVariableBytes` is configured | `GuardedExecution.cs:54-56` |
| A2 | `Guard()` reaches `Observe` through a null-conditional, so an unconfigured host pays one null check | `ScriptContext.cs:149` |
| A3 | `Observe`'s only inputs today are the tick counter and `nextMeasureAt`; it has no growth signal of any kind | `VariableBudget.cs:77-83` |
| A4 | `Measure` resets `producedSinceLastPass` and re-appoints `nextMeasureAt`; on a concurrent scope modification it backs off by `MinMeasureInterval` and returns without resetting the produced counter | `VariableBudget.cs:102-108` |
| A5 | M2's forced-measurement threshold is already `maxBytes.Value` — the exact quantity this design reuses | `VariableBudget.cs:50` |
| A6 | `While` guards once per iteration, `StatementBlock` once per statement | `While.cs:28`, `StatementBlock.cs:54` |
| A7 | The library multi-targets **`netstandard2.0;net8.0`** | `Pooshit.Scripting.csproj` |
| A8 | Conditional compilation per TFM is already used in this library | `IsExternalInit.cs:1` (`#if NETSTANDARD2_0`) |
| A9 | `GC.GetTotalAllocatedBytes(bool)` is **.NET Core 3.0+ / not in netstandard2.0**; `GC.GetTotalMemory(bool)` and `GC.CollectionCount(int)` are in both | BCL surface; **John must confirm at build time** |
| A10 | `VariableBudget` is shared by reference across contexts and across `task.run`, and is **not** inherited across `import` | #7732, `GuardedExecution.cs:51-56` |
| A11 | `units` is bounded below by bytes: the densest shape the sizer can produce is ~8 bytes per visited node (a collection of nulls), so a passing measurement implies `units ≲ maxBytes / 8` | `VariableSizer.SizeCore`, `CollectionElementOverhead = 8` |
| A12 | The measured calibration precedent is `DeadlineGuard.Check()` in `Guard()` at **~22 ns/checkpoint**, judged immaterial (10M checkpoints, 5 trials) | `ScriptContext.cs:145`, prior measurement |

---

## 4. Why the three named candidate directions do not work

#7877 names three directions. Two are refuted structurally; the third is right in kind and wrong in instrument.

### 4.1 "The interval stops scaling with the charged amount" — insufficient

Fix the interval at a constant `k`. A primitive that doubles per checkpoint reaches `2^k` times the live set before the guard looks. At `k = MinMeasureInterval = 256` that is `2^256`. The measured payload needed **26** doublings and ~80 checkpoints; it does not even reach the first appointment. **Removing the `units` term changes nothing about this payload and nothing about the shape of the failure.**

### 4.2 "The interval is capped" — the same refutation

A cap is a constant interval from the adversary's side. Same `2^k`. Any tick-denominated interval `k ≥ 2` is beatable; only `k = 1` bounds overshoot to a single doubling, and `k = 1` means a full `O(units)` scope walk at every statement — a quadratic engine.

**This is the decisive point of the whole design:** the failure is not that the interval is *too long*. It is that the interval is denominated in **checkpoints** while the threat is denominated in **bytes**, and the adversary chooses the exchange rate. No value of a checkpoint-denominated interval is safe, because the adversary picks how many bytes a checkpoint is worth.

### 4.3 "Measurement triggers on a growth ratio rather than an absolute tick budget" — right in kind

Correct in kind: the trigger must be denominated in the same currency as the threat. But the literal reading — trigger on the ratio of *measured footprint* to the ceiling — costs more than it buys. Setting `interval = max(1, floor(log2(maxBytes / bytes)))` is exactly "the number of doublings of headroom remaining", and it is structurally sound; but as `bytes → maxBytes` the interval collapses to 1 and every checkpoint pays a full `O(units)` walk. A script legitimately holding 100 MB under a 128 MiB ceiling and then running a million cheap statements becomes quadratic. **A guard whose cost explodes exactly where scripts legitimately live is not shippable.** Rejected, on record (§11).

The correct instrument is a growth signal that is **`O(1)` to sample** and that the adversary cannot decouple from the growth. §5 identifies it.

---

## 5. Architectural Overview — the decision

### 5.1 The one-sentence design

**`Observe` gains a second, growth-denominated trigger: it forces a `Measure` when the process's monotonic allocation counter has advanced by `maxBytes` since the last measurement — regardless of how many checkpoints have passed.** The existing tick cadence stays exactly as it is, demoted from a safety mechanism to what it always actually was: a cost heuristic and a backstop for growth that does not allocate.

```
                       Guard()  (per statement / iteration / element / callback)
                          │
                          └─► VariableBudget.Observe(scope)
                                   │
                                   ├─ tick trigger  : ticks >= nextMeasureAt        [unchanged]
                                   │                  cost amortisation; backstop for
                                   │                  non-allocating measured growth
                                   │
                                   └─ growth trigger: allocated() - allocatedAtLastPass
                                                      >= maxBytes                    [NEW]
                                                      the un-outrunnable one
                                   │
                                   ▼
                            Measure(scope)  ── resets producedSinceLastPass,
                                                nextMeasureAt, allocatedAtLastPass
                                                throws on breach
```

### 5.2 Why it cannot be outrun — the structural argument

Stated as the operator asked, as a property of the mechanism:

> **A doubling primitive cannot get past this, because doubling the retained set requires allocating the doubled amount, and the counter the trigger reads is incremented by the allocator itself, monotonically, before the memory it counts can become reachable from any script variable.**

Unpacked into the four claims it rests on, each independently checkable:

1. **Retention implies allocation.** For a script's measured footprint to grow by `ΔB` bytes, `ΔB` bytes of objects must come into existence. There is no path — no primitive, present or future, built-in or host-registered — by which a script's live set grows in bytes without the runtime allocating those bytes. This is a property of the CLR, not of this engine's surface, which is precisely why it does not enumerate primitives.
2. **Allocation is counted, monotonically, before the fact.** The runtime's cumulative-allocated-bytes counter is advanced by the allocation path itself. It never decreases; a garbage collection does not lower it. So between any two checkpoints, the counter's advance is an upper bound on nothing and a **lower bound on the retained growth** in that window.
3. **Therefore the unmeasured window is bounded in bytes, not in checkpoints.** The trigger fires at the first checkpoint after the counter has advanced by `maxBytes`. The growth rate per checkpoint is irrelevant to when it fires, because it is keyed on the growth itself rather than on a proxy for it. A primitive that doubles, triples, or grows by any function whatsoever trips it at the same *byte* position.
4. **So the overshoot is additive, not multiplicative.** At the moment the forced `Measure` runs, the live set is at most

   `(footprint at the last measurement, which was ≤ maxBytes or it would have thrown) + maxBytes + (growth of the single operation in flight)`

   The first two terms give **≤ 2 × maxBytes**. The third term is the pre-existing, documented single-checkpoint residual (#7732: *"the engine guarantees nothing inside a single host call"*), which this design neither widens nor repairs.

**The bound is `2 × maxBytes` plus one operation — the same guarantee `$s = $s + $s` already gets from M1**, which round-8 measured and passed (`transient peak bounded to ~2× the limit`). That is not a coincidence and it is the right target: it makes the sampled walk as strong as the exact per-value ceiling, so the guard has one guarantee instead of two.

**The one thing this bound is about, said precisely:** it bounds **retained bytes**, which is what `MaxVariableBytes` exists to bound (OOM protection). It does **not** bound `VariableSizer`'s *approximation* when that approximation grows without allocation — aliasing and self-reference (`$l.add($l)`, `$a = $l`) inflate the measured number while the process holds the same memory. That is the correct behaviour (the guard should not abort a script that is not using the memory), the assignment form is charged exactly by M1 anyway, and the tick cadence remains the backstop for the rest. §5.3 is where that backstop earns its keep.

### 5.3 What happens to line 108 — decided deliberately, not skipped

**`nextMeasureAt = ticks + Math.Max(MinMeasureInterval, units)` stays exactly as it is.** This is the KISS answer and the delete-check (#1136 §4) run in both directions:

- **Can the `units` term be deleted?** Its safety relevance is now nil — the growth trigger fires on any *allocating* growth independent of ticks, so the tick term can only ever schedule a measurement *earlier or later than one that is going to be forced anyway*. Its cost relevance is high: it is what keeps M3 amortised `O(1)` per checkpoint for a script with a large live set. Deleting it would make every 256th checkpoint pay a full walk of a large scope, for zero safety gain. **Keep.**
- **Can the whole tick cadence be deleted?** No. It is the only mechanism that catches measured growth which *does not allocate* — the alias/self-reference shapes of §5.2. Bounded and linear (one alias per statement), so a ≥256-checkpoint window is proportionate. **Keep.**
- **Does leaving line 108 contradict the task?** The task named line 108 as the defect site. It is the *symptom* site: the defect is that `Observe` had only one trigger and it was denominated in the wrong currency. Changing line 108 without adding a growth trigger fixes nothing (§4.1, and the ~80-checkpoint arithmetic in §1 proves it against the actual measured payload). Adding the growth trigger makes line 108 provably safety-neutral. Changing it *as well* would cost performance and buy nothing — the definition of a change that fails the delete-check.

If the operator wants line 108 changed regardless, that is a decision to make explicitly and not one this design should smuggle in; it is **OQ-1**.

### 5.4 Why the threshold is `maxBytes` and not a new number

`maxBytes` is reused, not chosen (#1136 §3 — no new magic number, and #114 §0 DRY at the constant level):

- **It is already the threshold of the sibling trigger.** M2 forces a measurement at `producedSinceLastPass >= maxBytes.Value` (`VariableBudget.cs:50`). The two triggers now read the same threshold from the same field for the same reason: *"one budget's worth of unaccounted growth is as much as the guard is willing to not have looked at."*
- **It is what produces the `2 ×` bound** in §5.2 claim 4, matching the guarantee M1 already delivers.
- **It self-amortises across every budget size.** Walk cost is `O(units)`, and by A11 a still-passing measurement implies `units ≲ maxBytes / 8`. The trigger period is `maxBytes` bytes of allocation. So the worst-case ratio is **one node-visit per 8 bytes allocated, independent of how `MaxVariableBytes` is configured** — a tiny 64 KB budget walks a tiny scope proportionally often; a 128 MiB budget walks a large scope proportionally rarely. There is no cliff at either end. **This is the same worst-case measurement frequency M2 has had since PR #15**, so the design introduces no new cost shape, only a new trigger into an existing one.

Rejected alternative — a headroom-scaled threshold (`maxBytes - lastMeasuredBytes`), which would tighten the bound from `2×` to `~1×`: it reintroduces a cliff (headroom → 0 means a walk per checkpoint for an allocating near-ceiling script), needs a floor constant to avoid it, and buys a factor of two on a bound the engine already accepts elsewhere. Rejected on §4/#1136 §4.

### 5.5 The one honest wrinkle — the counter is not available on both TFMs

The design needs one primitive: **a monotonic count of bytes allocated.**

| TFM | Instrument | Guarantee |
|---|---|---|
| **net8.0** | `GC.GetTotalAllocatedBytes(false)` — process-wide, monotonic, `precise: false` so it does not suspend threads | **Full.** §5.2 holds verbatim. |
| **netstandard2.0** | `GC.GetTotalMemory(false)` — current managed heap size | **Weaker.** Non-monotonic; a collection lowers it, so a script that first inflates the heap with garbage and then grows its live set into the space the collector frees can suppress the trigger. |

This is a real difference in a security-relevant guarantee and is reported as such rather than papered over. Three facts bound how much it matters:

1. **NuGet resolves the best TFM**, so any consumer on .NET Core 3.0+ / .NET 5+ gets the net8.0 asset and the full guarantee. The netstandard2.0 asset is reached only by .NET Framework 4.6.1–4.8 and .NET Core 2.x hosts.
2. On those hosts the netstandard2.0 path is still **strictly better than today** (today there is no growth trigger at all).
3. The exact instrument on .NET Framework would be `AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize`, which is monotonic — but it is not in the netstandard2.0 reference surface and reaching it needs reflection. Rejected (#1136 §1).

**Decision: implement both, behind one `#if NET8_0_OR_GREATER` in one private accessor** (precedent A8), and document the guarantee difference in `execution-guards-depth-memory.md` §8 and in the language reference's residual paragraph. Whether to instead accept a netstandard2.0 TFM bump, or to leave netstandard2.0 on today's cadence entirely, is **OQ-2** — it is a packaging decision, not an architectural one, and it belongs to the operator.

Also considered and rejected for netstandard2.0: `GC.CollectionCount(0)` as a monotonic proxy (available on both TFMs and genuinely monotonic, but the gen-0 budget it implies is runtime- and GC-mode-dependent and can be hundreds of MB under Server GC — an unknown constant inside a security bound is worse than a known weakness), and accumulating `Σ max(0, heapNow − heapPrev)` into a synthetic monotonic counter (still suppressible by the same garbage-replacement argument, at the cost of an extra field and an extra sample per checkpoint).

---

## 6. Components & Responsibilities

Exactly one type changes. No new type, no new file in the production assembly.

| Component | Owns after this change | Explicitly does **not** own |
|---|---|---|
| `VariableBudget` (`Pooshit.Scripts/VariableBudget.cs`) | *when* to measure — now from **two** independent triggers, one tick-denominated and one byte-denominated — and the baselines both are reset from | *what* a value costs (`VariableSizer`), *where* checkpoints happen (`ScriptContext.Guard` and its callers), *whether* a budget exists at all (`GuardedExecution.Prepare`) |
| `VariableSizer` | unchanged | — |
| `ScriptContext.Guard` | unchanged | — |
| `GuardedExecution.Prepare` | unchanged | — |
| `ScriptLimits` | unchanged — **no new knob** (§7, #1136 §3) | — |

### The change, described as responsibilities rather than code

1. **A new instance field, `allocatedAtLastPass` (long)** — the allocation-counter reading taken at the last completed measurement. Same lifetime and same thread-safety treatment (`Interlocked`) as `producedSinceLastPass` and `nextMeasureAt`, for the same reason (A10: the budget is shared by reference across contexts and across `task.run`).
2. **A new private static accessor returning the current monotonic allocation reading.** One method, one `#if NET8_0_OR_GREATER`, no other conditional compilation anywhere. Named for what it returns, not for the API it wraps.
3. **The constructor seeds `allocatedAtLastPass`** with a reading when `maxBytes.HasValue`, so the first delta is measured from the start of the run rather than from process start.
4. **`Observe` gains the second trigger**, evaluated only when `maxBytes.HasValue`, after the existing tick check and only if the tick check did not already fire.
5. **`Measure` re-seeds `allocatedAtLastPass`** on the success path, alongside its existing resets of `producedSinceLastPass` and `nextMeasureAt`.
6. **`Measure` also re-seeds `allocatedAtLastPass` on the `InvalidOperationException` back-off path** (`VariableBudget.cs:102-105`). This is deliberate and is the one non-obvious implementation point: that path exists because the scope chain was concurrently modified and the walk could not be completed. Without re-seeding, the growth trigger would refire at the very next checkpoint and keep refiring, converting a rare failed walk into a walk attempt per checkpoint. Re-seeding mirrors exactly what the existing line does for `nextMeasureAt` — a bounded back-off on a best-effort path — and the missed growth is picked up by the next window.

---

## 7. Unconfigured hosts pay nothing — confirmed, not changed

The existing gate pattern is **confirmed and reused unchanged**; no host's cost profile changes except the one that has already asked for the guard.

| Host configuration | Pays |
|---|---|
| `ScriptLimits.None` (both `MaxVariables` and `MaxVariableBytes` null) | **Nothing.** `GuardedExecution.Prepare` allocates no `VariableBudget` (A1), so `Guard()`'s `VariableBudget?.Observe(...)` is a null check (A2) and the accessor is never reached. |
| `MaxVariables` set, `MaxVariableBytes` null | **Nothing new.** The trigger's threshold is derived from `maxBytes`, so it is gated on `maxBytes.HasValue` — the same gate `ChargeProducedValue` (`:43`) and `ChargePreAllocation` (`:60`) already use. The entry-count guard is untouched. |
| `MaxVariableBytes` set (including `ScriptLimits.Default`, i.e. the secure-by-default profile since PR #16) | **One counter read, one subtract and one compare per checkpoint.** This is the cost §8 requires John to measure. |

The third row is the honest statement of the bill: since 1.0 flipped `MaxVariableBytes` on by default, *most* hosts land there. That is exactly why the cost question is gating rather than advisory, and why §8 states a falsifier rather than a hope.

**No `ScriptLimits` knob is added.** The threshold is `maxBytes` itself; there is no second number, so there is nothing to tune, no named operator who would tune it, and no telemetry-then-tune compound (#1136 §3).

---

## 8. What it costs — the measurement John must take, and what falsifies the design

The trigger must be evaluated at **every** checkpoint. Sampling it every *N*th checkpoint would reintroduce a tick-denominated term into the bound (`maxBytes` + *N* checkpoints of growth = `2^N` again) and would forfeit the entire argument of §5.2. So the design lives or dies on one number.

### M1 — the per-checkpoint cost (gating)

**Shape:** the `DeadlineGuard` precedent verbatim, so the numbers are comparable. A tight `while` loop with a trivial body, **10M checkpoints, 5 trials**, Release, net8.0. Three arms:

| Arm | `Limits` | Measures |
|---|---|---|
| a | `ScriptLimits.None` | baseline; must be **unchanged** from `master` (this is the §7 row-1 pin) |
| b | `MaxVariableBytes` set, on `master` | today's cost of a configured budget |
| c | `MaxVariableBytes` set, with this change | b + the counter read |

**Report:** `(c − b)` in nanoseconds per checkpoint, and `(a_after − a_before)` which must be zero within noise.

**Falsifier — stated as a result, not a feeling:** if `(c − b)` exceeds **~22 ns/checkpoint** — the `DeadlineGuard` figure already judged immaterial for configured hosts — **this design is wrong and must come back to the architect, not be patched down.** The only patch available is sampling the counter less often, and §8's opening paragraph explains why that patch destroys the guarantee this design exists to provide. The correct response to a failed M1 is to re-open the choice of instrument (a thread-local counter with an explicit re-baseline on thread change is the first fallback to price), not to weaken the cadence.

Expected, for calibration only — **not** a substitute for measuring: on net8.0 `GC.GetTotalAllocatedBytes(false)` is an FCALL that does not suspend threads and should land well under 22 ns. If it does not, that is a finding worth having.

### M2 — the walk-frequency regression (informational, not gating)

**Shape:** two scripts under a 128 MiB budget, wall-clock, `master` vs. this change.

| Script | Expectation |
|---|---|
| **Near-ceiling, idle:** builds ~100 MB of live list, then runs 1M cheap statements that allocate almost nothing | **No regression.** The trigger is allocation-denominated, so a script that holds memory without churning it never fires it. This is the shape the rejected §4.3 log-cadence would have made quadratic, and pinning it is how we show this design does not. |
| **Near-ceiling, churning:** the same ~100 MB live set plus a loop producing ~100 MB of short-lived strings | Some regression is *expected and correct* — this is the regime the guard is for. |

**Falsifier for the threshold choice (not for the design):** if the churning shape regresses by more than ~2×, the `maxBytes` threshold is too tight and the alternative is a larger multiple of it — which weakens the §5.2 bound proportionally and is therefore an explicit, arguable trade to bring back, never a silent constant edit.

### M3 — the netstandard2.0 build (gating, trivial)

Confirm A9 at build time: that `GC.GetTotalAllocatedBytes` genuinely does not resolve under `netstandard2.0` and genuinely does under `net8.0`, so the `#if` is load-bearing rather than decorative. If it resolves on both, **delete the `#if` and the fallback entirely** — the weaker guarantee of §5.5 disappears and so does OQ-2.

---

## 9. Interaction with M4 and with #7872 — stated, not left implicit

### 9.1 M4 (`ChargePreAllocation`) — they compose; neither subsumes the other

| | M4 | This design |
|---|---|---|
| When | **before** the allocation | at the first checkpoint **after** it |
| Input | a projected size read from the call site (an `int` argument, an argument's `ICollection.Count`, receiver/argument lengths) | the runtime's own allocation counter |
| Precision | exact for the shapes it can project | exact in bytes allocated, approximate in what those bytes are |
| Effect | the allocation **never happens** — round-8 measured `list.capacity=1e8` throwing at `allocDelta = 0.0 MiB` | the allocation has happened; the guard aborts before the *next* one |
| Coverage | only shapes present in the pre-allocation table | **everything that allocates**, table or no table |

**Neither is redundant.** M4 buys the one thing this design structurally cannot — refusing an allocation that would OOM the process in a single call, before it runs. This design buys the one thing M4 structurally cannot — coverage of shapes nobody has enumerated. They are the two halves of the same guarantee and the composition is the point.

**No double counting.** M4 adds its projection to `producedSinceLastPass`; this design reads a **separate** counter and does not feed `producedSinceLastPass`. The same bytes can trip both M2 and the growth trigger, but both triggers do the identical thing — force one `Measure` — so the cost of a coincidence is at most one extra walk, and `Measure` resets both baselines. §8.8.5's charge-once reconciliation is untouched: the growth trigger never charges anything, it only schedules.

### 9.2 #7872 (the reflected-method guard) — orthogonal, and #7872 says so itself

#7872 §12 residual 4 and OQ-5 both record that #7872 deliberately does **not** repair this cadence, and recommend exactly this split. That reading is confirmed here from the other side:

- **Different mechanism.** #7872 adds charge sites and a `MethodGuard`; all of its rows funnel into `ChargePreAllocation` or into a refusal. It is a wider M4. This design touches only *when the sampled walk runs*.
- **Different file surface.** #7872 edits `VariableSizer`, `MethodOperations`, `MethodResolver`, `ScriptMember` and adds `MethodGuard`. This edits `VariableBudget` only. The single overlap is that #7872 §17 adds a `VariableBudget` method (A23) — a **different member** from the ones this design touches, so whichever PR lands second rebases cleanly. **No merge-order dependency in either direction.**
- **Complementary coverage, and this is the load-bearing sentence for the operator:** #7872 closes the *reachable BCL* surface by curation, and by its own §12 residuals 10–12 it **cannot** close the host-registered surface — a zero-argument host member that allocates, or a host member that mutates a collection it was handed or is holding, is invisible to H1/H2/H3 by construction. That is not a gap in #7872; it is the permanent shape of a host-extensible engine. **The growth trigger is the only mechanism in the engine that covers it**, because it does not need to recognise the member at all. §10 turns exactly that into the regression test.

---

## 10. The regression test — specified

### 10.1 The requirement, and why the obvious test is disqualified

The test must fail against today's `max(256, units)`. The #7870 doubling loop is the natural shape, but `$l.addrange($l)` is disqualified: **it is charged today by nothing and charged tomorrow by #7872 §8.1 row 7**, so a test written on it would go green on the day #7872 merges, for the wrong reason, and would pin the allow-list rather than the cadence. The same disqualifies every built-in amplifier in round-8's inventory — after #7872 they are all either charged rows or refused members.

### 10.2 The primitive — named, and named as permanently uncharged

**A test-local host type whose method grows a script-held `List<object>` passed to it as an argument.** Registered on the parser's type provider (or supplied through the host root variable set), with a single method of the shape `void Grow(List<object> target)` whose body doubles the list in host C#.

Why it is not charged, now or after #7872 — four independent reasons, any one sufficient:

1. **It returns void**, so `ChargeProducedValue` (M1) never runs — the same reason `AddRange` escaped in #7870.
2. **It is not in the M4 pre-allocation table**, and cannot be: the table keys on `List<>`/`Dictionary<,>` capacity operations.
3. **#7872's tier-1 rows do not match.** Every row projects from an *integral* argument, a *string length*, or the `ICollection.Count` of an argument **being added by the script's own call**. Here the script's call adds nothing; the host mutates a reference it was handed.
4. **#7872's tier-2 shape rules do not match.** H1 needs `IFormattable`; H2 needs an integral argument; H3 permits by shape. A method taking a collection reference and returning void is permitted and unprojectable — #7872 §12 residuals 10 and 12, named there as permanent.

This is not a contrivance to dodge the guard. It is the ordinary shape of a host-registered API, and it is the exact class the operator's framing points at: *"the next such primitive will not have a design in flight to catch it."* **A built-in primitive that satisfies the requirement no longer exists after #7872 — the host surface is where this class permanently lives, so that is where the test must drive it.**

The growth also genuinely **allocates** (the host's own list growth), which matters: a primitive that inflated only `VariableSizer`'s approximation without allocating would not be caught by this design and, per §5.2, correctly should not be.

### 10.3 The test — shape and the arithmetic that isolates it

- **Fixture:** `Scripting.Tests/ExecutionGuardTests.cs`, alongside the existing `Variable_*` tests. `[Test, Parallelizable, MaxTime(2000)]`, matching the file.
- **The host type lives in its own file** under `Scripting.Tests/` (#114 §1, one type per file — do not copy the existing in-fixture `OpaqueHostObject` shape, which is pre-existing debt).
- **No shared fixture state** (2026-08-08 ruling): the test constructs its own `ScriptParser`, registers its own host type, and holds every object locally. No `[SetUp]` field, no static.
- **Limits:** `MaxVariableBytes = 1_000_000`, nothing else set.
- **Script:** create a list with one element; loop a **finite** 20 iterations calling the host grower on it; the loop literal is bounded so a regression fails the assertion instead of OOMing the runner (the precedent this file already sets for depth tests).
- **Assert:** `ScriptVariableLimitExceededException` with `Kind == VariableLimitKind.Bytes`, and `Measured` less than a small multiple of the limit (16× is a robust choice — today's bypass overshoots by >500×, so the assertion discriminates decisively while tolerating the counter's ~8 KB-per-thread reporting granularity).

**The isolation arithmetic — this is what makes the test pin the cadence and nothing else.** Under those limits, in those 20 iterations:

| Mechanism | Would it fire? | Why not |
|---|---|---|
| M1 (per-value ceiling) | no | no produced value approaches 1 MB |
| M2 (`producedSinceLastPass >= maxBytes`) | no | the only produced values are `$i = $i + 1` and the loop comparison, ~48 bytes/iteration × 20 ≈ 960 bytes, against a 1,000,000-byte threshold |
| M4 / #7872 charge sites | no | §10.2, four reasons |
| **Tick cadence** (`max(256, units)`) | **no** | ~3 checkpoints/iteration (A6) × 20 + top-level ≈ **64 checkpoints, against an initial `nextMeasureAt` of 256** |
| **Growth trigger** | **yes** | the list's cumulative backing-array allocation crosses 1 MB around iteration 17 |

So on `master` the script **completes with no exception** while holding ~8 MB against a 1 MB ceiling, and after the change it throws. **Nothing else in the guard can be what fires.**

**Verification John must perform and report** (this file already sets the precedent — *"Verified by deliberately reverting the production change"*): revert the `Observe` change with the test in place and confirm it fails by **completing without throwing**, not by throwing something else. A test that goes red for a different reason has not pinned the cadence.

### 10.4 Supporting tests

| Test | Pins | Notes |
|---|---|---|
| **No false positive under churn** | a script under a small `MaxVariableBytes` that allocates far more than the budget in *transient* values while retaining almost nothing must still complete | proves the growth trigger schedules measurements and never itself throws |
| **Unconfigured host allocates no budget** | `GuardedExecution.Prepare(..., ScriptLimits.None)` yields a context whose `VariableBudget` is null | mirrors the existing `T31b` shape — **but see below** |
| **Entries-only host is unaffected** | `new ScriptLimits { MaxVariables = n }` still throws on entry count and never reaches the byte path | the `maxBytes.HasValue` gate of §7 row 2 |

**On the second row and `InternalsVisibleTo`:** the existing `T31b` reaches `execution.Context.DepthBudget`, an `internal` member, through the pre-existing `[assembly: InternalsVisibleTo("Scripting.Tests")]` in `AssemblyInfo.cs`. That attribute predates the 2026-08-08 ruling and is debt (§2). **John must not add a new test that depends on it.** Write the unconfigured-host pin behaviourally through the public surface — a `ScriptLimits.None` parser running a script that would breach a configured budget completes normally — and, if a stronger structural pin is wanted, file the `InternalsVisibleTo` removal as its own task rather than extending the debt here.

---

## 11. Alternatives considered and rejected — on record

| Alternative | Rejected because |
|---|---|
| Drop the `units` term from line 108 | Insufficient by construction — `2^k` overshoot for a constant interval `k`, and the measured payload never reaches the first appointment at all (§1, §4.1). |
| Cap `nextMeasureAt` at a constant | Same refutation; a cap *is* a constant interval (§4.2). |
| Trigger on the ratio of measured footprint to ceiling (`interval = log2(maxBytes / bytes)`) | Structurally sound but the interval collapses to 1 near the ceiling, making every checkpoint an `O(units)` walk. Quadratic for a script legitimately living near its budget (§4.3). |
| Charge growth at the dispatch site: read `ICollection.Count` on the receiver and on every collection argument before and after each reflected call | Cheap, exact, TFM-agnostic — and **not structural**. It enumerates mutation channels, and the enumeration escapes at the first host member that mutates a collection it is *holding* rather than one it was passed (`$h.setTarget($l); $h.grow()`). That is the same enumeration trap the task exists to escape. |
| Maintain a watch-list of the collections seen at the last `Measure` and re-read their `Count` each checkpoint | Three moving parts (weak references so the guard does not pin memory, an eviction policy, a bootstrap) and blind for every checkpoint before the first `Measure` — which is precisely the window the measured payload lives in. #1136 §1. |
| `GC.CollectionCount(0)` as the growth signal | Monotonic and available on both TFMs, but the implied gen-0 budget is runtime- and GC-mode-dependent (hundreds of MB under Server GC). An unknown constant inside a security bound. |
| A synthetic monotonic counter accumulating `max(0, heapNow − heapPrev)` on netstandard2.0 | Still suppressible by garbage-replacement, at the cost of an extra field and an extra sample per checkpoint — complexity for a guarantee that remains non-structural (§5.5). |
| Reflect for `AppDomain.MonitoringTotalAllocatedMemorySize` on netstandard2.0 | Reflection to reach a platform API the reference surface excludes. #1136 §1. |
| A headroom-scaled threshold (`maxBytes − lastMeasuredBytes`) | Tightens `2×` to `~1×` at the cost of a cliff near the ceiling and a new floor constant (§5.4). |
| A new `ScriptLimits` knob for the threshold | No named operator, no environment difference, and the correct value is already in the object (#1136 §3, §7). |

---

## 12. Risks & Mitigations

| # | Risk | Mitigation |
|---|---|---|
| R1 | **The counter read is more expensive than expected**, making every configured host pay per checkpoint | M1 in §8 is gating and has a stated falsifier. The failure mode is a re-opened design decision, explicitly **not** a reduced sampling rate. |
| R2 | **Host allocation on other threads inflates the process-wide delta**, forcing extra script measurements | Bounded and small: one extra walk per `maxBytes` of *host* allocation — 128 MiB by default. Named conservatism, over-measuring only ever costs time. |
| R3 | **The netstandard2.0 guarantee is weaker than the net8.0 one** (§5.5) | Documented in the design, in `execution-guards-depth-memory.md` §8 and in the language reference residual. Strictly better than today on that TFM. OQ-2 offers the operator the packaging alternatives. |
| R4 | **The counter's reporting granularity** (~8 KB per thread with `precise: false`) delays the trigger for a very small `MaxVariableBytes` | Additive and tiny; the bound becomes `2 × maxBytes + ~8 KB/thread`. The regression test is sized at 1 MB so granularity is 0.8% of the threshold and the assertion is a multiple, not an equality. |
| R5 | **The `InvalidOperationException` back-off path livelocks** into a walk attempt per checkpoint | §6 item 6 re-seeds the allocation baseline on that path, mirroring what the existing line already does for `nextMeasureAt`. Call it out in review — it is the one line an implementer would plausibly omit. |
| R6 | **A future reader assumes the tick cadence is a safety mechanism** and "simplifies" it away, or assumes the growth trigger covers alias growth | §5.3 records both directions of the delete-check; the doc update in unit 4 carries it into `execution-guards-depth-memory.md` so it survives outside this file. |
| R7 | **The regression test passes for the wrong reason** after #7872 merges | §10.2's four independent reasons, and §10.3's verification-by-revert. |

---

## 13. Implementation Guidance — ordered units

Design only. No production code, no branch, no PR from this document.

**Unit 1 — the growth trigger** (`Pooshit.Scripts/VariableBudget.cs`, single file)
1. Add the `allocatedAtLastPass` field, `Interlocked`-accessed like its siblings.
2. Add the private static allocation-reading accessor with the one `#if NET8_0_OR_GREATER` (§5.5). Name it for what it returns.
3. Seed the field in the constructor when `maxBytes.HasValue`.
4. Add the second trigger to `Observe`, gated on `maxBytes.HasValue`, evaluated after the tick check and only if the tick check did not fire.
5. Re-seed the field in `Measure` on **both** the success path and the `InvalidOperationException` back-off path (§6 item 6).
6. **Leave line 108 unchanged** (§5.3). If a reviewer asks why, the answer is §4.1's arithmetic, not taste.
7. XML `<summary>` on `Observe` updated to name both triggers — one tight line, *what it is*. **No `<remarks>`, no `//` anywhere** (#114 §4 and the 2026-08-08 ruling).

**Unit 2 — the measurements** (§8). M3 first (it is a build check and can delete the `#if`), then M1 (gating), then M2 (informational). **Report all three numbers in the PR body.** A failed M1 bounces to the architect.

**Unit 3 — the tests** (§10). The host grower type in its own file; the regression test; the three supporting pins; the verify-by-revert step, reported. `[Description]` on each is **one sentence** naming what the test pins and why anyone cares — no measurements, no harness paths, no history (2026-08-08 ruling).

**Unit 4 — the documentation** (same PR)
- `docs/architecture/execution-guards-depth-memory.md` §8: add the growth trigger to the M3 row of the mechanism table (§8.2.1) and record §5.3's finding that the tick cadence is a cost heuristic plus a non-allocating-growth backstop, not a safety cadence.
- `docs/architecture/reflected-method-guard.md` §12 residual 4 and OQ-5: mark as **owned by this design**, with the repo path.
- The language-reference residual paragraph: state the `2 ×` bound, and state the netstandard2.0 difference (§5.5) rather than claiming one guarantee for both TFMs.

**Do not**
- add a `ScriptLimits` knob, a threshold constant, or any tunable;
- change `VariableSizer`, `ScriptContext`, `GuardedExecution` or any charge site;
- add a `VariableBudget` unit test that reaches internals (§10.4);
- sample the trigger every *N*th checkpoint if M1 comes back high — bounce instead (§8);
- touch `docs/architecture/reflected-method-guard.md` beyond the two pointer edits in unit 4; #7872 is a separate PR by a separate design.

---

## 14. Pre-Design Checklist (#1136 §5) — answered in order

**KISS / DRY / YAGNI**

- **No new type mirroring an existing one.** No new type at all in the production assembly: one field, one private accessor, one branch, on the class that already owns the decision.
- **No abstraction with one implementation.** No interface, no provider, no strategy for the counter — one private static method with one `#if`.
- **No element justified by "we might need X later."** Every element is required by the measured #7870 bypass or by the §5.2 argument. The rejected list in §11 is where the speculative shapes went.
- **No deprecation period, feature flag, compatibility shim or transition window.** The trigger is on from the first commit for every host that has a byte budget.
- **DRY math.** No block is duplicated: the change is ~8 lines at **one** site, so `block_size × site_count` is not engaged (#1267 governs >5-line blocks at >2 sites). At the *constant* level DRY is actively satisfied — the threshold is `maxBytes.Value`, the same field M2 already reads at `VariableBudget.cs:50`, so no second number enters the file (#1136 §3).
- **Every principle-overriding decision states its math.** §4.1/§4.2 (`2^k`, and 80 checkpoints vs. a 256 floor), §5.2 (the `2 × maxBytes` bound), §5.4 (one node-visit per 8 bytes allocated, from A11), §10.3 (960 bytes of produced values vs. a 1,000,000-byte M2 threshold; 64 checkpoints vs. 256). No paraphrase carries a decision anywhere in this document.

**Existing systems first**

- **Audited before designing:** M1 and M2 (`ChargeProducedValue`, `VariableBudget.cs:42-52`), M3 (`Observe`/`Measure`), M4 (`ChargePreAllocation` + the `VariableSizer` table + the four charge sites), the `Guard()` spine, `GuardedExecution.Prepare`'s allocation gate, `ScriptLimits`.
- **No new layer.** The design is a second trigger inside the existing sampling mechanism, deliberately shaped after M2 — same threshold, same effect, different signal. The alternative that *would* have been a new layer (a dispatch-site growth charge, or a collection watch-list) is priced and rejected in §11.
- **No new persisted data.** Library; no schema.
- **Consumer chain recursed.** `allocatedAtLastPass` has exactly one reader (the trigger) and two writers (constructor, `Measure`). Nothing is stored that nothing acts on.

**Configurability**

- **No new knob**, in `ScriptLimits` or anywhere. §7.
- **No telemetry-then-tune compound.** Nothing is recorded for later tuning.
- **No new magic number.** The threshold is an existing configured value; `MinMeasureInterval` is untouched.

**Less is better**

- **Delete-check run on every element**, including the two that were kept: §5.3 runs it in both directions on line 108 and on the tick cadence as a whole, and keeps them for stated reasons rather than by default.
- **Deleted from this design:** a threshold constant; a `ScriptLimits` knob; a headroom-scaled threshold; the log-headroom cadence; the dispatch-site growth charge; the collection watch-list; a synthetic netstandard2.0 counter; a `VariableBudget` unit test reaching internals; any change to line 108, to `VariableSizer`, or to any charge site.
- **Trade-offs named with their arithmetic:** §5.4 (2× vs. 1× and the cliff it would cost), §5.5 (two TFMs, two guarantees, with the three facts that bound the difference), §8 (a gating falsifier with a number), R2/R4 (both conservatisms quantified).
- **Radical-clean where nothing is consumed:** if M3 shows `GC.GetTotalAllocatedBytes` resolves on both TFMs, the `#if`, the fallback, the weaker guarantee and OQ-2 are all **deleted**, not kept "for safety".

**Document discipline**

- Cites #114 and #1136 as load-bearing (header). Operator framing quoted verbatim (§0).
- Scope **and** non-scope explicit (§2), including the four adjacent findings this design deliberately does not touch and the `InternalsVisibleTo` debt it declines to extend.
- Residual precise and not over-claimed: §5.2 states exactly what the bound is about (retained bytes) and exactly what it is not (the sizer's approximation under aliasing); §5.5 states the TFM difference rather than averaging it away.
- **Nothing superseded.** This extends §8 of `execution-guards-depth-memory.md` and resolves OQ-5 / residual 4 of `reflected-method-guard.md`; both stay live and both get a pointer edit in the same PR (unit 4).

---

## 15. Open Questions

| # | Question | Recommendation | Gating? |
|---|---|---|---|
| **OQ-1** | §5.3 leaves `nextMeasureAt = ticks + max(256, units)` **unchanged**, on the argument that the growth trigger makes it provably safety-neutral while it remains load-bearing for cost. #7877 named that line as the defect. Does the operator accept leaving it, or is changing it wanted regardless? | **Leave it.** §4.1's arithmetic shows changing it fixes nothing (the measured payload never reaches the first appointment), and deleting the `units` term costs a full walk every 256 checkpoints for a large live set, for zero safety gain. Flagged because it is the one place this design declines the literal ask. | No — but it is the decision most likely to be questioned. |
| **OQ-2** | The full structural guarantee is available on **net8.0 only**; netstandard2.0 gets a weaker, suppressible heuristic (§5.5). Three options: **(i)** ship both with the difference documented; **(ii)** leave netstandard2.0 on today's cadence and document the guarantee as net8.0-only; **(iii)** bump the TFM, dropping .NET Framework consumers. | **(i).** It is strictly better than today on netstandard2.0, costs one `#if`, and does not make a packaging decision on the operator's behalf. **(iii) is not mine to take** — it breaks consumers. | **Yes if the operator wants (iii)** — that changes the csproj and belongs to them. |
| **OQ-3** | §10.2's regression test drives a **host-registered** primitive, because §10.1 shows no built-in uncharged super-linear primitive survives #7872. Is the operator content that the pin lives on the host surface, given that is where the class permanently lives (#7872 §12 residuals 10–12)? | **Yes** — and it is the stronger pin: it tests the mechanism against an *unrecognised* member, which is the property the design claims. A built-in-only pin would silently become an allow-list test. | No — but it is the answer to "which primitive drives it", and worth an explicit nod. |
| **OQ-4** | `[assembly: InternalsVisibleTo("Scripting.Tests")]` (`AssemblyInfo.cs:6`) contradicts the 2026-08-08 ruling. This design declines to extend it (§10.4) and declines to remove it (out of scope). | **File it as its own task.** Removing it means rewriting every existing test that reaches an internal budget — a large, unrelated diff that has no business in a cadence PR (one feature per PR). | No — needs the orchestrator to file it. |

---

## 16. Audit

| Item | ✓ | Location |
|---|---|---|
| Pre-Design Checklist (#1136 §5) answered in order as doc sections | ✓ | §14 |
| **The un-outrunnable argument is structural, not empirical, and stated as such** | ✓ | §5.2 — four claims, the first being "retention implies allocation", which is a CLR property and not an inventory of this engine's primitives |
| The three named candidate directions each addressed; two refuted structurally | ✓ | §4.1 (`2^k`), §4.2, §4.3 (cost cliff) — plus §1's 80-checkpoints-vs-256 arithmetic against the actual measured payload |
| Cost answered with a **named measurement** John must take | ✓ | §8 M1, calibrated against the `DeadlineGuard` ~22 ns precedent, same 10M-checkpoint / 5-trial shape |
| **A stated result that falsifies the design** | ✓ | §8 M1 — `(c − b) > ~22 ns/checkpoint` bounces to the architect; reduced sampling is explicitly named as the wrong repair and why |
| Unconfigured hosts pay nothing — confirmed or deliberately changed | ✓ | §7 — confirmed, three-row table, existing `maxBytes.HasValue` gate reused; the byte-configured host's bill stated honestly |
| **Interaction with M4 stated, not implicit** | ✓ | §9.1 — six-row comparison; neither subsumes the other; charge-once untouched |
| **Interaction with #7872's charge sites stated, not implicit** | ✓ | §9.2 — different mechanism, different files, no merge-order dependency, complementary coverage on the host surface |
| Regression test specified, including **which primitive drives it and why it is not already charged** | ✓ | §10.2 — a host member growing a passed-in `List<object>`; four independent reasons it is uncharged now and after #7872 |
| Test isolation proven — nothing else in the guard can be what fires | ✓ | §10.3 arithmetic table + verify-by-revert |
| Test rulings honoured (no shared fixture state, one-sentence `[Description]`, no new `InternalsVisibleTo` dependency, no `<remarks>`, no `//`) | ✓ | §10.3, §10.4, §13 units 1 and 3 |
| Every principle-overriding decision states its math — or the conflict was surfaced | ✓ | §4.1, §5.2, §5.4, §10.3, §14; the two decisions that decline the literal ask are surfaced as OQ-1 and OQ-3 rather than rationalised |
| Scope and non-scope named | ✓ | §2 |
| Readable as John, no build-blocking gaps | ✓ | §13 — ordered units, exact file, exact members, the non-obvious back-off line called out (§6 item 6), explicit do-nots |
| Alternatives rejected on record | ✓ | §11 — nine, each with the reason it fails |
| Filed to working tree **and** DiVoid | ✓ | `docs/architecture/variable-budget-cadence.md` + the DiVoid node this document is filed as |
