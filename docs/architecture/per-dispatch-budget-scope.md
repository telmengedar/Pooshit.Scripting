# Architectural Document: Per-Dispatch Budget Scope for Host-Dispatched Cached Lambdas

> **Repo path:** `docs/architecture/per-dispatch-budget-scope.md` — this file is the canonical copy; the DiVoid `documentation` node mirrors it.
> **Source task:** DiVoid #7880 (read its `CORRECTION 2026-08-08` section, not only the original ask — the original mechanism is disproved).
> **Also closes:** #7885 (a cached lambda has no timeout at all) and #7886 (`InvokeFrom` redirects only `DepthBudget`).
> **Diagnosis:** **#7888** — *"Diagnosis — pooscript limiter counters: the escape is a cached `LambdaMethod`, not parse-time capture"*, verdict `valid_bug`, run against `master @ 7a329f9`. Carries the per-counter table, the verbatim experiment record (E1–E16) and the ruled-out surfaces. Experiment ids are cited inline below rather than re-derived. *(Earlier drafts of the brief cited #7883 for this; that id holds an unrelated portal eval. #7888 is the diagnosis.)*
> **Evidence:** #7882 (Uberkarl-side empirical proof). **Driver:** #7879 (Uberkarl behaviour quarantine).
> **Prior decisions this design must not silently override:** #7736 §7.7 (`Invoke` keeps the captured depth budget) and #7782 (the public-contract decision that produced it). **Neither is amended.** §6.3 below re-reads §7.7 and applies its reasoning to the new entry point rather than overturning it.
> **Load-bearing contracts:** Design Contracts #1136 (§1 KISS/DRY/YAGNI; §5 Pre-Design Checklist walked as §15 of this document), Code Contracts #114 §0, DRY threshold #1267, anti-seed-complexity #1184, principles-trump-design #1333, real-names discipline #6836.
> **Predecessor design this extends (not supersedes):** `docs/architecture/execution-guards-depth-memory.md` (#7736). That document stays live and current; this one adds a fourth execution boundary to the model it established.

---

## 1. The user's words — and which half is already true

> "usage of limits accumulated over multiple runs, it should be per run and also per execution context (if not already happening)."
>
> — Toni, 2026-08-08

> "Limiters should reset on (each execution) start; anything else makes no sense. And they must be stored in the run context, not captured/shared at parse time — otherwise parallel/concurrent script execution shares mutable budget state and corrupts."
>
> — Toni, DiVoid #7880

**The parenthetical hedge — "(if not already happening)" — is load-bearing, and the answer is: it is already happening, for the documented entry points.** This design does not deliver it. It delivers the one place where it is not happening.

| Clause of the directive | Status **before** this design | What this design changes |
|---|---|---|
| "stored in the run context, not captured/shared at parse time" | **Already true.** `ScriptParser.Limits` is an immutable `ScriptLimits` config object (`ScriptLimits.cs` — every knob is `init`-only, `None`/`Default` are shared instances). No live counter exists at parse time. Measured (#7888 E11): *"300 alternating `Execute()` pairs OK → a shared `ScriptLimits` instance does NOT share live counters"*. | Nothing. The premise in #7880's original text is disproved. |
| "reset on (each execution) start" | **Already true for `Execute` / `ExecuteAsync`.** Every live counter is allocated fresh at the single site `GuardedExecution.Prepare` (`GuardedExecution.cs:50-54`), reached from `Script.cs:90` and `Script.cs:117`. Measured (#7888): 500 sequential `Execute` at `MaxSteps=50` all pass; 20 000 fresh executes all pass. | Makes a **host-dispatched cached lambda an execution start** — the one dispatch the engine never recognised as a run. |
| "per execution context … parallel/concurrent script execution shares mutable budget state and corrupts" | **Already true at the documented entry points.** Measured (#7888): 8 threads × 200 `Execute` of one parsed `IScript`, `MaxSteps` sized so one run barely fits → *"0/8 threads failed"*; 16 threads × 100 executes of a depth-3 recursion at `MaxDepth=6` → *"0/16 threads failed"*. `StepBudget.Consume` uses `Interlocked`, so this was never torn state. | Extends the same property to cached-lambda **dispatch**, where it is genuinely broken today (E15: 7/8 threads failed) — as a **consequence** of the fix, not as separate machinery. |

**So the design question is not "move the counters into the run context" — they are there.** It is:

> **What counts as a "run" when host C# dispatches a cached lambda with no execution in progress?**

---

## 2. Problem Statement

A `LambdaMethod` **extends a run's lifetime past the `Execute` that created it.**

```
LambdaToken.Execute:53      ScriptContext lambdacontext = new(context);   // copy ctor
ScriptContext.cs:15-21      copy ctor copies StepBudget / DepthBudget / VariableBudget BY REFERENCE
LambdaMethod.cs:12          the LambdaMethod holds that ScriptContext forever
LambdaMethod.cs:44,99       Invoke() charges context.Guard() and rebuilds from `context`
```

A host that parses once and invokes many times (the supported, intended `CompileOnce` contract) therefore charges **one** `StepBudget` — the one allocated by the single `GuardedExecution.Prepare` that produced the lambda — for the lambda's **entire lifetime**. `StepBudget.consumed` is monotonic with no reset path, so:

- Measured (#7882): `MaxSteps=100` → throws on invoke **#8**; `MaxSteps=1000` → throws on invoke **#77**. ~13 steps per invoke either way. The trip point scales with the running total, not with any single dispatch's cost.
- Reproduced independently against the **Release build** (#7888 E1, verbatim): *"threw after 199 successful invocations: `ScriptStepLimitExceededException`: Script exceeded the configured step limit of 1000"*.
- Once tripped, **every** subsequent invoke throws forever. Permanent quarantine, not a hiccup.
- Raising the limit only delays it. **Any long-lived parse-once/invoke-many script is doomed** under a lifetime-cumulative budget.
- **Concurrent dispatch of one cached lambda shares all of it** — #7888 E15: 8 threads × 5 000 invokes at `MaxSteps=100000` → *"7/8 threads failed; total successful invokes = 33330"*. A symptom of the same escape, not a separate mechanism (`Interlocked` was never the issue — attribution was).

Two independent defects share the same escape mechanism and point in the **opposite** direction:

- **#7885 — no timeout at all.** `GuardedExecution.Dispose` (`:81`) disposes the `CancelAfter`-armed CTS **un-fired** when `Execute` returns. The cached lambda still holds the derived `CancellationToken` of that dead source, so it can never become cancelled. Measured (#7888 E4a/E4b/E16): the same `Timeout=300 ms` fired at **323 ms** on a direct `Execute`, while an infinite loop dispatched through a cached lambda was **still running after 3 000 ms**, and three consecutive ~900 ms invokes all completed. This is **under**-enforcement — a hostile or merely wedged handler is unbounded — and no fix to the step counter addresses it.
- **#7886 — the documented remedy does not remedy.** `LambdaMethod.InvokeFrom` reaches `InvokeCore` (`:99`), which builds `new ScriptContext(context, depthBudget)` — and that substituting ctor (`ScriptContext.cs:29-35`) substitutes **only `depthBudget`**. Steps, variables, limits and cancellation still resolve from the **defining** context. Measured (#7888 E12): definer `MaxSteps=100` / invoker `1e6` → *"threw `ScriptStepLimitExceededException` … step limit of 100"*. **A host following `docs/pooscript-language-reference.md` §12 to the letter still gets the Uberkarl symptom.**

**Success criteria.**

1. Parse once, cache a handler lambda, dispatch it 10 000× from host C# under a finite `MaxSteps` → no throw.
2. The same, under a finite `Timeout` with a body that exceeds it → `ScriptTimeoutException` on **every** dispatch, including the first.
3. A host extension that follows §12 (`InvokeFrom`) gets the **invoking** run's budgets in full, not just its depth.
4. **A script cannot obtain a fresh budget by bouncing through a host extension.** No mechanism in this design is reachable by script choice.
5. **The #7736 §7.7 counter-example still aborts with a catchable `ScriptDepthLimitExceededException`, never a `StackOverflowException`.**
6. The memory guard is not weakened: a value retained in a lambda's **closure** is still charged.

---

## 3. Scope & Non-Scope

### In scope

- A named public entry point on `LambdaMethod` for **host dispatch with no execution in progress**, which opens a budget scope in its own right.
- Which of the four counters reset at that boundary and which do not — **four counters, four different answers** (§6).
- The **discriminator** between host dispatch and in-script transitive invocation (§7), and the sandbox consequence of getting it wrong.
- **#7885** — the cached-lambda timeout hole. It is closed *by construction* by reusing `GuardedExecution`; it is not separable from the step fix (§13).
- **#7886** — `InvokeFrom` generalised from depth-only to the whole governing budget set (§9).
- **Explicit decision** on `DepthBudget.breached`, the sticky latch with no reset path (`DepthBudget.cs:49-52`) — §6.5.
- **Explicit decision** on the caller's `CancellationToken` sticking to a cached lambda after its `Execute` returned — §6.6.
- The documentation debt both defects create on landing (§12).
- The regression tests the fix must land with (§11).

### Explicitly out of scope (non-goals)

- **Any behaviour change to `LambdaMethod.Invoke(params object[])`.** #7736 §7.7 / #7782 decided this and the decision stands. Making `Invoke` behave contextually is rejected in §7 with its own reasoning; the characterisation test that pins it (`Depth_HostExtensionInvokeArgsSpuriouslyBreaches`) is **not** touched.
- **An ambient "execution in progress" tracker** (`[ThreadStatic]` or `AsyncLocal`). Considered and rejected in §7 as the discriminator, and rejected again in §16 R1 as a defensive backstop.
- **Making `Invoke` time-bounded.** `Invoke` runs under the defining run's token by definition; there is no deadline to arm for it. Hosts that want a bounded dispatch use the new entry point.
- **Public surface for the budget types.** `StepBudget` / `DepthBudget` / `VariableBudget` stay `internal`. Nothing in this design requires a host to name them (#7882 §5 identified their inaccessibility as the reason the fix must land engine-side — this design accepts that and keeps them internal).
- **New `ScriptLimits` knobs.** No configurability is added (#1136 §3 — no named operator, no environment difference).
- **Resolving #7736 OQ-9's residual.** A host driving one cached lambda on *M* of its own threads still shares one `DepthBudget` and still spuriously breaches at *M > MaxDepth*. That is the direct, unavoidable consequence of the depth carve-out (§6.3). **OQ-9 is answered affirmatively for the *step/timeout* half — host-thread dispatch does need public surface — and remains open for the depth half**, with the same mitigation as before: size `MaxDepth` above your own concurrency.
- **#7877** (`VariableBudget.Measure` cadence grows with the amount charged) — a pre-existing, separately filed defect, untouched here.
- **The surfaces #7888 ruled out by experiment, not reasoning** — a shared `ScriptLimits` instance across scripts (E11), `VariableBudget` accumulation (E7b/E14), and the `[ThreadStatic]` parse-depth counter from #7869 (E8a/E8b unwind correctly across pooled `ParseAsync` threads; E8c: 16 threads, 0 failures). **No defect in any of them; none is touched.** Listed explicitly so a later reader does not re-open them.
- **Uberkarl-side work.** The one-line call-site change (`Invoke` → the new entry point) plus a package bump is Uberkarl's; it is recorded in §17 OQ-3 because #7880 anticipated "just bumps the package".

---

## 4. Assumptions & Constraints — verified against source

| # | Statement | Confidence | Source |
|---|---|---|---|
| A1 | `ScriptLimits` is immutable config; nothing mutable is captured at parse time. | Verified | `ScriptLimits.cs` — every knob `init`-only; `None`/`Default` shared instances |
| A2 | `GuardedExecution.Prepare` is the **single** site that allocates live counters and arms the deadline. | Verified | `GuardedExecution.cs:50-64`; only callers are `Script.cs:90`, `Script.cs:117` (+2 test sites) |
| A3 | `Prepare` **already** supports inheriting a `DepthBudget` instead of allocating one — added for the import boundary. | Verified | `GuardedExecution.cs:51` (`inheritedDepthBudget ?? …`) |
| A4 | **The import boundary is already exactly this design's shape.** `ExternalScriptMethod.Invoke` enters/exits the *caller's* `DepthBudget`, then calls `Script.Execute(variables, callerToken, depthBudget)` — a full fresh `GuardedExecution` with a fresh `StepBudget`, a freshly armed deadline, and an **inherited** depth budget. | Verified | `ExternalScriptMethod.cs:29-42`; `Script.cs:89-97` |
| A5 | `ScriptContext`'s copy ctor and the depth-substituting ctor both copy `StepBudget`/`VariableBudget` **by reference**; the substituting ctor's only caller is `LambdaMethod.InvokeCore:99`. | Verified | `ScriptContext.cs:15-35`; grep of `new ScriptContext(` across `Pooshit.Scripts/` |
| A6 | `VariableBudget` measures an **instantaneous footprint** by walking the scope chain **up to a captured `root`**, not a cumulative spend. Its mutable state (`producedSinceLastPass`, `ticks`, `nextMeasureAt`) is a sampling cadence that self-heals at every `Measure`. | Verified | `VariableBudget.cs:85-114` — `Measure` walks `scope → parent → … → root` and zeroes `producedSinceLastPass` |
| A7 | `StepBudget.Consume` and `DepthBudget.Enter/Exit/CheckBreached` are already `Interlocked`/`Volatile`. Concurrency was never torn state — it is a budget **attribution** defect. | Verified | `StepBudget.cs:32`, `DepthBudget.cs:34,43,50` |
| A8 | `MethodOperations.CreateParameters` injects the invoking `ScriptContext` into any extension-method parameter of that type, consuming a target slot but **no script argument**. There is no host-extension case in which the invoking context is unavailable. | Verified (#7736 §7.7, QA-verified against the pre-existing `.where(`/`.indexof(` tests) | `MethodOperations.cs`; `EnumerableExtensions.cs:34,211,260` |
| A9 | `ScriptLimits.Default` (the 1.0 secure-by-default instance) leaves `MaxSteps` and `Timeout` **null**. On the default path this design allocates **no** `StepBudget` and **no** `CancellationTokenSource` per dispatch. | Verified | `ScriptLimits.cs` — `Default` sets only `MaxDepth`, `MaxParseDepth`, `MaxVariableBytes`, `RegexTimeout`; `GuardedExecution.cs:50,56-59` short-circuit on null |
| A10 | A `CancellationToken` derived from a **disposed, un-fired** `CancellationTokenSource` is what a cached lambda holds today (#7885). Re-linking to it is at best semantically dead and at worst an `ObjectDisposedException` hazard. | Asserted from `GuardedExecution.cs:61-64,81`; the ODE half is **flagged for verification** (§16 R5) | — |
| A11 | 1.0 shipped today (`a0f68db`, PR #16). Pre-1.0 latitude for additive/deprecating public changes (#7712 A3) may have closed. | Assumed — **operator question OQ-1** | git log; #7712 A3 |

---

## 5. Architectural Overview

The engine has **three** execution boundaries today. This design adds a **fourth**, built from the same parts as the third.

```
                       fresh    fresh     depth      variable    caller
  BOUNDARY             steps?   deadline?  budget?    budget?     token
  ─────────────────────────────────────────────────────────────────────────
1 Script.Execute        YES      YES       fresh      fresh       host's
  Script.ExecuteAsync                      (top level)
        └─ GuardedExecution.Prepare(vars, tp, ct, limits)

2 task.run body         no       no        FRESH      shared      shared
        └─ LambdaMethod.InvokeOnNewStack  — a genuinely new PHYSICAL stack

3 import(...)           YES      YES       INHERITED  fresh       carried
        └─ ExternalScriptMethod.Invoke → Script.Execute(v, ct, inheritedDepth)
           └─ GuardedExecution.Prepare(…, inheritedDepthBudget: caller's)

4 host dispatch  ◄── NEW        YES      YES       INHERITED  INHERITED   host-supplied
        └─ LambdaMethod.InvokeAsExecution([ct,] args)
           └─ GuardedExecution.Prepare(…, inheritedDepthBudget:    captured
                                          inheritedVariableBudget: captured)
```

**Boundary 4 is boundary 3 with one difference**, and the difference is principled: an imported script has its **own** variable scope, so it gets a fresh `VariableBudget` rooted at its own arguments; a lambda runs inside a **closure over the defining run's scope**, so it must keep the defining run's `VariableBudget` and its `root`, or the closure stops being charged (§6.4).

Nothing else in the engine changes shape. `GuardedExecution` remains the only thing that allocates counters and arms deadlines (A2) — it gains one optional parameter, mirroring the one it already has.

**Data flow of one host dispatch:**

```
host C#:  cachedLambda.InvokeAsExecution(shutdownToken, delta)
   │
   ├─ 1. capturedContext.DepthBudget?.ResetBreach()        ← new run ⇒ clear the sticky latch (§6.5)
   │
   ├─ 2. GuardedExecution.Prepare(
   │        variables:               capturedContext.Arguments   (scope anchor only)
   │        typeprovider:            capturedContext.TypeProvider
   │        callertoken:             shutdownToken               ← host's, NOT the captured one (§6.6)
   │        limits:                  capturedContext.Limits
   │        inheritedDepthBudget:    capturedContext.DepthBudget ← CARVE-OUT (§6.3)
   │        inheritedVariableBudget: capturedContext.VariableBudget)
   │     ⇒ fresh StepBudget · freshly armed linked CTS · same depth · same variable budget
   │
   ├─ 3. execution.Context.Guard()                          ← one step, on the FRESH budget
   │
   ├─ 4. InvokeCore(governing: execution.Context,
   │                depthBudget: execution.Context.DepthBudget,
   │                arguments:   args)
   │        └─ depth.Enter() / lambda ctx = scope(captured) + budgets(governing) / depth.Exit()
   │
   ├─ 5. catch OperationCanceledException ⇒ execution.Convert(e)
   │        ⇒ ScriptTimeoutException when the deadline fired on its own,
   │          the original OCE when the host's token was cancelled
   │
   └─ 6. execution.Dispose()                                ← tears down the CTS for THIS dispatch only
```

Step 6 is the whole of the #7885 fix: the deadline is armed and disposed **inside** the dispatch, so it is alive exactly while the dispatch runs.

---

## 6. The four counters — four different answers

The single most important property of this design is that **the four counters do not get the same treatment.** A fix that says "fresh budgets per invoke" without carving depth out silently converts a catchable abort into a process kill.

| Counter | Bounds | Answer at boundary 4 | Failure mode if wrong |
|---|---|---|---|
| `StepBudget` | per-dispatch **work** | **Fresh instance** | over-enforcement → permanent quarantine (#7880, the bug) |
| `Timeout` (linked CTS) | per-dispatch **wall clock** | **Freshly armed, disposed after** | under-enforcement → unbounded handler (#7885) |
| `DepthBudget` | **physical call stack** | **Inherited by reference**, latch cleared | under-enforcement → **uncatchable `StackOverflowException`** |
| `VariableBudget` | instantaneous **footprint** | **Inherited by reference**, untouched | reallocating with the wrong `root` → closure stops being charged (memory-guard regression) |

### 6.1 `StepBudget` — a fresh instance, not a reset

`MaxSteps` bounds *script-driven looping within one execution* (`ScriptLimits.cs` XML; §12 of the language reference). A host dispatch is one execution. The counter must start at zero.

**Fresh allocation, not `StepBudget.Reset()`.** #7882's fix sketch suggested "`Invoke` internally re-arms/resets its own captured `StepBudget`". Rejected, for two reasons:

1. **DRY.** `GuardedExecution.Prepare` already allocates a `StepBudget` from `limits.MaxSteps` and is the only site that does (A2). Reusing it adds nothing; adding a `Reset()` adds a second, parallel way to start a budget.
2. **The user's own "per execution context" clause.** A reset mutates a **shared** object. Two host threads dispatching the same cached lambda concurrently would reset each other's counter mid-flight — precisely the "parallel/concurrent script execution shares mutable budget state and corrupts" failure the directive names. A fresh instance per dispatch is by construction per-execution-context. `Interlocked` makes the counter safe; it does not make a *shared* counter *correct*.

Cost: one small object per dispatch, and **only when `MaxSteps` is configured** — `ScriptLimits.Default` leaves it null (A9), so the default path allocates nothing.

### 6.2 `Timeout` — freshly armed and disposed inside the dispatch (#7885)

`Timeout` is not a counter; it is a deadline realised as a linked, `CancelAfter`-armed CTS (`GuardedExecution.cs:61-64`). The defect is a **lifetime** defect: `Dispose` (`:81`) tears the source down un-fired when `Execute` returns, so the token the cached lambda holds can never fire. Its deadline is not un-reset — it is *destroyed*.

Reusing `GuardedExecution` for boundary 4 fixes this with no additional mechanism: `Prepare` arms a fresh deadline linked to the host-supplied token, and the `using` in `InvokeAsExecution` disposes it when the dispatch returns. `execution.Convert(e)` (`:74-78`) already distinguishes "the deadline fired on its own" (→ `ScriptTimeoutException`) from "the host's token was cancelled" (→ rethrow the original) — no new exception plumbing.

**This is why #7885 is not separable from #7880** (§13). You cannot open a fresh step scope through `GuardedExecution` without also arming and disposing the deadline; building one without the other means building the scope twice.

Cost: one CTS + one timer registration per dispatch, **only when `Timeout` is configured** (`GuardedExecution.cs:56-59` returns early otherwise). Default path: zero.

### 6.3 `DepthBudget` — the carve-out. Inherited, never fresh.

**`DepthBudget` must NOT be freshly allocated per dispatch.** #7736 §7.7 argued and rejected exactly that (as "option (d)"), and this design **re-reads that decision and applies it unchanged**. It is not amended.

The counter-example lives in this repo. `ExecutionGuardTests.InvokeSplitExtensions` is a test-local host extension whose body invokes a callback. Route recursion through it at every level:

```
$fac = $n=>{ if($n>0) { return($fac.invokecallback($n-1)) } return(0) }
```

Every hop resolves from the **definition-site** context — the same one at every level. A fresh depth budget per call therefore reads **depth 1 forever while the physical stack grows without bound.** Three properties make that unacceptable rather than merely looser (§7.7, quoted in substance):

1. **There is no backstop.** The `RuntimeHelpers.EnsureSufficientExecutionStack` probe was built, measured and dropped — recursion through reflection went from completing normally to hard process death with **no** window in which the probe fired. `MaxDepth` is not a ceiling with a margin behind it; it *is* the only mechanism watching.
2. **The unguarded shape would be the most expensive one.** §11.1 measured a reflected host-callback chain aborting safely only to depth **16**, against **21** for direct `$f.invoke`. A fresh-budget option removes the guard from precisely the dispatch shape that consumes the most stack per level.
3. **The two errors are not comparable in kind.** A false positive is a catchable `ScriptDepthLimitExceededException` carrying its `Limit`. A false negative is process death with no diagnostic and no recovery. **Over-counting fails safe.**

**Does the "no run in progress" premise not make the stack genuinely fresh?** If the premise held, a fresh budget would be correct by the same §7.6 reasoning that justifies `InvokeOnNewStack` (`task.run` bodies do get a fresh `DepthBudget`, because a `Task.Run` genuinely starts a new physical stack). **But the engine cannot verify the premise.** `InvokeAsExecution` is a public method; a host extension reached from a running script can call it, and then the stack is not fresh. `InvokeOnNewStack` is `internal` and reachable only through `TaskHost.Run`, so its premise is structurally guaranteed. The new entry point's premise is a *documented host obligation*, and a design must not stake process survival on a host obligation.

**Therefore: `InvokeAsExecution` passes the captured `DepthBudget` to `Prepare` as `inheritedDepthBudget`.** This is the same treatment the import boundary already gives it (A4) and for the same reason recorded there: *"the depth budget deliberately does [cross], since it bounds a shared physical resource rather than per-script work"* (`ExternalScriptMethod.cs:24-26`).

**What breaks without the carve-out:** T5 and T6 (§11) — a script that recurses through a host extension at every level would read depth 1 forever and kill the host process with an uncatchable `StackOverflowException` instead of aborting with `ScriptDepthLimitExceededException`. That is the single worst outcome available in this design space, and it is worse than the bug being fixed.

**Cost, stated honestly:** *M* concurrent host-thread dispatches of one cached lambda share one depth counter and spuriously breach at *M > MaxDepth*. This is #7736 OQ-9's residual, unchanged. Mitigation is unchanged: size `MaxDepth` above your own dispatch concurrency. It is not fixed here because fixing it requires exactly the fresh budget that (1)–(3) forbid.

### 6.4 `VariableBudget` — inherited by reference, untouched

**This is the one counter whose answer differs from the "steps / timeout / variables reset, depth does not" framing — and the diagnosis's own measurements are what decide it.** #7888's per-counter table records `VariableBudget` → *"no reset — **benign**"*, *"level-based, not cumulative"*; its trap-1 sentence nonetheless lumps variables in with the resettable counters. **The table wins** (OQ-6). Resetting would be a regression. The evidence:

1. **`VariableBudget` is not a spend counter.** `Measure` (`VariableBudget.cs:85-114`) walks the live scope chain and compares the *current* footprint against the ceiling. It is instantaneous, not cumulative. Its mutable state — `producedSinceLastPass`, `ticks`, `nextMeasureAt` — is a **sampling cadence**, and every `Measure` zeroes `producedSinceLastPass`. There is no accumulation and no quarantine path. Measured (#7888 E7b/E14): **20 000 invokes clean**. #7880's correction records the same: *"Ruled out by experiment: … `VariableBudget` accumulation (level-based, not cumulative)."*
2. **Reallocating requires re-deriving `root`, and getting `root` wrong weakens the guard.** `Measure` walks `scope → parent → … → root` and **stops at `root`** (`:91-93`), where `root` is the host-supplied variable provider from the original `Execute`. A lambda's context chain runs *through the closure* up to that root. A fresh `VariableBudget` rooted anywhere else — e.g. at the lambda's captured `Arguments` — would stop the walk **below the closure**, so a value stashed in a closure variable would stop being charged after the first dispatch. That is a memory-guard hole in a secure-by-default 1.0, introduced to fix a defect that does not exist.
3. **It survives the delete check** (#1136 §4). Delete "reallocate the variable budget" from this design and nothing observable breaks.

**Decision: `InvokeAsExecution` passes the captured `VariableBudget` to `Prepare` as `inheritedVariableBudget`.** The footprint bound remains anchored at the original host root and keeps charging the closure.

**Residual, recorded not hidden:** `producedSinceLastPass` carries across dispatches, so a `ChargePreAllocation` (`:59-71`) can false-positive on residue from a previous dispatch. It is bounded below `maxBytes` (a `Measure` is forced the moment the accumulation reaches the ceiling, `:50-51`) and self-heals at the next `Measure`. This is the **same** behaviour that already exists within one long-running execution — not something this design introduces. The adjacent cadence defect is #7877, separately filed and out of scope.

**Test T10 pins this decision** — it is what fails if someone later "helpfully" reallocates the variable budget.

### 6.5 Explicit decision — the `DepthBudget.breached` sticky latch

`DepthBudget.breached` (`DepthBudget.cs:35,49-52`) is a one-way latch with **no reset path**. `CheckBreached` re-raises the original breach at every subsequent `Guard()`.

**Within one execution it is load-bearing.** It defeats a script that wraps recursion in `try`/`catch`, swallows the `ScriptDepthLimitExceededException`, and continues. `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` pins that behaviour deliberately.

**Across a cached lambda's lifetime it is a second permanent-quarantine mechanism** — the same shape as the step bug, with the same "no recovery path" property. A handler that breaches depth once (bad input, one unusually deep data structure) is dead forever, even though every subsequent dispatch is a new run under a fresh step budget and a fresh deadline.

**Decision: clear the latch at the start of each `InvokeAsExecution`, and only there. `depth` itself is untouched.**

- The latch's semantic is *"a breach happened **in this run** and the script must not continue past it"*. A new dispatch is, by this design's central premise, a new run. Clearing at a run boundary is exactly consistent with the directive's "limiters reset on each execution start".
- **The stack-safety mechanism is unaffected.** `Enter()` still throws at `depth > Limit` (`:34-37`). The latch is the anti-swallow *policy* re-raise, not the physical bound. Clearing it cannot cause a `StackOverflowException`.
- **It must not live inside `Prepare`.** The import boundary also passes `inheritedDepthBudget` (A4), and an import is the *same* run — clearing there would let an imported script swallow the caller's depth abort. The reset belongs at the `InvokeAsExecution` call site only. This is a concrete implementation constraint, not a stylistic note.
- Mechanism: one new **internal** method `DepthBudget.ResetBreach()` — a single `Interlocked.Exchange(ref breached, 0)`. No public surface.

**Concurrency note (accepted, recorded):** if thread A breaches while thread B starts a new dispatch, B's clear can erase A's latch before a concurrently-blocked holder observes it, losing a policy abort. The physical bound (`Enter`) is unaffected, so the worst case is a lost abort, not a process kill. A conditional clear (only when `depth == 0`) was considered and **rejected**: it would make a cached lambda's recoverability nondeterministic under concurrency, which is worse than a rare lost policy abort.

### 6.6 Explicit decision — the caller's `CancellationToken`

**The captured token does not carry to a detached dispatch. The host supplies one.**

The captured `ScriptContext.CancellationToken` is one of two very different things and the engine cannot tell them apart:

- When `Timeout` **was** configured, it is the engine's own derived token from a CTS that `Dispose` already tore down (`GuardedExecution.cs:64,81`). It is a dead deadline token, not the host's token at all. Re-linking to it is semantically wrong and is an `ObjectDisposedException` hazard (A10, §16 R5).
- When `Timeout` was **not** configured, it is the host's own token (`GuardedExecution.cs:57-58`). That may be a process-lifetime shutdown token — in which case keeping it is *right*, it kills handlers on shutdown — or a request/load-scoped CTS the host cancelled and disposed after `Execute` returned — in which case keeping it **silently poisons every subsequent dispatch forever**.

Guessing in the poison direction produces a permanent quarantine: precisely the bug class this design exists to close. Guessing in the other direction leaves the host with a dispatch that is still bounded (`Timeout` now works, §6.2) and with an explicit way to say what it means.

**Decision: two overloads.**

- `InvokeAsExecution(CancellationToken cancellationToken, params object[] arguments)` — the real method; `cancellationToken` becomes the `callertoken` handed to `Prepare`.
- `InvokeAsExecution(params object[] arguments)` — one-line convenience delegating with `CancellationToken.None`.

A host that wants shutdown cancellation passes its shutdown token. A host that wants a wall-clock bound configures `Timeout`. A host that does neither gets the same contract as the parameterless `IScript.Execute()` — unbounded, its own choice. This is stated in the host contract (§12).

---

## 7. The discriminator — Trap 2, and the sandbox consequence of getting it wrong

`Invoke` is reached from two genuinely different places:

1. **Host C# with no run in progress** → must open a budget scope. (Uberkarl's per-frame `onUpdate`.)
2. **Transitively from inside a running script**, via a host extension that did not declare a `ScriptContext` parameter → must **not** open one.

**The consequence of conflating them.** If invocation-with-a-fresh-budget is reachable by script choice, a script writes:

```
while (true) { $handler.bounce() }        # bounce() is a host extension calling Invoke(args)
```

Each iteration costs the outer script **one** step and grants the inner dispatch a **full fresh `MaxSteps`** — and, if the deadline is re-armed too, a full fresh `Timeout`. Total reachable work is `MaxSteps²` (and `MaxSteps × Timeout` of wall clock). At `MaxSteps = 1e6` that is `1e12` steps: a DoS-shaped sandbox hole, dressed as a bug fix. **This is what test T4 exists to catch.**

### The options

| # | Shape | Verdict |
|---|---|---|
| **A** | **Ambient marker.** `[ThreadStatic]` (or `AsyncLocal`) "execution in progress" flag set by `GuardedExecution`; `Invoke` opens a scope only when unset. | **Rejected.** (i) A public method's semantics would depend on invisible ambient state — the opposite of #6836's "expose the real structures under their real names". (ii) It changes `Invoke`'s behaviour **silently** for every existing host, which re-opens the #7736 §7.7 / #7782 decision and would require amending the `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` characterisation test. (iii) The flag must be threaded correctly across `ExecuteAsync`'s `Task.Run` hop and every `task.run` body hop — new failure surface whose failure mode is *silently wrong budget attribution*, the exact defect class being fixed. (iv) It buys nothing the named entry point does not, at the cost of a mechanism that is hard to reason about and harder to test. |
| **B** | **Named public entry point.** `Invoke` unchanged; a new `InvokeAsExecution` always opens a scope. | **CHOSEN.** See below. |
| **C** | **`Invoke` always opens a scope.** | **Rejected** — the hole above, unconditionally. |
| **D** | **`Invoke` opens a scope; close the hole some other way.** | **Rejected** — there is no other way. The hole exists *because* the fresh budget is reachable by script choice; any closure of it is a discriminator, i.e. option A or B. |
| **E** | **Public `ScriptContext` ctor taking `ScriptLimits`, host builds a scope and calls `InvokeFrom`.** | **Rejected.** It puts the **depth carve-out in the host's hands** — a host-built context would carry a fresh `DepthBudget`, which is §6.3's forbidden shape, and the host has no access to the captured one (`DepthBudget` is `internal`). It also makes the host responsible for the CTS lifetime that #7885 proves is easy to get wrong. More public surface, worse guarantees. |

### Why B

1. **The hole cannot open by script choice.** The new-scope behaviour is reachable only by a method the *host* explicitly calls. A script cannot choose which method a host extension invokes. **T4 passes by construction**, not by a check that could be forgotten.
2. **`Invoke` stays the conservative default.** After this design, `Invoke` is the entry point that *never* grants a fresh budget — so a host extension author who reaches for the shortest name gets the **safe** behaviour. That is the right default for a security-relevant public API, and it means #7736 §7.7 / #7782 need no amendment: `Invoke(params object[])` is byte-for-byte unchanged in semantics, and `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` / `Depth_RecursionThroughInvokeArgsExtensionAtEveryLevelThrows` stay green **unmodified**.
3. **It answers #7736 OQ-9 in the affirmative.** OQ-9 asked whether host-thread dispatch needs public surface, and deliberately deferred on the grounds that no named consumer existed (#1136 §2). There is now a named consumer: Uberkarl (#7879), holding PR #33. The question is answered by a real requirement, not speculation — which is exactly the condition #1184 sets for building the thing.
4. **The engine keeps ownership of the whole scope lifecycle** — budget allocation, deadline arming, deadline disposal, depth carve-out, latch reset. The host writes one call.

### The residual risk B carries, stated plainly

A host extension reached from a running script *can* call `InvokeAsExecution` and thereby open the hole. **This is host misconfiguration of exactly the class §12 already names** — the same class as a host exposing a `Func<string,string>` wrapping `File.ReadAllText`. It is not closable by the engine without option A's ambient machinery (rejected in §16 R1 as a defensive backstop too). Mitigation:

- `InvokeAsExecution`'s XML remarks name the failure concretely: *"never call this from a host extension reached by a running script — it grants that dispatch a fresh step budget and a fresh deadline. Declare a `ScriptContext` parameter and call `InvokeFrom` instead."*
- §12 of the language reference names it in the same paragraph as the delegate caveat.
- **`InvokeFrom` is now fully correct (§9)**, so there is no longer any reason for a host extension author to reach for the wrong method. Before this design, `InvokeFrom` was silently partial for steps — which is *why* hosts reached elsewhere.

### Naming

**`InvokeAsExecution`** — it names the real mechanism in the engine's own vocabulary: this invocation *is* an execution in the `Script.Execute` sense, opening the same `GuardedExecution`. Alternatives considered: `InvokeDetached` (says what it is *not*), `InvokeGuarded` (misleading — `Invoke` is guarded too, by the defining run), `InvokeAsRun` ("run" is not a type name in this codebase). Low-stakes; recorded as OQ-2 since it is public surface at 1.0.

---

## 8. Components, contracts and the exact shape

No new components. Four existing types change; one internal method and one internal constructor are added; one internal constructor is **deleted**.

### 8.1 `LambdaMethod` (`Pooshit.Scripts/Providers/LambdaMethod.cs`)

**Owns:** binding a lambda's parameters and executing its body under a *governing* set of budgets.
**Does not own:** deciding what those budgets are for a host dispatch — that is `GuardedExecution`'s job, invoked from the new entry point.

`InvokeCore`'s parameter changes from a `DepthBudget` to **a governing `ScriptContext` plus an explicit `DepthBudget`**. The depth budget stays a separate parameter deliberately: it is the one thing *not* taken from the governing context, and making the carve-out structurally visible in the signature is a feature given §6.3.

| Entry point | Visibility | Governing context | Depth budget | Audience |
|---|---|---|---|---|
| `Invoke(params object[])` | public — **unchanged** | the **captured** context | the captured context's | host code or a test with genuinely no invoking context; the conservative choice; the transitive-from-script path |
| `InvokeFrom(ScriptContext, params object[])` | public — **generalised (§9)** | the **invoking** context | the invoking context's | a host extension declaring a `ScriptContext` parameter; engine dispatch of `$f.invoke(…)`; `EnumerableExtensions` |
| `InvokeAsExecution([CancellationToken,] params object[])` | public — **NEW** | a **freshly prepared** context | the **captured** context's (carve-out) | host C# / host thread, no run in progress |
| `InvokeOnNewStack(params object[])` | internal — **unchanged semantics** | the captured context | a **fresh** budget with the same `Limit` | `TaskHost.Run` only |

`InvokeAsExecution`'s obligations, in order:

1. Validate the argument count (the existing `CheckArguments`).
2. Clear the captured `DepthBudget`'s breach latch (§6.5) — **before** any `Guard()`, because `Guard()` calls `CheckBreached()`.
3. Prepare a `GuardedExecution` from the captured context's `Arguments`, `TypeProvider` and `Limits`, the host-supplied token, and **both** inherited budgets. Dispose it when the dispatch returns (`using`).
4. `Guard()` the prepared context (charges one step to the fresh budget; observes the fresh token).
5. `InvokeCore(governing: prepared context, depthBudget: prepared context's depth budget, arguments)`.
6. Convert an `OperationCanceledException` through `GuardedExecution.Convert` — `ScriptTimeoutException` when the deadline fired on its own, the original exception when the host's token was cancelled.

### 8.2 `GuardedExecution` (`Pooshit.Scripts/GuardedExecution.cs`)

**Owns:** allocating the live counters for one execution and owning the deadline's lifetime. Unchanged responsibility; it remains the single site (A2).

`Prepare` gains **one optional parameter**, `inheritedVariableBudget`, resolved by the same `??` shape as the existing `inheritedDepthBudget` on the adjacent line. Semantics: *"variable budget inherited from a caller whose variable scope this execution runs inside, used as-is instead of allocating a fresh one, or `null` for an execution with its own scope root."*

**Why a parameter and not a second `PrepareForDispatch`.** DRY math (#1267): the allocation-plus-arm block is `GuardedExecution.cs:50-64` ≈ **13 lines**; sites after this design = **3** (`Execute`, `ExecuteAsync`, `InvokeAsExecution`). `13 × 3 = 39`, far above the ~15-20 threshold. A parallel method would duplicate the CTS arm/dispose logic — the exact logic whose lifetime bug is #7885. **One site, one optional parameter.**

**The counter-math, for the block that is *not* extracted.** The `using` + `try`/`catch (OperationCanceledException)` + `Convert` wrapper is ≈ **5 lines** at **3** sites = **15**, at the low end of the #1267 band. It is **not** extracted, and the reason is not "three near-identical lines": the three bodies genuinely differ in their invocation shape — `script.Execute(ctx)` (sync), `await Task.Run(…, executionToken)` (async), and `InvokeCore(governing, depth, args)` (with the depth carve-out). A shared wrapper would need a delegate parameter, allocating a closure per execution on the hot path and named with an awkward verb-noun-of-noun. That is indirection, not abstraction (#1136 §4). **Math stated: 5 × 3 = 15, at threshold, and the bodies are not near-identical.**

### 8.3 `ScriptContext` (`Pooshit.Scripts/ScriptContext.cs`)

**Owns:** the per-scope view of a run — arguments chain, type provider, token, limits, and references to the run's counters.

- **Deleted:** `internal ScriptContext(ScriptContext context, DepthBudget depthBudget)` (`:29-35`). Its only caller is `LambdaMethod.InvokeCore:99` (A5).
- **Added:** `internal ScriptContext(ScriptContext scope, ScriptContext governing, DepthBudget depthBudget)`.
  - `Arguments` ← a new child `VariableProvider` over `scope.Arguments` (**the closure chain — this is what makes a lambda a closure and must come from the defining context**).
  - `TypeProvider` ← `scope.TypeProvider` (the body was parsed against it).
  - `Limits`, `StepBudget`, `VariableBudget`, `CancellationToken` ← `governing`.
  - `DepthBudget` ← the explicit parameter.

Net: **zero new constructors**; one is replaced by a strictly more capable one. No public surface. The generalisation *is* the #7886 fix (§9).

### 8.4 `DepthBudget` (`Pooshit.Scripts/DepthBudget.cs`)

**Owns:** the physical-stack bound. Gains one **internal** method, `ResetBreach()` — a single `Interlocked.Exchange(ref breached, 0)`, called **only** from `InvokeAsExecution` (§6.5). `depth`, `Enter`, `Exit`, `CheckBreached` and `Limit` are untouched.

### 8.5 Unchanged, explicitly

`StepBudget`, `VariableBudget`, `ScriptLimits`, `Script`, `TaskHost`, `ExternalScriptMethod`, `LambdaToken`, `IExternalMethod`, `EnumerableExtensions`. No new types. No new configuration.

---

## 9. `InvokeFrom` — from depth-only to the whole governing budget set (#7886)

Today `InvokeFrom(invokingContext, args)` calls `InvokeCore(invokingContext.DepthBudget, args)`, and `InvokeCore` merges that single budget into the **captured** context. Steps, variables, limits and cancellation still resolve from the definer. §12 prescribes `InvokeFrom` as *the* correct pattern for a host extension — so a host that follows the instruction exactly still gets the #7880 symptom. The doc is not wrong so much as **silently partial**, which is worse: it converts a correct-looking fix into a non-fix.

**Fix:** `InvokeFrom` calls `InvokeCore(governing: invokingContext, depthBudget: invokingContext.DepthBudget, arguments)`. With §8.3's constructor, the lambda body then runs with the invoking run's `StepBudget`, `VariableBudget`, `Limits` and `CancellationToken`, over the **defining** context's closure chain. The change is one call-site edit made possible by the constructor generalisation — no new mechanism.

**Blast radius is narrower than it looks.** When definer and invoker are the *same* execution — which covers **every in-repo call site** (`EnumerableExtensions.cs:34,211,260`; `ScriptMethod.cs:93` → `IExternalMethod.Invoke` → `InvokeFrom`) — the budget objects are already the same instances, so there is **no observable change**. The change bites exactly one shape: a host extension invoking a lambda that was defined in a *different* execution. That is the Uberkarl §12-remedy shape, and getting it right is the point.

**Is charging the invoker an escape?** No. `Limits` are per-execution *policy* set by the host, not per-script capabilities. A host that passes a lambda defined under strict limits into an execution with loose limits chose both. Running a lambda under the limits of the execution it is running in is the correct attribution and the only one the host can reason about.

---

## 10. What breaks — concrete table

| # | Change | Who is affected | Direction / justification |
|---|---|---|---|
| **B1** | `LambdaMethod.InvokeFrom` resolves `StepBudget`, `VariableBudget`, `Limits` and `CancellationToken` from the **invoking** context, not the defining one. | A host extension invoking a lambda defined in a **different** execution. **No change when definer == invoker**, which is every in-repo call site and the overwhelmingly common host shape. | Behavioural **fix** (#7886). The documented §12 remedy starts working. |
| **B2** | New public `LambdaMethod.InvokeAsExecution(params object[])` and `InvokeAsExecution(CancellationToken, params object[])`. | Additive. | No break. **Requires OQ-1** (public-API latitude at 1.0). |
| **B3** | A lambda dispatched via `InvokeAsExecution` gets a **fresh `StepBudget`** per dispatch. | A host relying on the *broken* cumulative behaviour as a crude lifetime cap. | **Deliberate.** #7882 §5 establishes there is no supported way to get a per-dispatch budget today, so nobody can depend on the fixed behaviour; and the cumulative behaviour was never documented as a lifetime cap — it is the bug. A host that genuinely wants a lifetime cap counts dispatches itself. |
| **B4** | A lambda dispatched via `InvokeAsExecution` gets a **freshly armed deadline**. Cached handlers that legitimately ran longer than the configured `Timeout` **start throwing `ScriptTimeoutException` where they never did.** | Any host that configures `Timeout` **and** caches handlers. | **Deliberate** (#7885) — this is closing an *under*-enforcement hole in a secure-by-default 1.0. **Sharp edge:** the very first dispatch can now throw. Answering #7885's open question (does `BehaviorLoader` configure `Timeout`? — OQ-5) determines whether this bites Uberkarl on landing. |
| **B5** | `InvokeAsExecution` does **not** observe the captured `CancellationToken`; the host supplies one. | A host relying on a captured long-lived token to kill cached handlers. | Must pass the token explicitly. Justified in §6.6: the captured token is either a dead deadline token or an ambiguous host token whose "poison" failure mode is a permanent quarantine. |
| **B6** | The `DepthBudget` sticky breach latch is cleared at the start of each `InvokeAsExecution`. A cached lambda that once breached depth is no longer permanently dead. | Any host caching a lambda that has ever breached `MaxDepth`. | **Deliberate** (§6.5). Within a run the latch is unchanged and still defeats a swallowed abort. |
| **B7** | `LambdaMethod.Invoke(params object[])` — **explicitly unchanged.** | — | #7736 §7.7 / #7782 stand. `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` and `Depth_RecursionThroughInvokeArgsExtensionAtEveryLevelThrows` stay green **without modification**. |
| **B8** | `InvokeOnNewStack` / `task.run` — **explicitly unchanged.** | — | Fresh `DepthBudget`, shared everything else, as today. |
| **B9** | Docs: §12's `InvokeFrom` paragraph and `LambdaMethod.Invoke`'s XML remarks stop being depth-only; a new host-dispatch paragraph lands. | Every reader of §12. | Required — both become wrong on landing (§12 of this doc). |
| **B10** | internal `ScriptContext(ScriptContext, DepthBudget)` is replaced by `ScriptContext(ScriptContext, ScriptContext, DepthBudget)`; `GuardedExecution.Prepare` gains an optional parameter; `DepthBudget` gains an internal `ResetBreach()`. | Internal only. | No external break. |
| **B11** | Per-dispatch allocation: one `GuardedExecution`, one `ScriptContext`, plus a `StepBudget` **only when `MaxSteps` is set** and a `CancellationTokenSource` + timer **only when `Timeout` is set**. | Hosts on hot dispatch paths (Uberkarl: 60 dispatches/s/object). | `ScriptLimits.Default` sets neither (A9) → **the default path allocates neither**. When configured, the cost is two small objects and one timer registration per dispatch, against an interpreted script body — orders of magnitude below noise (the §7.5 cost argument). |

**Nothing in the public surface is removed or renamed.** Under SemVer this is a MINOR release (additive API + a bug-fix behaviour change to `InvokeFrom`), not a MAJOR.

---

## 11. Test plan — the fix must land with all of these

The five named in the brief, plus five that pin the decisions this design makes. All against a **test-local** host extension where an extension is needed, never `EnumerableExtensions` (#7782's rule — converting an in-repo caller must never be able to make a test pass without fixing the method under test).

| ID | Test | Asserts | Pins |
|---|---|---|---|
| **T1** | Parse once, cache the lambda, `InvokeAsExecution` **10 000×** under `MaxSteps = 1000` with a small fixed-cost body. | No throw. | #7880 — the step reset. Fails today at ~invoke #77. |
| **T2** | Same lambda, `Timeout = 100 ms`, body sleeps ~300 ms. Dispatch three times. | `ScriptTimeoutException` on **every** dispatch **including the first**. | #7885. "Including the first" is load-bearing: it fails today (measured: three ~900 ms invokes all completed under a 300 ms `Timeout`) and it fails again under any fix that only *resets* rather than *re-arms*. |
| **T3** | Host extension declaring a trailing `ScriptContext` and calling `InvokeFrom`; definer `MaxSteps = 100`, invoker `MaxSteps = 1_000_000`; 500 iterations. | Completes. | #7886. Fails today with `ScriptStepLimitExceededException` at the definer's limit. |
| **T4** | **NEGATIVE.** A script loops through a host extension that calls `Invoke(args)` on a cached lambda, total work well above `MaxSteps`. | Throws `ScriptStepLimitExceededException`. **Must NOT complete.** | §7 — the sandbox hole. **This is the test that fails if the fix resets steps unconditionally**, i.e. if `Invoke` is ever given scope-opening behaviour. |
| **T4b** | Same shape, but the extension calls `InvokeFrom(context, args)` — the §12-prescribed pattern. | Throws `ScriptStepLimitExceededException`. | §9 — generalising `InvokeFrom` must not open the hole either. The invoking context's budget is the running script's, so the bound holds. |
| **T5** | The #7736 §7.7 counter-example, **unmodified**: recursion routed through the test-local `Invoke(args)` extension at every level, finite literal (≈32, well above `MaxDepth`, well below the measured crash depth). | Throws `ScriptDepthLimitExceededException`, **not** `StackOverflowException`. | §6.3. This is the **existing** `Depth_RecursionThroughInvokeArgsExtensionAtEveryLevelThrows`. **It must stay green with no edit.** |
| **T6** | **The T5 analogue for the new door.** Recursion routed through a test-local extension calling `InvokeAsExecution` at every level, same finite literal. | Throws `ScriptDepthLimitExceededException`, **not** `StackOverflowException`. | §6.3 — the depth carve-out **on the new entry point**. This is what fails if someone later "helpfully" gives `InvokeAsExecution` a fresh `DepthBudget`. **Arguably the single most important new test in this plan.** Its literal must be finite for the same reason T5's is: under that regression an unbounded literal would kill the test runner instead of failing the assertion. |
| **T7** | (a) Drive a cached lambda to a depth breach, then `InvokeAsExecution` it again with a shallow argument. (b) `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` re-run **unmodified**. | (a) succeeds — the latch cleared. (b) still green — the latch still sticky *within* a run. | §6.5 — both halves of the latch decision, and B7. |
| **T8** | (a) `InvokeAsExecution(hostToken, …)` with `hostToken` cancelled mid-dispatch. (b) A cached lambda whose *captured* token is cancelled, dispatched via `InvokeAsExecution(CancellationToken.None, …)`. | (a) `OperationCanceledException`, **not** `ScriptTimeoutException`. (b) completes normally. | §6.6 / B5 — the host token is observed; the captured token is not. |
| **T9** | *N* threads × *M* `InvokeAsExecution` on **one** cached lambda, `MaxSteps` sized so a single dispatch barely fits. **Reuse #7888 E15's exact shape — 8 threads × 5 000 invokes at `MaxSteps=100000`** — since that is the measured failing baseline (*"7/8 threads failed; total successful invokes = 33330"*). | Zero failures. | The directive's "per execution context" clause, for the boundary where it is genuinely broken today. Sizing the test to the known-red configuration is what makes it load-bearing rather than presence-bearing. |
| **T10** | A lambda that stashes a large value in a **closure** variable (defined outside the lambda body, assigned from inside it), dispatched repeatedly under a `MaxVariableBytes` the closure exceeds. | Throws `ScriptVariableLimitExceededException`. | §6.4 — the memory guard is not weakened. **This is what fails if someone later reallocates the `VariableBudget` with a re-derived `root`.** Note it must stay green *alongside* #7888 E7b/E14's 20 000-clean-invokes result: the guard fires on a genuinely oversized closure and stays silent otherwise. |

**Not tested, deliberately:** the misuse case where a host extension reached from a running script calls `InvokeAsExecution`. It is documented host misconfiguration (§7 residual), the same class as §12's delegate caveat; a test asserting it is bounded would assert something false, and a test asserting it is *unbounded* would pin a hole as a contract.

---

## 12. Documentation debt — required on landing

1. **`docs/pooscript-language-reference.md` §12.** The paragraph beginning *"On **every engine-dispatched invocation** … the budget is resolved from whichever context is actually invoking the lambda"* is currently a true statement **about depth** that reads as a general statement about budgets (#7886). It must (a) stop being depth-only and say plainly that `InvokeFrom` now resolves the whole budget set from the invoking context, and (b) gain a host-dispatch paragraph naming `InvokeAsExecution`, its per-dispatch step and deadline semantics, its **inherited** depth semantics, the host-supplied-token requirement, and the misuse warning (never from inside a host extension). The §12 sub-bullet listing *"what the engine still cannot guarantee"* must record that a cached handler dispatched via plain `Invoke` has **no** deadline — and that `InvokeAsExecution` is the fix.
2. **`LambdaMethod.Invoke` XML remarks.** Currently depth-only. Must state that `Invoke` also charges the **defining** run's step and variable budgets and observes the **defining** run's token, and must point host-dispatch callers at `InvokeAsExecution`. Its "It remains correct where there is genuinely no invoking context: host C# code, or a test driving a lambda returned from `IScript.Execute`" sentence is now **wrong as guidance** — that audience wants `InvokeAsExecution`. `Invoke` remains correct as the *conservative* choice, and the remarks should say exactly that.
3. **`LambdaMethod.InvokeFrom` XML remarks.** Must state the generalised contract: the whole governing budget set comes from the invoking context; the closure scope comes from the defining one.
4. **`LambdaMethod.InvokeAsExecution` XML remarks.** The four-counter table in prose, plus the misuse warning (§7 residual) named concretely, not as a bare "prefer X".
5. **`docs/architecture/execution-guards-depth-memory.md`.** §7.7's entry-point table gains the fourth row; OQ-9 is marked **partially resolved** (public surface exists for host-thread dispatch; the shared-depth residual stands, §3 non-goals). §7.7's decision itself is **not** amended — add a cross-reference to this document.
6. **This document** is the design of record for boundary 4; #7736 remains the design of record for the guard model itself. Neither supersedes the other; both stay live.

---

## 13. PR decomposition — recommended

**Two PRs, in dependency order.** One feature per PR (#1165 / the operator's rule); the three DiVoid items do **not** map to three PRs, and the reason is coupling, not convenience.

### PR 1 — `InvokeFrom` resolves the whole governing budget set (#7886)

- `ScriptContext`: replace the depth-only substituting ctor with the `(scope, governing, depthBudget)` ctor.
- `LambdaMethod`: `InvokeCore(governing, depthBudget, arguments)`; re-point `Invoke`, `InvokeFrom`, `InvokeOnNewStack`.
- Docs: §12's `InvokeFrom` claim; `InvokeFrom` XML remarks.
- Tests: **T3**, **T4b**. **T5** and `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` must stay green unmodified — that is this PR's main regression surface.

Small, independently valuable (it makes the documented §12 remedy actually work), and independently reviewable. **Does not unblock Uberkarl on its own.**

### PR 2 — host-dispatched lambdas get a fresh budget scope (#7880 + #7885)

- `GuardedExecution.Prepare`: `inheritedVariableBudget` parameter.
- `DepthBudget`: internal `ResetBreach()`.
- `LambdaMethod`: the two `InvokeAsExecution` overloads.
- Docs: §12 host-dispatch paragraph; `Invoke` + `InvokeAsExecution` XML remarks; #7736 §7.7 table row + OQ-9 note.
- Tests: **T1, T2, T4, T6, T7, T8, T9, T10**.

**Unblocks Uberkarl PR #33** (plus a one-line Uberkarl call-site change, OQ-3).

### Why #7885 is not its own PR

The timeout fix **is** the `GuardedExecution` reuse. You cannot open a fresh step scope through `Prepare` without also arming and disposing the deadline it owns — they are the same six lines. Splitting them means building the scope twice and throwing one away. This is not "bundling three counter-semantics changes into one review": PR 2 is **one** feature — *a host dispatch is an execution* — and the deadline is one of the four counters that an execution scopes. The counter-semantics change that genuinely is separable (#7886, which is about a different method and a different question) **is** its own PR.

### Why PR 1 first

PR 2 needs PR 1's `InvokeCore(governing, depth, args)` signature. Building PR 2 first would mean a throwaway intermediate shape. PR 1 is small; the ordering costs little.

---

## 14. Implementation guidance — ordered phases

**Phase 1 (PR 1).**

1. Add `internal ScriptContext(ScriptContext scope, ScriptContext governing, DepthBudget depthBudget)` per §8.3. Delete `internal ScriptContext(ScriptContext, DepthBudget)`; its only caller is `LambdaMethod.InvokeCore:99`.
2. Change `InvokeCore` to `(ScriptContext governing, DepthBudget depthBudget, object[] arguments)`; build the lambda context from `(this.context, governing, depthBudget)`.
3. Re-point: `Invoke` → `(context, context.DepthBudget, args)`; `InvokeOnNewStack` → `(context, freshBudget, args)`; `InvokeFrom` → `(invokingContext, invokingContext.DepthBudget, args)`.
4. Verify `Invoke` and `InvokeOnNewStack` are **semantically identical** to before. They are: `(context, context, context.DepthBudget)` reproduces the old `new ScriptContext(context, context.DepthBudget)` field-for-field.
5. Docs per §12 items 1 (`InvokeFrom` half) and 3. Tests T3, T4b.

**Phase 2 (PR 2).**

6. `GuardedExecution.Prepare`: add `DepthBudget inheritedDepthBudget = null, VariableBudget inheritedVariableBudget = null`; the variable-budget line mirrors the existing depth line — `inheritedVariableBudget ?? (limits.MaxVariables.HasValue || limits.MaxVariableBytes.HasValue ? new VariableBudget(…) : null)`.
7. `DepthBudget.ResetBreach()` — internal, one `Interlocked.Exchange`. **Do not call it from `Prepare`** (§6.5 — the import boundary also inherits a depth budget and must keep the latch).
8. `LambdaMethod.InvokeAsExecution(CancellationToken, params object[])` per §8.1's six obligations, and the one-line `params`-only overload delegating with `CancellationToken.None`.
9. **Verification item (§16 R5):** confirm the `callertoken` handed to `Prepare` on this path is never a token from a disposed `CancellationTokenSource`. By design it is the host's own token, so it should not be — but assert it, because the failure would be an `ObjectDisposedException` surfacing from `CreateLinkedTokenSource` on a path that only fires when `Timeout` is configured.
10. Docs per §12 items 1 (host-dispatch half), 2, 4, 5. Tests T1, T2, T4, T6, T7, T8, T9, T10.

**Do NOT:**

- Change `Invoke`'s semantics, deprecate it, or edit `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` / `Depth_RecursionThroughInvokeArgsExtensionAtEveryLevelThrows` (§10 B7; #7782 — a failure there is a re-argued decision, never a silent test edit).
- Give `InvokeAsExecution` a fresh `DepthBudget` (§6.3; T6).
- Reallocate the `VariableBudget` at boundary 4 (§6.4; T10).
- Add an ambient execution tracker (§7 option A; §16 R1).
- Add a `Reset()` to `StepBudget` (§6.1).
- Make `StepBudget` / `DepthBudget` / `VariableBudget` public, or add `ScriptLimits` knobs (§3).

---

## 15. Pre-Design Checklist (#1136 §5) — answered in order

### KISS / DRY / YAGNI

- **No new type mirroring an existing type.** Zero new types. The design reuses `GuardedExecution`, `ScriptContext`, `DepthBudget`, `VariableBudget`, `StepBudget`, `LambdaMethod` under their real names (#6836) and invents no parallel vocabulary. An "inherited budgets" carrier struct was considered for `Prepare`'s two optional parameters and rejected as a mirror type (#1136 §6).
- **No abstraction with one implementation.** None added. `InvokeAsExecution` is a method, not an interface.
- **No element justified by "we might need X later".** `InvokeAsExecution` has a named consumer holding a blocked PR (Uberkarl #7879/PR #33) — the exact condition #1184 requires. The `CancellationToken` overload exists because dropping the captured token without a replacement is a *regression* (§6.6), not because a host might want cancellation someday.
- **No deprecation period, feature flag, compatibility shim, or transition window.** None. `Invoke` is not `[Obsolete]`-marked — #7736 §7.7 option (b) rejected that, and this design strengthens the rejection: `Invoke` is now the *conservative, safe* choice, so warning on it would warn on correct code.
- **DRY math quoted.** `Prepare`'s allocate-and-arm block: `13 lines × 3 sites = 39` → far above ~15-20 → **shared** (one optional parameter, not a parallel `PrepareForDispatch`). The `using`+`catch`+`Convert` wrapper: `5 lines × 3 sites = 15` → at threshold, **not extracted**, with the math-grounded reason that the three bodies differ in invocation shape and the extraction would be a delegate-taking wrapper — indirection, not abstraction (§8.2).

### Existing systems first

- **Audited.** `GuardedExecution.Prepare` already allocates every counter and arms the deadline (A2), already supports depth inheritance (A3), and `ExternalScriptMethod.Invoke` already implements *exactly* this boundary shape for imports (A4). The design adds **one optional parameter** to the existing site rather than a new one.
- **No new layer proposed**, so the "name the concrete reason it can't live on the existing surface" bar does not apply. The one genuinely new surface — `InvokeAsExecution` — is justified in §7 as the only discriminator that cannot be opened by script choice, with the four alternatives enumerated and rejected.
- **No new persisted data.** None (a library, no store).
- **Consumer chain recursed.** `InvokeAsExecution` → Uberkarl `BehaviorLoader` per-frame dispatch → the blocked PR #33 → the shipping game. Named consumer, not a dead end.

### Configurability

- **No new config knob.** `ScriptLimits` is untouched. Every value in this design is a structural consequence, not a tunable.
- **No telemetry-then-tune compound.** None.
- **No magic numbers introduced.** None. Existing constants (`ScriptLimits.Default*`) are untouched.

### Less is better

- **Delete / merge / inline run on every element.** `InvokeAsExecution` — delete ⇒ either the bug stays or the sandbox hole opens; kept. The `CancellationToken` overload — merged into a single real method plus a one-line convenience; deleting it is a regression (§6.6). `DepthBudget.ResetBreach` — delete ⇒ a once-breached cached lambda stays permanently quarantined, the exact bug class; kept, 2 lines. `Prepare`'s `inheritedVariableBudget` — delete ⇒ the closure stops being charged, a memory-guard regression; kept. The 3-arg `ScriptContext` ctor — **merged**: it replaces the 2-arg one, so the type gains no constructor. **Explicitly deleted from the design:** reallocating the `VariableBudget` (§6.4), a `StepBudget.Reset()` (§6.1), the ambient execution tracker (§7 A / §16 R1), and any public budget surface (§3).
- **Trade-offs named explicitly.** §6.3's over-count cost (OQ-9's residual, stated not hidden), §6.4's `producedSinceLastPass` residual, §6.5's latch-clearing race, §7's host-misconfiguration residual, §10 B4's sharp edge (handlers start throwing), §10 B11's allocation cost.
- **Radical-clean where unconsumed.** The depth-only substituting ctor has exactly one caller and is **deleted**, not kept alongside its replacement.
- **Reader inventories.** §8's per-type table covers every changed member; §9's blast-radius paragraph enumerates every in-repo `InvokeFrom` call site by file:line (`EnumerableExtensions.cs:34,211,260`; `ScriptMethod.cs:93`). §12 enumerates every doc site that becomes wrong. Not representative cases — all of them.

### Data deliverables

Not applicable — no SQL, no migration, no backfill.

### Document discipline

- Cites Code Contracts (#114 §0) and Design Contracts (#1136) as load-bearing (header).
- Scope inventories explicit (§3), including a non-scope list.
- No multi-paragraph "rationale for keeping X" for things that obviously stay.
- **No predecessor superseded.** #7736 stays live and current; this document extends its model with a fourth boundary and cross-references §7.7 rather than overriding it. §12 item 5 requires the reciprocal cross-reference to land in #7736's own file.

---

## 16. Risks & Mitigations

| # | Risk | Mitigation | Residual |
|---|---|---|---|
| **R1** | **A host extension reached from a running script calls `InvokeAsExecution`** → fresh step budget and fresh deadline per bounce → DoS-shaped hole. | XML remarks name the failure and the remedy concretely; §12 names it alongside the delegate caveat; **`InvokeFrom` is now fully correct (§9)**, so there is no longer a reason to reach for the wrong method. | **Accepted**, same class as §12's existing "a host that willingly exposes a hole". An ambient in-execution check was considered as a backstop and **rejected**: it reintroduces §7 option A's ambient state (with its `Task.Run`/`task.run` thread-hop failure surface) to defend against a documented host obligation. If the misuse is ever observed in the wild, that is the moment to build it — with the real shape in hand (#1184). |
| **R2** | Latch-clearing race erases a concurrently-breached run's policy abort (§6.5). | The physical bound (`Enter`) is unaffected; worst case is a lost policy abort, never a process kill. | Accepted; conditional clearing rejected as nondeterministic. |
| **R3** | `Prepare` reaches six parameters. | Both inherited parameters are optional, default `null`, sit on adjacent lines with the same `??` shape, and each carries a one-line XML doc. | Accepted — the alternative duplicates the CTS lifetime logic whose bug is #7885. |
| **R4** | Hosts keep calling `Invoke` and stay broken. | Documentation (§12 items 1, 2) at the three places a host looks: §12, `Invoke`'s remarks, `InvokeAsExecution`'s remarks. | Accepted. `[Obsolete]` rejected (§15 YAGNI) — `Invoke` is correct, and now specifically the *safe*, choice. |
| **R5** | **`ObjectDisposedException` from `CreateLinkedTokenSource`.** The captured token belongs to a CTS `Dispose` already tore down (A10). | The design **never links to the captured token** — `callertoken` is host-supplied (§6.6). | **Verification item for the implementer** (§14 step 9). The hazard only exists on a path this design does not take; assert it rather than assume it, because the failure would surface only when `Timeout` is configured. |
| **R6** | Per-dispatch allocation on a 60 Hz hot path. | Zero extra allocation on the `ScriptLimits.Default` path (A9, B11). When `MaxSteps`/`Timeout` are configured: two small objects + one timer registration per dispatch, against an interpreted script body — orders of magnitude below the per-step cost (#7712 §7.6's cost argument, same direction). | Accepted. |
| **R7** | B4's sharp edge — cached handlers that legitimately exceeded `Timeout` start throwing on the very first dispatch after upgrade. | Break table row B4; §12 host-contract note; **OQ-5** asks the Uberkarl side whether `BehaviorLoader` configures `Timeout` **before** sizing the fix. | Deliberate — it is the closure of an under-enforcement hole in a secure-by-default 1.0. |
| **R8** | Someone later "simplifies" the depth carve-out away, or reallocates the variable budget. | **T6** and **T10** exist precisely to fail in those cases, and §14's "Do NOT" list names both. | Mitigated by test. |

---

## 17. Open questions for the operator

**OQ-1 — Public-API latitude at 1.0. (Blocking the design's chosen shape.)**
This design adds two public methods to `LambdaMethod` (purely additive; nothing removed, renamed, or signature-changed). #7712 A3 permitted additive/deprecating changes **pre-1.0**; 1.0 shipped today. **Is additive public surface acceptable (→ 1.1.0), or must this be additive-only through existing signatures?**
**Consequence of "no new public API", stated so the answer is informed:** the only remaining discriminator is §7 option A (ambient marker). That silently changes `Invoke`'s behaviour for every existing host, re-opens the #7736 §7.7 / #7782 decision, forces the `Depth_HostExtensionInvokeArgsSpuriouslyBreaches` characterisation test to be re-argued and edited, and introduces ambient state whose failure mode is silently-wrong budget attribution. **Recommendation: allow the additive surface.**

**OQ-2 — Name.** `InvokeAsExecution` (recommended — names the real mechanism, `GuardedExecution`, in the engine's own vocabulary) vs `InvokeDetached` / `InvokeGuarded` / `InvokeAsRun`. Low stakes but it is public surface at 1.0, so it is cheap to settle now and expensive later.

**OQ-3 — Uberkarl call-site change.** #7880 says Uberkarl "just bumps the package — no Uberkarl-side workaround". This design requires a **one-line change** (`onUpdate.Invoke(delta)` → `onUpdate.InvokeAsExecution(shutdownToken, delta)`) plus the bump. That is using the correct API, not a workaround — but it is more than a bump. **Confirm acceptable.** The alternative that requires literally zero Uberkarl change is §7 option A, not recommended (see OQ-1).

**OQ-4 — Should `InvokeAsExecution` refuse to open a scope when an execution is already in progress on the current thread?** Recommendation: **no**, for now (§16 R1). It is the ambient mechanism this design rejects as a discriminator, reintroduced as a backstop against a documented host obligation. Flagged because it is the one place a reasonable person could disagree.

**OQ-5 — Does Uberkarl's `BehaviorLoader` configure `ScriptLimits.Timeout`?** (#7885's own open question, unanswered.) If yes: its cached per-frame handlers currently run with **no watchdog at all** — the opposite of what the #7737 watchdog swap intended, making this a live risk in the running game rather than a latent one — **and** break-table row B4 bites on landing, since handlers that legitimately exceed the configured `Timeout` will start throwing. Worth answering before PR 2 is sized.

**OQ-6 — One internal tension inside the diagnosis (#7888), resolved here; confirm the resolution.**
#7888 says two different things about `VariableBudget`, and this design follows the one backed by experiment:

- Its **per-counter table** records `VariableBudget` → *"no reset — **benign**"*, *"level-based, not cumulative"*, and its **ruled-out** section reports E7b/E14: *20 000 invokes clean*.
- Its **trap 1** paragraph then lumps it in with the resettable counters: *"Steps/timeout/variables are per-**dispatch work** bounds; depth is a **physical-stack** bound."*

**This design follows the table, not the trap sentence** (§6.4): `VariableBudget` is inherited by reference and not reallocated, because (a) it measures an instantaneous footprint rather than a cumulative spend, and (b) reallocating requires re-deriving `root`, and any `root` other than the original host root stops the footprint walk **below the closure** — a memory-guard hole in a secure-by-default 1.0, introduced to fix a defect E7b/E14 prove does not exist. **T10 pins it.** Flagged because it is the one place this design's per-counter answer differs from a sentence in the diagnosis; the disagreement is with that sentence only, not with the diagnosis's own measurements. Recommend amending #7888's trap 1 wording to match its table.

---

## 18. Summary of decisions

1. **A host dispatch is an execution.** New public entry point `LambdaMethod.InvokeAsExecution([CancellationToken,] params object[])` opens a `GuardedExecution` — the same mechanism `Script.Execute` and the import boundary already use. No new machinery.
2. **Four counters, four answers.** `StepBudget` fresh; `Timeout` freshly armed **and disposed inside the dispatch** (this *is* #7885's fix); `DepthBudget` **inherited** (the carve-out — a fresh one reads depth 1 forever and turns a catchable abort into an uncatchable `StackOverflowException`); `VariableBudget` **inherited** (reallocating it would re-root the footprint walk below the closure and weaken the memory guard).
3. **The discriminator is a named entry point, not ambient state.** `Invoke` is unchanged and becomes the conservative, always-safe choice; the fresh-budget behaviour is unreachable by script choice, so the DoS hole cannot open. #7736 §7.7 / #7782 are re-read and **upheld, not amended**; their characterisation tests stay green unmodified.
4. **The depth breach latch resets per dispatch, `depth` does not** — a new run should not inherit a previous run's policy abort, and the physical bound is untouched.
5. **The captured `CancellationToken` does not carry**; the host supplies one, because the engine cannot distinguish a shutdown token (keeping it is right) from a load-scoped one (keeping it is a permanent quarantine).
6. **`InvokeFrom` is generalised** from depth-only to the whole governing budget set, which is what makes the documented §12 remedy actually work.
7. **Two PRs:** `InvokeFrom` generalisation first (small, independently valuable), then the host-dispatch scope (unblocks Uberkarl PR #33). #7885 rides in PR 2 **by construction** — it is the same six lines — not by bundling.
