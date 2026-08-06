# Architectural Document: Recursion-Depth & Variable-Usage Execution Guards

> **Repo path:** `docs/architecture/execution-guards-depth-memory.md` (committed; `master` @ `834adbf`). The DiVoid node **#7736** is the graph copy of this file — when the two diverge, this file wins and the node is re-synced from it.

**Repo:** `Pooshit.Scripting` (project `Pooshit.Scripts/`), branch point `master` @ `9b969ec`
**DiVoid:** task **#7715** · derived from design **#7712** (`docs/architecture/cancellation-support.md`, merged) · engine rule **#7718** · driver **#7407** (Uberkarl) · predecessor task **#7409** · QA **#7717** / **#7719** · operator crawl **#7711** · language reference **#2946**
**Contracts (load-bearing):** Design Contracts **#1136**, Code Contracts **#114 §0**, DRY threshold **#1267**, brief-anchoring **#1184**, principles-trump-design **#1333**
**Status:** design for implementation by john-backend-dev. No code in this document.

**Verified against merged code, not against #7712.** Two QA rounds changed details after #7712 was written. Where this document and #7712 disagree, this document reflects the merged tree at `9b969ec` and wins.

**Amended 2026-08-06 (DiVoid #7782)** — verified against `master` @ `834adbf`, the merged depth guard, not against this document's own earlier drafts. One contract decision added and its consequences threaded through: **§7.7** (new — the three `LambdaMethod` entry points, and why `Invoke(params object[])` keeps the captured budget), **§7.2** (row corrected), **§7.6** (defect closed; residual redirected to §7.7), **§11** item 6, **§11.5** (new bullet), **§11.6** (new — the host rule and the `pooscript-language-reference.md` §12 replacement wording), **§12/B19** (the "unchanged" claim qualified as a decision), **§13** (T9a/T9b/T9c), **§16** (R11 closed, R12 added), **§17.2** (OQ-9).

---

## 1. Problem Statement

#7409 gave the engine three opt-in guards on `ScriptParser.Limits` — `Timeout`, `MaxSteps`, `RegexTimeout`. They bound **time** and **work**. They bound neither of the two resources a runaway script can exhaust without ever ticking a checkpoint slowly enough to matter: the **CPU stack** and the **managed heap**.

#7712 §10 named both as residuals the engine does not guarantee:

> 4. **Unbounded memory allocation** — `$s = $s + $s` in a loop, or a huge single allocation.
> 5. **Stack overflow** from unbounded recursion (recursive lambdas, mutually recursive imports). `StackOverflowException` cannot be caught and terminates the process.

Item 5 is qualitatively worse than every other residual in that list: a `StackOverflowException` in .NET Core is **uncatchable and unrecoverable** — it terminates the process immediately, taking the host with it. No cancellation token, no step budget, no watchdog thread can intervene. Only a counted check *before* the recursion happens can.

**The user's ask, verbatim (2026-08-05):**

> "recursion depth guard sounds good, but heap exhaustion is a bit unexplored. The only memory which can add up from what i know is the variables dictionaries (aside from actual injected surfaces where you can exhaust memory, but that is under control of consumer)
> Could we have some optional variable usage guard? - light (number of entries) - hard (memory usage like blobs and such are also approximated)"

and on sizing the work:

> "not too hard to implement and a good milestone for something uberkarl could use."

**On where the boundary sits** (2026-08-05, resolving OQ-2) — this is the articulation the design uses throughout, in preference to any of its own phrasings:

> "we talk about usage - a 500MB host object already exists in memory and is not introduced by the script - so that much is okay, script should handle script, not the whole engine."

**On peak versus sustained usage** — this resolves the overshoot objection and is promoted to a stated design principle in §8.2:

> "with the peak usage i would say its okay - the guard should not work at system limits but be rather strict, so at worst a bad script has a really long string which is still in bounds but the limits are set that double the limits are not critical"

**On the threat model** (resolving OQ-4):

> "if its possible in one script - script imports are currently not expected to be enabled. but if its technically possible we have to expect it - its user authored scripts - thats why we go for all of this effort to be able to guard."

**The budget-with-headroom principle.** Taken together, the last two quotes define the contract this design delivers against: **the limit is a budget deliberately set below the point where overshoot hurts — not a cliff at the system boundary.** A host is expected to configure `MaxVariableBytes` strictly enough that reaching a small multiple of it is survivable. That framing is only defensible if the design **quantifies** the multiple rather than hand-waving it, which §8.2.4 does: **≤2× observed, ≤3× transient, for every shape the guard covers.**

**Business driver.** Uberkarl (#7407) runs **untrusted community scripts** as level behaviour. After #7409 it can interrupt a runaway script by time or step count. It still cannot survive a recursive lambda (process death) or a script that grows its variables until the host dies.

**Success criteria.**

1. A recursive lambda or a mutually-recursive `import` chain aborts with a typed, catchable exception instead of killing the process.
2. A script that grows its variable footprint without bound is aborted before the host's heap is exhausted, on a best-effort approximation the host can reason about.
3. Both knobs default off; a default-configured parser is byte-identical to `9b969ec`.
4. Both abort paths reach the host unwrapped through every dispatch wrapper (rule #7718) and cannot be swallowed by a script-level `try`/`catch`.

**Honest framing up front (#1136 §4, and answering the user's "not too hard").** The two halves are not the same size. The depth guard is small, exact, and genuinely "not too hard". The variable guard is **~2–3× the depth guard**, introduces a new approximation with real blind spots, and buys a bound on *sustained growth* — not heap containment. §14 states the cost breakdown and §16 puts the resulting decision to the user.

---

## 2. Scope & Non-Scope

### In scope

- A nullable `MaxDepth` knob on `ScriptLimits`, counting **call** depth through lambda invocation and imported-script invocation.
- Nullable `MaxVariables` (entry count) and `MaxVariableBytes` (approximated footprint) knobs on `ScriptLimits`.
- The exception contract for both, including their relationship to `ScriptException` and to rule #7718's passthrough list at **every** dispatch wrapper, and to the script-visible `try`/`catch`.
- The host-facing surface (`ScriptParser.Limits`), the host contract for what the guards do **not** cover, and the language-reference update.
- The test plan.

### Explicitly out of scope

| Item | Why |
|---|---|
| **Process / container isolation** | The only complete containment for a host object graph. Named in the host contract (§11) as the eventual answer for Uberkarl; **nothing is designed for it here.** |
| **The async interpreter rewrite (#7713)** | Filed separately. §7.4 records the one forward-compatibility constraint this design must respect so #7713 does not have to rip anything out. |
| **A reflection fast path for direct lambda calls** | The structural fix for the low `MaxDepth` ceiling (§11.5, R9). A real performance/architecture task with its own before/after measurement. Named in §11.5 and OQ-7; **nothing is designed for it here.** |
| **`ParseDelegate` hardening (#7716)** | The compiled path has no `ScriptContext` plumbing at all and is already documented as not-for-untrusted-code (#2946 §13). Unchanged here. |
| **Defect #2896** (`Foreach` re-enumeration) | Untouched, as in #7409. |
| **Bounding memory reachable through host-injected surfaces** | The user drew this line explicitly: *"aside from actual injected surfaces where you can exhaust memory, but that is under control of consumer"*. §8.4 states exactly where the measurement stops and why that is the same line. |
| **Any fourth/fifth guard beyond the two asked for** | #1184. Two knobs were asked for; §13 records the one seam that falls out at zero present cost and nothing else. |
| **Retro-fitting a `MaxDepth` default value** | No magic numbers (#1136 §3). The engine ships no opinion; §11 gives the host sizing guidance instead. |

---

## 3. Assumptions & Constraints

| # | Assumption | Confidence |
|---|---|---|
| A1 | Both target frameworks (`netstandard2.0`, `net8.0`) must keep building; `init` accessors work on `netstandard2.0` via the existing `IsExternalInit.cs` polyfill (gated `#if NETSTANDARD2_0`, verified by QA #7719). | Verified in tree |
| A2 | `ScriptLimits` knobs are `init`-only. This is **not** a style preference — it is the structural fix for CF-2 (#7717). Any new knob must be `init` for the same reason. | Verified (`ScriptLimits.cs:31,37,43`) |
| A3 | The interpreter is synchronous today: `Script.ExecuteAsync` wraps `script.Execute` in `Task.Run` (`Script.cs:106`). No interpreter frame spans an `await`. #7713 will change this. | Verified (`Script.cs`) |
| A4 | `List<object>` (`list`) and `IDictionary` (`dictionary`) are registered by **default** on every `ScriptParser` (`ScriptParser.cs:45,61`), so `$l = new list(); $l.add(x)` is available unless the host sets `TypeInstanceProvidersEnabled = false`. In-place value growth is therefore a mainstream shape, not an exotic one. | Verified |
| A5 | Pooscript variable names are **lexical** — `$name` tokens are produced at parse time; there is no runtime `setvar(computedName, …)`. The set of distinct variable names in one scope is bounded by the script source. | Verified (`ScriptVariable.cs`, `#2946 §4`) — **load-bearing for §8.3** |
| A6 | The engine creates a `Pooshit.Scripting.Parser.VariableProvider` for **every** derived scope (`ScriptContext.cs:16`). Only the *root* provider is host-supplied. All script-level assignments land in an engine-created provider, because the script body is a `StatementBlock` which derives a scope before the first statement runs. | Verified (`ScriptContext.cs:16`, `StatementBlock.cs:51`) |
| A7 | No consumer today configures any limit. Mamgo `scriptservice` and the `ScriptExecutor` CLI leave `Limits` at `ScriptLimits.None`. | Per #7712 §9.3 |

**Constraints.**

- **Backward compatibility is non-negotiable.** Default-configured behaviour must be unchanged, and the guarantee must be **structural**, not conventional — CF-2 (#7717) was exactly a convention-level guarantee failing in practice. §12 states the structural argument.
- **The `Guard()` hot path must stay near-free when nothing is configured.** #7712's whole guard design rests on this. `Guard()` runs per statement and per loop iteration.
- **The arithmetic hot path must stay near-free too.** M1/M2 (§8.2.1) sit on `ValueOperation.ExecuteToken`, which runs for every `+`, `*`, shift and bitwise op in every script — `$i = $i + 1` in a loop is the commonest shape in the language. When no variable knob is configured the budget is `null` and the cost is one null check; when configured, the numeric path costs two failed type tests. Pinned by T18c.
- **Rule #7718 binds:** every catch-all dispatch wrapper must pass the abort-shaped exceptions through. §9 enumerates all of them from the merged code.

---

## 4. Architectural Overview

Both guards slot into the **existing** three-part shape #7712 established. Nothing new is invented.

```
ScriptParser.Limits : ScriptLimits          (init-only knobs, opt-in, default null)
   │  captured at Parse()
   ▼
Script.Execute / ExecuteAsync
   │  GuardedExecution.Prepare(...)  ── allocates a budget object ONLY per configured knob
   ▼
ScriptContext ── holds by reference ──►  StepBudget?      (existing, monotonic)
   │                                     DepthBudget?     (NEW, enter/exit)
   │                                     VariableBudget?  (NEW, sampled measurement)
   │  derived per block / loop / lambda / catch (copy ctor propagates all three references)
   ▼
   ├── Guard()                     per statement, per loop iteration, per element, per lambda
   │      token check → StepBudget?.Consume() → VariableBudget?.Observe(Arguments)   ◄── NEW (M3)
   │
   ├── ValueOperation.ExecuteToken  VariableBudget?.ChargeProducedValue(result)      ◄── NEW (M1+M2)
   ├── AssignableToken.Assign       VariableBudget?.ChargeProducedValue(value)       ◄── NEW (M1+M2)
   │      (two shared base classes — all 12 operators, all assignment forms)
   │
   ├── LambdaMethod.Invoke         DepthBudget?.Enter() … try … finally Exit()       ◄── NEW
   └── ExternalScriptMethod.Invoke DepthBudget?.Enter() … try … finally Exit()       ◄── NEW
                                    + inherited depth budget crosses into the nested script
```

**Three additive changes, zero refactors of merged code:**

1. `ScriptLimits` gains three `init`-only nullable knobs.
2. `ScriptContext` gains two nullable by-reference budget fields alongside the existing `StepBudget`.
3. A new abstract exception base collapses the abort-family passthrough at the four narrow wrapper sites (§9).

**Deliberately not done:** consolidating the three budget objects into one `ExecutionBudget`. Considered and rejected in §7.5 with the cost math — it refactors merged, QA-approved code for a hot-path gain that is below noise.

---

## 5. Components & Responsibilities

| Component | Owns | Does **not** own |
|---|---|---|
| `ScriptLimits` (existing, extended) | The host's declared thresholds. Immutable (`init`-only). | Any counting, measuring, or throwing. |
| `DepthBudget` (**new**, internal) | The current call depth of one logical execution and its ceiling. Enter/exit and the breach throw. | *Which* constructs count as a call — the two call sites decide that. Any notion of physical stack frames. |
| `VariableBudget` (**new**, internal) | All three mechanisms (§8.2.1): the single-value ceiling (M1), the monotonic growth trigger (M2), the measurement cadence and last measurement (M3), and the breach throw for both variable knobs. | The sizing heuristic itself (delegated to `VariableSizer`), and the definition of which providers are walkable. |
| `VariableSizer` (**new**, internal static) | Approximating the footprint of one `object`, cheaply, without reflection. | Deciding *when* to run. Correctness — it is explicitly an approximation (§8.5). |
| `StepBudget` (existing) | Step counting. | **Unchanged by this design.** |
| `ScriptContext.Guard()` (existing) | The single continue-or-throw checkpoint. Gains **one** line (M3). | Depth (enter/exit, not per-checkpoint) and the value ceiling (M1/M2, which fire where values are produced). |
| `ValueOperation.ExecuteToken` (existing) | Declaring "an arithmetic result was produced". One shared base for all twelve operators. | The ceiling policy. |
| `AssignableToken.Assign` (existing) | Declaring "a value entered an assignable slot". One shared base for every assignment form, including compound-assign. | The ceiling policy. |
| `LambdaMethod.Invoke` (existing) | Declaring "a script-level call is starting/ending" for lambdas. | The counting policy. |
| `ExternalScriptMethod.Invoke` (existing) | The same, for `import`ed scripts, **and** propagating the caller's depth into the nested execution. | The nested script's own step budget (deliberately not propagated — #7712 §7.8, unchanged). |
| `GuardedExecution.Prepare` (existing) | Allocating exactly the budget objects the configured knobs require, and nothing else. | — |
| `ScriptAbortException` (**new**, abstract) | Being the single catchable category "the engine stopped this script". | Carrying data — the concrete subtypes do. |

---

## 6. Interactions & Data Flow

### 6.1 Depth — the flow that makes the guard work

```
$fac = (n) => { if(n > 0) { $fac.invoke(n - 1) } }      ← recursive lambda
```

Per invocation:

```
ScriptMethod.ExecuteToken  →  MethodOperations.CallMethod  →  [reflection]  →  LambdaMethod.Invoke
                                                                                    │
                                                       DepthBudget.Enter()  ────────┤ throws if depth+1 > MaxDepth
                                                                                    │
                                                       expression.Execute(lambdacontext)
                                                                                    │
                                                       DepthBudget.Exit()   ────────┘ in `finally`
```

The budget object is reached through the **captured** context the `LambdaMethod` holds, and it is the *same object instance* as the root execution's, because the copy constructor propagates it by reference. §7.1 explains why that is the only shape that works.

### 6.2 Imports — depth crosses, steps do not

```
a.poo:  import("b").invoke()        b.poo:  import("a").invoke()
```

`ExternalScriptMethod.Invoke` (`Data/ExternalScriptMethod.cs:25`) receives the **caller's** `ScriptContext`. It:

1. `Enter()`s the caller's depth budget (throwing on breach).
2. Executes the nested script, **handing the same `DepthBudget` instance to the nested execution** so the nested script's own lambda invocations continue the same count.
3. `Exit()`s in a `finally`.

The nested script still gets its own `StepBudget` (a fresh one from its own parser's limits, or none) — that is #7712 §7.8, unchanged. §7.3 justifies the asymmetry.

### 6.3 Variable footprint — sampled at the existing checkpoint

**M1/M2 — where a value is produced (immediate, `O(1)`):**

```
ValueOperation.ExecuteToken(result)  ─┐
AssignableToken.Assign(value)        ─┴→ VariableBudget?.ChargeProducedValue(v)
                                              │
                                              ├─ size(v) > MaxVariableBytes → THROW      (M1: ceiling)
                                              └─ producedSinceLastPass += size(v)        (M2: trigger)
                                                    └─ over budget → force a pass now
```

**M3 — the sampled ground truth (at the existing checkpoint):**

```
Guard()  →  token check
         →  StepBudget?.Consume()
         →  VariableBudget?.Observe(context.Arguments)      ← the only new line in Guard()
                 │
                 ├─ interlocked tick; below the next interval and M2 not tripped → return (common case)
                 └─ otherwise → Measure(chain) → compare → throw, or re-arm the interval and reset M2
```

`Measure` walks the chain of **engine-created** `VariableProvider`s from the current scope to the last one before a foreign provider, summing entries and (only when `MaxVariableBytes` is configured) approximated bytes.

---

## 7. The Depth Guard — decisions

### 7.1 Where the counter lives — and why the obvious answer is wrong

**Decision: a `DepthBudget` object held by reference on `ScriptContext`, propagated by the existing copy constructor, incremented and decremented explicitly at two call sites.**

The brief asked whether the existing context-derivation mechanism gives depth for free. **It does not — it counts the wrong thing twice over**, and both errors are silent:

**Error 1 — context derivation is not call depth.** `new ScriptContext(context)` occurs at **six** sites in the merged tree, and only one of them is a call:

| Site | Is it a call? |
|---|---|
| `Control/StatementBlock.cs:51` | No — a `{ }` block |
| `Control/While.cs:26` | No — a loop scope |
| `Control/For.cs:32` | No — a loop scope |
| `Control/Foreach.cs:42` | No — a loop scope |
| `Control/Try.cs:40` | No — a `catch` scope |
| `Providers/LambdaMethod.cs:43` | **Yes** |

A counter incremented in the copy constructor would fire on a deeply-nested-but-finite script (nested `if`/`for` blocks), which is a false positive on entirely legitimate code.

**Error 2 — and this is the one that would make the guard silently useless — the context chain does not grow under recursion at all.** `LambdaToken.Execute` (`Tokens/LambdaToken.cs:53`) captures a context at **lambda-creation** time and hands it to the `LambdaMethod`. `LambdaMethod.Invoke` (`Providers/LambdaMethod.cs:43`) then derives its invocation scope from that **captured** context — not from the caller's. So every invocation of `$fac`, at any recursion depth, derives from the *same* creation-site context. A depth counted along the context chain would read **1 forever** while the physical stack grows without bound.

Both errors are avoided by the same choice, which is also the shape #7712 §7.6 already established for `StepBudget`:

> "A step counter must therefore live in a **separate small object held by reference** and propagated by the derived constructor alongside the token."

`DepthBudget` is that same shape. Because the object is shared by reference from the root, incrementing it at `LambdaMethod.Invoke` counts real call nesting regardless of which context the lambda captured.

**Rejected alternative: `[ThreadStatic]` ambient depth.** It is simpler (no plumbing), and it is arguably *more* correct for the hazard, since the thing being protected is the physical per-thread stack — it would also cover host re-entry and foreign `IScript` implementations for free. It is rejected on one concrete, filed ground: **#7713 makes the interpreter async.** Once an interpreter frame spans an `await`, the increment and the decrement can run on different pool threads; the counter corrupts (one thread leaks, another goes negative) and #7713 would have to rip it out. `ScriptContext` flows with the logical call, so the context-carried budget survives that rewrite untouched. This is not speculative future-proofing (#1136 §1 YAGNI) — #7713 is a filed task that rewrites exactly these call sites.

**Rejected alternative: `AsyncLocal<int>`.** Survives #7713, but a write to an `AsyncLocal` copies the execution context, and the write would sit on the lambda-invocation path — the single highest-traffic script-level call site in the engine (`LambdaMethod.cs:29-34`). A by-reference field read is free; an execution-context copy is not.

### 7.2 What counts as one unit of depth

**Decision: exactly two constructs increment depth.**

| Construct | Site | Counts |
|---|---|---|
| Lambda invocation | `Providers/LambdaMethod.InvokeCore` — the shared body behind all three entry points | **Yes** — the single choke point for every lambda call, whether reached by reflection (`$f.invoke(…)`), by a host extension (`.where(…)`, `.indexof(pred)`, any host-registered method taking a `LambdaMethod`), or by `task.run`. **All three entry points count; they differ in *which* budget they count against — see §7.7.** |
| Imported-script invocation | `Data/ExternalScriptMethod.cs:25` (`Invoke`) | **Yes** |
| Block / loop / `catch` scope | the four other derivation sites | **No** — see §7.1 Error 1 |
| Host method call (reflected) | `MethodOperations.CallMethod` | **No** — the engine does not recurse there; the host's own stack usage is host-contract territory (§11). |
| Operator / member / indexer evaluation | — | **No** — bounded by the parse tree's depth, which is bounded by the source text. |

Two sites. `block_size × site_count = 5 × 2 = 10` (#1267) — **below** the ~15–20 threshold, so no additional helper is extracted beyond `DepthBudget`'s own `Enter`/`Exit` pair, which is the object's API rather than a DRY extraction.

**`Enter`/`Exit` must be paired in a `try`/`finally`** at both sites so that a `Return`, a `Break`, a script `throw`, a cancellation, or any abort unwinds the counter correctly. This is the one implementation detail most likely to be got wrong; T5 in §13 pins it.

### 7.3 Does depth cross the import boundary? — **Yes**, and steps still do not

**Decision: the depth budget crosses `import`; the step budget continues not to (#7712 §7.8, unchanged).**

If depth did not cross, `a.poo` importing `b.poo` importing `a.poo` would reset the count to zero at every hop and recurse to process death — and mutually-recursive imports are one of the **two vectors #7712 §10 item 5 names by name**. A guard that misses half its stated target is not a guard.

**Why the two budgets legitimately differ.** A step budget bounds *work*, and work is a per-script allowance — it is reasonable for an imported library script to be granted its own. A depth budget bounds a **shared physical resource**: there is exactly one CPU stack, and both scripts consume it. Partitioning an allowance is coherent; partitioning a stack is not. That asymmetry is the whole justification and it should be recorded in the `ExternalScriptMethod` XML remarks next to the existing §7.8 note.

**Mechanism (as implemented).** `GuardedExecution.Prepare` accepts an **inherited `DepthBudget`**: when the caller supplies one it is used as-is and the nested script's own `MaxDepth` allocates nothing. `ExternalScriptMethod` holds an `IScript`; the engine's own implementation is the internal `Script` class, reached through an internal entry that passes the caller's budget down. A **foreign** `IScript` implementation falls back to the public `Execute(variables, token)` and does **not** inherit depth — a documented host-contract limitation (§11), not a defect.

**Whose `MaxDepth` applies? — one ceiling for the whole nest: the outermost one wins.** `DepthBudget` carries a **single fixed `Limit`**, set when it is first allocated, exactly as `StepBudget` does. `GuardedExecution.Prepare` accepts an inherited budget; **if the caller already has one, it wins** over whatever the nested script's own `MaxDepth` would have allocated. A nested script never allocates a second budget and never re-reads a second ceiling.

*(Position changed. An earlier draft of this section specified per-frame ceilings — "each frame enforces its own script's ceiling against the shared count". That is wrong, and the ambiguity is what produced the implementer's question. The correction is below.)*

Three reasons the single-ceiling shape is not merely simpler but **more correct**:

1. **The resource is shared, so the bound must be too.** There is one stack. Under per-frame ceilings the effective bound would depend on which frame happens to be executing at the moment of the check — the same nest would abort at different depths depending on interleaving. A single ceiling for a single resource is the coherent model.
2. **It is the right outcome for the driver.** For Uberkarl the untrusted community script is the **entry point**, so its budget is allocated first and inherited by everything it imports. Its tight ceiling governs the entire nest — precisely the containment wanted. The inverse case (a trusted outer script importing an untrusted inner one) leaves the inner script bounded by the outer's ceiling, which still bounds the shared stack, and the host chose to import it.
3. **Per-frame ceilings earn nothing.** Nothing in the §13 test plan requires them, no consumer distinguishes them, and Toni has confirmed imports are **not expected to be enabled** in Uberkarl at all (§1, OQ-4 quote), so the multi-limit case is not load-bearing. Per-frame machinery would be a mechanism with no named consumer (#1136 §2).

**This matches the implemented code.** No change is required to work in flight; this section is the documentation catching up to the correct shape.

**Documented limitation:** a foreign `IScript` implementation nested by a host does not inherit depth. §11 records it.

### 7.4 Host re-entry — depth does **not** carry, stated and justified

**Decision: a host method that calls back into `IScript.Execute` starts a fresh execution with a fresh `DepthBudget`.**

Justification: re-entry is the *host's* decision, made in host code, on a script the host chose, possibly from a different parser with different limits. #7712 §10 already places everything inside a host call on the consumer's side of the line, and #7712 §7.8 already established that budget state does not cross a host boundary. Carrying depth across would additionally require a public surface for "inherit this budget", which is exactly the kind of surface #7712 §8.1 spent its trade-off budget avoiding.

**The residual is real and must be stated, not hidden:** the physical stack keeps growing across a host re-entry, so a host that re-enters recursively can still overflow. That belongs in the host contract (§11) alongside the existing "host method that blocks" residual, and it is another instance of the same underlying truth: **in-process guards are a mitigation, not a boundary** (#7712 §10).

### 7.6 ~~⚠ OPEN DEFECT~~ **RESOLVED** — concurrent `task.run` bodies share one depth counter

> **Status, 2026-08-06 (DiVoid #7749, #7782).** **Resolved as specified below**, in three parts: `TaskHost.Run` now calls `LambdaMethod.InvokeOnNewStack`, which mints a fresh `DepthBudget` carrying the parent's `Limit`; `LambdaMethod` implements `IExternalMethod`, so engine dispatch (`ScriptMethod.ExecuteToken`) resolves the budget from the *invoking* context; and the three lambda-taking `EnumerableExtensions` methods take an engine-injected `ScriptContext` and invoke through `InvokeFrom`. T8 passes. **The residual is not the defect below but its mirror image on the public surface — the third entry point, `Invoke(params object[])`, still resolves from the captured context. §7.7 decides that contract; it is a documented property, not an open defect.** *(Originally found while verifying this design against the shipped implementation, and flagged to the operator rather than worked around.)* The analysis below is retained as the record of that finding.

`DepthBudget` holds a single `depth` field mutated with `Interlocked.Increment`/`Decrement`, and is shared **by reference** across every derived `ScriptContext` — including the contexts used by `task.run` bodies, which its own XML remarks state explicitly. `TaskHost.Run` is `Task.Run(() => method.Invoke())`, and `LambdaMethod.Invoke` calls `Enter()` on that shared budget.

**Consequence:** *N* concurrent `task.run` lambdas each nesting **one** level deep raise the shared counter to *N*. With the usable ceiling in the high single digits (§11.1), **nine concurrent `task.run` calls containing no recursion whatsoever trip the guard.** That is a spurious abort on an entirely ordinary concurrent script.

**Why the design did not catch it earlier.** §7.1 correctly established that depth must be shared *along a call chain* and chose a by-reference budget for that reason. It did not distinguish *nesting* (which must accumulate) from *concurrency* (which must not): a `task.run` body is a **new physical stack**, so it is exactly the case where the count must start again. `StepBudget` is legitimately global because total work is a global quantity; **a stack is not** — there is one per thread, and the guard exists to protect a stack.

**Recommended fix (small, for the implementer to confirm):** where a lambda body begins on a new thread — i.e. the `TaskHost.Run` path — derive a **fresh `DepthBudget` carrying the same `Limit` with a zeroed counter**, rather than sharing the parent's. Nesting inside that task then counts from zero against the same ceiling, which is the correct semantics for a fresh stack. Everything else (§7.1's chain sharing, §7.3's import inheritance) is unaffected.

**Rejected alternative: accept the over-count as conservative.** Over-counting is normally the safe direction, but at a ceiling of ~8 it produces routine false positives on non-adversarial scripts, which would push hosts to raise `MaxDepth` — straight into the R9 footgun. The safe direction becomes the dangerous one.

*(Ironically, the `[ThreadStatic]` shape rejected in §7.1 would have got this case right for free. The rejection still stands on the #7713 grounds given there; the correct resolution is a fresh budget per new stack, which keeps the async-safe context-carried shape and fixes the semantics.)*

### 7.7 The three invocation entry points — and why `Invoke(params object[])` keeps the captured budget

**Decision (DiVoid #7782, 2026-08-06): `LambdaMethod.Invoke(params object[])` keeps its current semantics unchanged. No behaviour change, no deprecation, no new public surface. The gap it leaves is closed by documentation and pinned by test, because every alternative trades a visible, catchable false positive for an invisible, uncatchable false negative — and beneath `MaxDepth` there is nothing.**

`LambdaMethod` has three entry points into the same `InvokeCore` body. They all enter and exit a budget; they differ only in **which** budget:

| Entry point | Visibility | Budget resolved from | Reached by |
|---|---|---|---|
| `Invoke(params object[])` | public | the context **captured when the lambda was defined** | host C# code calling a `LambdaMethod` directly |
| `InvokeFrom(ScriptContext, params object[])` | public | the **invoking** context | `IExternalMethod.Invoke` (engine dispatch of `$f.invoke(…)`), `EnumerableExtensions`, any host extension that declares a `ScriptContext` parameter |
| `InvokeOnNewStack(params object[])` | **internal** | a **fresh** budget carrying the same `Limit` | `TaskHost.Run` only |

#### The gap, stated precisely

`EnumerableExtensions` is **not registered by default** — it is a worked example of a host extension, not a privileged one. #7749's sweep converted every *in-repo* caller to `InvokeFrom`; it could not convert callers that do not exist yet. A host writing its own lambda-taking extension reaches for the shorter, undeprecated `Invoke(args)` and gets pre-#7749 accounting: *N* concurrent, **non-recursive** invocations of one shared lambda charge one shared counter, and a `MaxDepth` of *N-1* aborts a script that never recursed. QA #7744 round 3 measured exactly this and rated the failure mode critical twice.

#### Why the budget must not be widened — the asymmetry that decides it

The tempting fix is a fourth option not in #7782: give `Invoke(args)` a **fresh** budget with the same `Limit`, exactly as `InvokeOnNewStack` does, on the reasoning that "no invoking context" means "a new logical stack". **It is wrong, and this repo already contains the counter-example.**

`Scripting.Tests/ExecutionGuardTests.InvokeCallback` is a test-local host extension whose whole body is `callback.Invoke(n)`. Route the recursion *through it at every level* — `$fac = $n=>{ if($n>0) { return($fac.invokecallback($n-1)) } return(0) }` — and trace it:

```
script  →  reflected InvokeCallback  →  lambda.Invoke(n)     [budget A, depth 1]
              body  →  .invokecallback  →  lambda.Invoke(n-1) [budget B, depth 1]
                          body  →  …                          [budget C, depth 1]
```

`Invoke` resolves from the **captured** context, and the captured context is the *definition site* — the same one at every level. A fresh budget per call therefore reads **1 forever** while the physical stack grows without bound. This is §7.1 Error 2 exactly, re-introduced through the public surface: *"a depth counted along the context chain would read 1 forever while the physical stack grows."*

Three properties make that unacceptable rather than merely looser:

1. **There is no backstop.** The `EnsureSufficientExecutionStack` probe was built, measured, and dropped: recursion through reflection went from completing normally to hard process death with **no** window in which the probe fired. `MaxDepth` is not a ceiling with a margin behind it — it is the only mechanism watching. A false negative here is an uncatchable `StackOverflowException`, not a late abort.
2. **The unguarded shape would be the *most expensive* one.** §11.1 measured a reflected host-callback chain safely aborting only to depth **16**, against **21** for direct `$f.invoke`. The fresh-budget option removes the guard from precisely the dispatch shape that consumes the most stack per level.
3. **The two errors are not comparable in kind.** A false positive is a `ScriptDepthLimitExceededException` — catchable, carrying its `Limit`, diagnosable, and avoidable by the host at zero cost. A false negative is process death with no diagnostic and no recovery. **Over-counting is the direction that fails safe. §7.6 rejected "accept the over-count" for the `task.run` case because a *fresh physical stack* genuinely resets the resource being measured; `Invoke` has no such justification — nothing about it implies a new stack.**

#### Why the false positive is a documentation defect, not a contract defect

**The engine already hands every host extension the correct answer for free.** `MethodOperations.CreateParameters` injects the invoking `ScriptContext` into any extension-method parameter of that type, consuming a target slot but **no script argument** — the parameter is invisible at the script call site (this is the B20 mechanism, already verified by QA against the pre-existing `.where(`/`.indexof(` tests). So a host extension that wants correct depth accounting adds one parameter and calls `InvokeFrom`. There is **no host-extension case in which the invoking context is unavailable.** The remaining legitimate audience for `Invoke(args)` — host C# driving a lambda returned from `Execute`, a unit test constructing one by hand — genuinely has no invoking context, and for them the captured budget is the only budget there is.

#### The three options in #7782, and why each is rejected

- **(b) `[Obsolete]`-warn `Invoke`.** Rejected: it warns on **correct** code. `Invoke(args)` is the right call whenever no invoking context exists, and there is no non-obsolete alternative for that audience (`InvokeOnNewStack` is internal). A deprecation that has no correct replacement for a legitimate call teaches callers to suppress the warning, which is worse than silence.
- **(c) Throw, or fall back, when a budget is configured and no invoking context is available.** Rejected: it converts a *conditional, rare* false positive into an *unconditional* hard failure for every host that calls `Invoke` with `MaxDepth` set — including this repo's own `InvokeCallback`-shaped tests. It also cannot distinguish "host forgot to take the context" from "there genuinely is no context", because both arrive as the same call.
- **(d) Fresh budget** (not in #7782; considered here). Rejected on the false-negative argument above. **This is the option most likely to be proposed again later, which is why T9c exists to fail if it lands.**

#### What ships instead

1. XML remarks on `Invoke` naming the concrete failure (concurrency counted as nesting) and the concrete remedy (declare a `ScriptContext` parameter; call `InvokeFrom`) — not a bare "prefer `InvokeFrom`".
2. The §12 correction in `docs/pooscript-language-reference.md`, scoping its claim to engine-dispatched invocation (§11.6 below carries the wording).
3. T9a/T9b/T9c (§13), written against a **test-local** host extension so that converting an in-repo caller can never again make them pass without addressing the method.

**Residual, recorded not hidden:** a host that drives a returned `LambdaMethod` on **its own threads** has neither an invoking context nor access to `InvokeOnNewStack`, and reproduces the shared-counter shape with no engine hook that would notice. §11.6 states it as a host-contract limitation and OQ-9 asks whether it needs a public entry point. **It is not fixed here, because no named consumer needs it today (#1136 §2) and the mitigation — size `MaxDepth` above your own concurrency — is available now.**

### 7.5 Rejected: consolidating the three budgets into one object

The additive shape leaves `ScriptContext` with three nullable by-reference fields and `Guard()` with two null-conditional calls instead of one. A single `ExecutionBudget` holding all three counters would reduce that to one field and one null check.

**Rejected.** The gain is one predicted-not-taken branch per checkpoint. #7712 §7.6 already made the governing cost argument in the other direction — one interpreter step is a virtual dispatch plus dictionary lookups plus boxing, three or more orders of magnitude above a branch. Against that, consolidation would refactor merged, QA-approved code (`StepBudget`, `ScriptContext`, `GuardedExecution`) and enlarge the review surface for a below-noise gain. The additive shape also keeps each budget single-responsibility, which matters because the three have genuinely different lifetimes: `StepBudget` is monotonic, `DepthBudget` is enter/exit, `VariableBudget` is sampled. Merging three different lifetimes into one object is the parallel-layer smell (#1136 §2 form 2), not a simplification.

**And note where the additive shape actually lands:** `DepthBudget` is not consulted in `Guard()` at all. The hot checkpoint gains exactly **one** null-conditional call, for the variable budget.

---

## 8. The Variable-Usage Guard — decisions

This is the half that needed the design pass. Each of the four genuine open questions from the brief is settled below with its reasoning and its blind spots.

### 8.1 Two knobs, one measurement pass

**Decision: `MaxVariables` (`long?`, entry count) and `MaxVariableBytes` (`long?`, approximated footprint) are two thresholds on a single measurement, not two mechanisms.**

They are independent (either, both, or neither may be set) but share one walk, one cadence, one exception type. When only `MaxVariables` is set the walk counts entries and **never touches a value** — genuinely the cheap tier the user asked for. When `MaxVariableBytes` is set the same walk additionally sizes each value.

This is the DRY answer: two separate mechanisms would duplicate the chain walk, the cadence logic, and the concurrency handling. It is also the KISS answer to "light vs. hard" — they are two thresholds on one number-pair, not two subsystems.

### 8.2 Where the check happens — **three mechanisms, two of them at the sites a reader expects**

This section answers the question "where does the check happen?" directly, because the natural expectation is *"in the set-variable token"* and an earlier draft of this design did not do that. **Position changed — see the reconciliation below.**

#### 8.2.1 The three mechanisms and what each one catches

| # | Mechanism | Site(s) | Catches | Cost |
|---|---|---|---|---|
| **M1** | **Value ceiling** — no single value may exceed `MaxVariableBytes` | `Operations/Values/ValueOperation.cs` `ExecuteToken` (the result of **any** arithmetic/bitwise operator — one shared base class covers all twelve) and `Operations/AssignableToken.cs` `Assign` (any value entering **any** assignable slot — one shared base covers variables, members, indexers, and every compound-assign form) | `$s = $s + $s`; long operator chains; a host method returning a huge value into a variable | `O(1)` — two type tests, both failing for the common numeric case |
| **M2** | **Growth trigger** — a monotonic *bytes-produced-since-last-pass* counter; crossing the budget **forces** a measurement pass | the same two sites as M1, in the same helper call | Aggregate accumulation of many separately-produced values, before the sampled interval would have noticed | `O(1)` — one add, alongside M1 |
| **M3** | **Sampled walk** — the ground truth, at `Guard()`, at a size-proportional interval | `ScriptContext.Guard()` (one line, one site) | **In-place mutation that never passes M1 or M2** — `$l.add(…)` in a loop; entry counts; and it is what resets M2 | amortised ≤ 1 unit/checkpoint |

**Two sites, not twelve.** Both M1 and M2 hook shared base classes — `ValueOperation.ExecuteToken` is the single `ExecuteToken` for all of `Addition`, `Multiplication`, `Subtraction`, `Division`, `Modulo`, the four bitwise ops and the four shift/rotate ops; `AssignableToken.Assign` is the single entry for every assignment form. One helper (`VariableBudget.ChargeProducedValue`) does the ceiling check and the trigger increment together, called at exactly two places.

#### 8.2.2 Reconciliation — assignment-site vs. sampled. **I changed position.**

An earlier draft **rejected** assignment-site measurement outright, on the grounds that it "completely misses in-place mutation" — `$l = new list(); while(true) { $l.add("xxxx") }` never performs a second assignment while the value grows. That reasoning is still correct **as far as it goes**, and it is why M3 exists and cannot be removed.

But it was used to justify the wrong conclusion, and it concealed a real defect in the sampling-only design:

> **The sampled interval is proportional to *walk cost*, not to *how fast the footprint can grow*.** A script holding one huge string has a walk cost of ~1 unit — a string is one unit; the sizer reads `Length` and does not walk characters. With the previously specified `MinMeasureInterval` floor of 256, `$s = $s + $s` would run **256 doublings between checks**. Sampling alone is not merely laggy for the exponential shape, it is **useless** for it — and that is the shape #7712 §10 item 4 names by name.

No step-count-based interval can fix this, because growth is exponential in *statements* while the interval is denominated in *walk units*. The fix is not a better cadence; it is a check at the point where a large value is **produced**.

**So the answer is both, and they are not redundant — they catch disjoint shapes:**

- **M1/M2 catch values the script *creates*.** They are `O(1)` and immediate, and they are what makes the overshoot bound in §8.2.4 a *guarantee* rather than a hope.
- **M3 catches growth the script *causes without creating a value* —** appending existing values to a collection, where nothing new is allocated at the operator or assignment level. Nothing at an assignment site can see this.

The natural expectation ("check in the set-variable token") was right about *where*, and the earlier draft was right about *why that is not sufficient*. The design needs both, and the third mechanism (M2) falls out of the first at zero extra cost.

**One refinement on the assignment site.** The hook belongs on `AssignableToken.Assign` — the **base class** — not on `ScriptVariable.AssignToken`. That one placement covers variables, member assignment, indexer assignment and every compound-assign form (`$s += …`, which does *not* route through `ValueOperation`), at one site instead of several.

#### 8.2.3 The sampled cadence (M3) — narrowed job, unchanged math

M3's interval is `max(MinMeasureInterval, unitsVisitedByTheLastMeasurement)`, `MinMeasureInterval` a named `const` of 256.

**Why not a fixed interval.** With a fixed interval `N`, a script growing a collection linearly is measured `n/N` times at average cost `n/2`, giving total work `≈ n²/(2N)`. For `n = 10⁶` and `N = 1000` that is `5 × 10⁸` element visits — ruinous, and incurred by exactly the script the guard exists to catch. Concrete failure, not hypothetical, so the fix is not speculative complexity (#1136 §1).

**The size-proportional interval.** Setting the next interval to the units the last pass visited makes measurement **amortised ≤ 1 unit of work per checkpoint** for any growth curve: if the last pass visited `u` units, the next runs `u` checkpoints later. The floor of 256 caps cost for small graphs at ~1/256 of checkpoints.

**The floor is now safe** precisely because M1/M2 exist. M3 no longer has to catch exponential growth — M1 bounds any single value and M2 forces a pass on cumulative production. M3's remaining job is *linear* accumulation via in-place mutation, and for that a doubling-tolerant interval is well matched: `u` iterations adding `u` elements at most doubles the element count.

**Hot-path cost when unconfigured:** `VariableBudget` is `null`, so `Guard()` performs one null-conditional call that does not dispatch, and M1/M2's two sites do the same. This matches the `StepBudget?` precedent exactly.

**`MinMeasureInterval` stays a `const` (#1136 §3).** No named operator will tune it, it does not differ across environments, and promoting it would be the textbook "configurable for future tuning" anti-pattern.

#### 8.2.4 The quantified overshoot — what "budget with headroom" actually guarantees

The budget-with-headroom principle (§1) is only defensible with a number. With `C = MaxVariableBytes`:

| Shape | Worst-case **observed** at next check | Worst-case **transient** during the operation |
|---|---|---|
| A single produced value (`$s = $s + $s`) | **≤ 1× C** — M1 refuses any value above the budget | **≤ 3× C** — operand (≤C) plus a result up to 2C is allocated *before* M1 can inspect it |
| **A long operator chain in one statement** (`$s + $s + $s + …`, *n* terms) | **≤ 1× C** | **≤ 3× C** — see below |
| Aggregate growth by separately produced values | **≤ ~2× C** — M2 forces a pass once ~C bytes have been produced | ≤ ~2× C |
| Aggregate growth by re-appending *existing* values | trips **early** — the sizer charges aliases per name (§8.6), so the measured figure exceeds the real footprint | safe direction |
| A host method returning a large value **into a variable** | ≤ ~2× C — M1/M2 at the assignment site | ≤ ~3× C |
| A host method's large object appended to a collection with no assignment | **not bounded** — charged an opaque constant | — |
| A single host call allocating internally (`"".padright(2000000000)`) | **not bounded at all** | — |

**The long-operator-chain verdict — the case that most plausibly broke the 2× assumption, and does not.** Left-associative evaluation of `$s + $s + … + $s` builds intermediates of `2\|s\|, 3\|s\|, … , n\|s\|`; without M1 the observed overshoot would be **n×** and total allocation `≈ n²\|s\|/2`, with `n` bounded only by the length of the script source — so an adversary with a 100 KB script could reach a ~10,000× overshoot. **M1 collapses this to ≤3× transient regardless of `n`,** because the check runs on the result of *every* `ValueOperation`, so the chain trips at the first intermediate that exceeds the budget — the second term, not the n-th. **Chain length stops being a variable in the bound.** This is the single strongest argument for M1 existing.

**On the counter that was floated.** Carrying a counter on the context to react at the individual steps of a chain is the right instinct and M2 is that counter — but deliberately as a **growth *trigger*, not as accounting.** Full delta accounting (charge the new value, un-charge the old) was considered and rejected: un-charging requires the *current* size of the old value, which for a collection that was mutated in place since it was charged is stale, so the accounted figure **drifts** — and drift in a safety mechanism is worse than lag. M2 avoids this entirely by being monotonic and reset at every M3 pass; it is never read as a live footprint, only as "enough has been produced, go re-measure". It cannot drift because it is never trusted.

**Deliberate over-triggering, stated.** `$s = $s + $s` charges the same result object twice — once at `ValueOperation`, once at `AssignableToken`. For M2 this is a harmless over-estimate that costs at most one extra measurement pass. For M1 it is idempotent: same object, same size, same verdict. **Over-triggering can never cause a false abort**, only an earlier ground-truth measurement.

**What is still not bounded, and must be said in the same breath:** the last two rows of the table. A single host call can allocate arbitrarily before returning, and no engine mechanism observes it. That is #7712 §10's standing residual, unchanged, and §11 repeats it.

### 8.3 The light tier's threat model is narrower than it looks — stated, not hidden

`MaxVariables` caps the number of live variable entries. Per **A5**, pooscript variable names are lexical: there is no runtime-computed variable name, so the number of *distinct names per scope* is bounded by the script source. Entry count therefore grows only through **scope multiplication** — one live scope per recursion frame, each binding its lambda parameters — and recursion depth is precisely what `MaxDepth` already bounds.

Concretely: `live entries ≈ distinct_names_in_source × live_scopes`, with `live_scopes ≤ MaxDepth × (blocks nested per frame)`. Once `MaxDepth` is configured, `MaxVariables` is a second bound on a quantity that is already bounded.

**Resolved (OQ-1): keep it, documented honestly.** It costs ~5 lines on top of a walk that must exist for the hard tier, it needs no sizing pass, and it is a genuine cheap rail against closure/scope accumulation — in particular for a host that configures it *without* `MaxDepth`. But it is **not** the memory guard, and neither the XML docs nor the language reference may imply that it is. A host that wants the memory guarantee sets `MaxVariableBytes`; `MaxVariables` is a sanity rail beside it.

**One trap the implementer must not fall into.** An *incremental* entry counter (increment when `ScriptVariable.AssignToken` finds `GetProvider(Name) == null` and auto-declares) is the natural cheap implementation and it is **wrong**: scopes die. `while(true) { $x = 1 }` re-declares `$x` in a fresh block scope every iteration, so a monotonic counter grows without bound while live memory is constant — a false positive on a perfectly bounded script. Entry count **must** come from the walk over live providers, never from a cumulative declaration counter.

### 8.4 Scope boundary — where the walk stops

The user drew the line twice, and the second phrasing is the one this design adopts as its governing sentence:

> "we talk about **usage** - a 500MB host object already exists in memory and is not introduced by the script - so that much is okay, **script should handle script, not the whole engine**."

The measured quantity is therefore **what the script introduced**, not what the script can reach. A host object the script merely holds a reference to was allocated by the host, is owned by the host, and would exist whether or not the script ran. Charging it to the script would make the guard fire on scripts that allocated nothing — the opposite of useful.

**Decision: the walk covers engine-created `VariableProvider` instances on the current scope chain and stops at the first provider that is not one.**

- Every derived scope is a `Pooshit.Scripting.Parser.VariableProvider` (A6), so **all script-driven variable growth is inside the walked region** — including top-level assignments, because the script body is a `StatementBlock` that derives a scope before the first statement runs.
- The **root** provider is host-supplied (`Execute(variables, …)`). It is not walked. That is precisely the user's line: what the host injected is the host's problem.
- A host that supplies a custom `IVariableProvider` implementation gets the same treatment — walk stops there. This has the pleasant side effect that **no change to the public `IVariableProvider` interface is required**, avoiding another compile-breaking interface change on top of #7409's B2/B3/B4/B5.

**Mechanism.** `VariableProvider` gains two **internal** members: access to its local values and to its parent. Internal, so no public-surface change and no `[InternalsVisibleTo]` concern beyond the test assembly. `Values` is already `protected`, so a host subclass of `VariableProvider` is walked too — correct, since its entries are still engine-managed slots.

### 8.5 The sizing heuristic — and its blind spots, stated plainly

**Decision: a closed switch over a fixed, small set of shapes, with a recursion depth cap of 4. No reflection. No field walking. No object-graph traversal.**

| Value shape | Charge | Recurses? |
|---|---|---|
| `null` | 0 | — |
| `string` | header + `2 × Length` | — |
| primitive-element array (`byte[]`, `char[]`, `int[]`, …) | header + `Length × sizeof(element)` | — |
| reference-element `Array` | header + `Length × 8`, then elements | Yes, while under the depth cap |
| `IDictionary` | header + `Count × 24`, then keys and values | Yes, while under the depth cap |
| `ICollection` (covers `List<object>`) | header + `Count × 8`, then elements | Yes, while under the depth cap |
| `IEnumerable` that is **not** `ICollection` | opaque constant, **never enumerated** | No |
| any value type / boxed primitive | fixed constant | — |
| anything else — host objects, `LambdaMethod`, `Task`, delegates | opaque constant, **not walked** | No |

**Why non-`ICollection` `IEnumerable` is never enumerated:** enumerating a host sequence can be infinite, side-effecting, or blocking. The guard must never do that. It is the same reasoning #7712 §7.5 applied when it wrapped host enumeration in a per-element checkpoint rather than materialising it.

**The depth cap does double duty as the cycle guard.** A `list` containing itself would otherwise recurse forever. The cap of 4 bounds it; no visited-set, no reference tracking, no allocation. One mechanism, two jobs.

**Blind spots — say these out loud in the doc and in the language reference. Do not imply precision the heuristic does not have:**

| Blind spot | Direction | Consequence |
|---|---|---|
| Host object graphs are charged a flat opaque constant | **Under**-counts, unboundedly | A script holding one host object that owns 500 MB is charged ~64 bytes. **This is by design and is confirmed correct** — *"a 500MB host object already exists in memory and is not introduced by the script"* (§8.4). It is usage, not reachability. |
| A `LambdaMethod` closure captures a whole context chain, charged as opaque | **Under**-counts | A list of closures under-reports. Partially mitigated by `MaxDepth` bounding live frames. |
| Nesting deeper than the depth cap | **Under**-counts | A list-of-list-of-list-of-list of large strings stops being sized at level 4. |
| Two variables aliasing one object | **Over**-counts | Charged once per name. Over-counting is the safe direction for a guard. |
| .NET object headers, padding, string interning, GC generations | Both | The number is an approximation of *retained payload*, never of process RSS. |
| A single statement between checkpoints | **Lags** | §8.2.4 overshoot — bounded to ~3× transient by M1, not unbounded. |

**The constants stay `const` (#1136 §3).** Header sizes and the depth cap have no named tuner and do not vary by environment. Named `const`s in `VariableSizer`, not knobs. Adding a knob per constant would be the textbook "configurable for future tuning" anti-pattern (#1136 §6).

### 8.6 Nested and shared values — counted once per walk

The brief asked whether a value visible from several scopes is counted once or per-scope.

**Decision: once.** Each variable *entry* lives in exactly one provider's dictionary — the one that declared it. Parent scopes are visible through the chain but do not hold a second copy (`VariableProvider.GetProvider` walks up to find the owner; `ScriptVariable.AssignToken` writes to the owner it found). The walk visits each provider on the chain exactly once, so each entry is charged exactly once.

The remaining case is **aliasing** — two distinct names bound to the same object (`$a = $b` where the value is a `list`). Those are two entries and are charged twice. That is an over-count in the safe direction and is cheaper than any deduplication scheme, which would need a reference-identity set allocated per measurement pass on the hot path. Recorded as a blind spot in §8.5 rather than engineered around.

**What the walk does *not* see:** scopes that are already dead (correct — they are garbage), sibling scopes on other branches of the tree (correct — not live), and the scope chains captured by lambdas stored in a value (a genuine under-count, §8.5).

### 8.7 Concurrency — measurement is best-effort

`task.run` lambdas share the parent context, and therefore the `VariableBudget` instance, across threads (#7712 §7.6). Two consequences:

1. **The tick counter must be an interlocked increment**, exactly as `StepBudget.Consume` does, for the same reason: a racing plain increment makes the cadence non-deterministic.
2. **A measurement pass can race a concurrent write.** Enumerating a `Dictionary` while another thread writes to it throws `InvalidOperationException`. This hazard is *new* — nothing enumerates these dictionaries today.

**Decision: the measurement pass is best-effort. If the walk faults on a concurrent modification, the pass is abandoned and the interval re-armed; it is not retried inline and it does not surface as a script error.** No locking is introduced — locking the variable dictionaries would put a lock on the hottest path in the interpreter to protect a heuristic. The next pass will observe the growth. A script cannot exploit this to evade the guard indefinitely: the racing writer is itself ticking the same checkpoint counter, so passes keep coming.

**Per-chain measurement, stated.** Each thread measures the chain it is guarding, so concurrent branches each charge the shared ancestor scopes. Against a *shared* threshold this over-counts the ancestors and under-counts the sum of the branches. It is a heuristic guard on an approximate number; the alternative — a global registry of live scopes — is a whole subsystem for a marginal accuracy gain and is rejected on KISS grounds.

---

## 9. The Exception Contract — and rule #7718

### 9.1 Two new exception types under one new abstract base

**Decision:**

```
Exception
└── ScriptException                      (abstract, existing)
    ├── ScriptRuntimeException           (existing — remains a SIBLING of the abort family)
    └── ScriptAbortException             (abstract, NEW)
        ├── ScriptStepLimitExceededException      (existing — re-parented)
        ├── ScriptTimeoutException                (existing — re-parented)
        ├── ScriptDepthLimitExceededException     (NEW)  → Limit
        └── ScriptVariableLimitExceededException  (NEW)  → Limit, Measured, Kind
```

`ScriptVariableLimitExceededException` carries a small `VariableLimitKind { Entries, Bytes }` enum so a host can log *which* threshold tripped without parsing the message. Two concrete values, both needed today — not speculative (#1136 §1). The alternative of two separate exception types was rejected as type proliferation for one shared abort reason.

Neither new exception carries a source position, for the same reason `ScriptStepLimitExceededException` does not (#7712 §8.4): the throwing site is a checkpoint, not a semantic error.

### 9.2 Why a new base — and the correction to the brief's framing

The brief stated that a type deriving from `ScriptException` "inherits the existing passthroughs for free". **That is true at three of the five wrapper families and false at the other two.** Verified against the merged tree:

| Dispatch wrapper | Shape in merged code | New `ScriptException` subtype without a new base |
|---|---|---|
| `Tokens/ScriptToken.cs:18-26` | `catch(OCE)` → `catch(ScriptException){throw}` → `catch(Exception)` | **Free** ✔ |
| `Control/StatementBlock.cs:36-47` and `:56-67` | same | **Free** ✔ |
| `Operations/AssignableToken.cs:19-30` | same | **Free** ✔ |
| `Control/Throw.cs:46-58` and `:59-70` | same | **Free** ✔ |
| `Tokens/ScriptMethod.cs:86-101` and `:117-133` | `catch(OCE)` → `catch(StepLimit)` → `catch(Timeout)` → `catch(ScriptRuntimeException)` → `catch(Exception)` | **NOT free** ✘ — falls into `catch(Exception)` and is **wrapped into a `ScriptRuntimeException`** |
| `Operations/MethodOperations.cs:292` | `catch(TargetInvocationException e) when (e.InnerException is OCE or StepLimit or Timeout)` | **NOT free** ✘ — falls to the generic `TargetInvocationException` arm and is **wrapped** |

Those two families use **narrowly-typed enumeration**, deliberately, because #7718 requires narrow passthrough (a blanket `catch(ScriptException){throw;}` there would stop `ScriptRuntimeException` from an imported script being re-wrapped with outer call-site context, which `DebugTests.ExternalMethodFail` depends on). A new type must be named at all three of those chains.

There is a **seventh** site the brief did not list and #7718's table does not classify as a dispatch wrapper, because it is not one — but it is load-bearing here:

| Site | Shape | Consequence |
|---|---|---|
| `Control/Try.cs:29-44` — the **script-visible** `try`/`catch` | `catch(OCE) when (context token cancelled)` → `catch(ScriptStepLimitExceededException){throw}` → `catch(Exception)` → hands to the script's `catch` block | A new type not named here is **caught by the script's own `catch` block** — `try { deep_recursion() } catch { }` would silently defeat the guard entirely. |

`ScriptTimeoutException` is absent from `Try` because it is only *converted* at the `Script.Execute` boundary, outside every `Try`. The two new types are raised **inside** the engine and will reach `Try`. This is the single most important correctness point in this section.

**The DRY math (#1267).** The abort-family enumeration is repeated at four sites (`Try`, two `ScriptMethod` chains, one `MethodOperations` filter). Each `catch(X){throw;}` clause is 3 lines.

| | Enumerated clauses | Duplicated lines |
|---|---|---|
| Today (3 abort types, `Try` naming only one) | 1 + 2 + 2 + filter(3 terms) | 15 + a 3-term filter |
| Adding two types **without** a base | 3 + 4 + 4 + filter(5 terms) | **33 + a 5-term filter** |
| Adding two types **with** `ScriptAbortException` | 1 + 1 + 1 + filter(2 terms) | **9 + a 2-term filter** |

`block_size × site_count = 3 × 4 types × 3 catch-chain sites = 36` lines of pure type enumeration — **well above the ~15–20 threshold**. The extraction costs one ~14-line abstract type and saves 24 lines of duplication now, plus 9 lines and a filter term for every future guard. The named-helper test passes: `ScriptAbortException` is a one-word name.

**And it makes rule #7718 mechanically checkable.** After this change, the rule reduces to a fixed two-clause shape that never grows again:

> every catch-all dispatch wrapper carries `catch(OperationCanceledException){throw;}` followed by `catch(ScriptAbortException){throw;}`, before the catch-all.

#7718 explicitly exists because this list keeps getting missed — three times in one task. A rule whose passthrough list is a fixed pair is a rule the next contributor can satisfy without an inventory. **#7718 must be updated in the same change** to state the new two-clause shape and to add `Try.cs` to its table with its distinct classification ("script-visible catch, not a dispatch wrapper, but must rethrow aborts").

**Backward-compatibility of re-parenting.** Inserting `ScriptAbortException` between `ScriptException` and the two existing types changes no matching behaviour: `catch(ScriptException)` still matches, `catch(ScriptStepLimitExceededException)` still matches, and `ScriptRuntimeException` remains a **sibling** of the abort family — the exact property QA #7719 verified and that `Import.cs:34` / `NewInstance.cs:65` rely on. §12 B11 records it.

### 9.3 The contract a host sees

Extending #7712 §9.1 (rows a–d unchanged):

| # | Scenario | `await ExecuteAsync(ct)` throws | Task state | Sync `Execute` throws |
|---|---|---|---|---|
| a | Caller cancels `ct` | `OperationCanceledException` / `TaskCanceledException` | **Canceled** | same |
| b | `Limits.Timeout` elapsed | `ScriptTimeoutException` | **Faulted** | same |
| c | `Limits.MaxSteps` exceeded | `ScriptStepLimitExceededException` | **Faulted** | same |
| d | `Limits.RegexTimeout` elapsed | `ScriptRuntimeException` wrapping `RegexMatchTimeoutException` | **Faulted** | same |
| **e** | **`Limits.MaxDepth` exceeded** | **`ScriptDepthLimitExceededException`** (message names the limit) | **Faulted** | same |
| **f** | **`Limits.MaxVariables` / `MaxVariableBytes` exceeded** | **`ScriptVariableLimitExceededException`** (`Kind`, `Limit`, `Measured`) | **Faulted** | same |

Rows e and f join b and c on the **Faulted** side, for the reason #7712 §9.1 gave: they are *the script misbehaved*, not *the host asked to stop*. Uberkarl must be able to attribute "this community script recursed away" to a specific script and distinguish it from "we shut the level down". All six are distinguishable by type.

Neither new type is ever produced when its knob is null — the budget object is not allocated (§12).

---

## 10. Configuration Surface

**Decision: the three new knobs sit on the existing `ScriptLimits`, `init`-only, defaulting to `null`.** No new options object, no new host-facing type, no new entry point.

| Knob | Type | Default | Effect when null |
|---|---|---|---|
| `MaxDepth` | `int?` | `null` | No depth ceiling; `DepthBudget` is not allocated (today's behaviour) |
| `MaxVariables` | `long?` | `null` | Entry count is never measured |
| `MaxVariableBytes` | `long?` | `null` | Footprint is never measured; when both variable knobs are null, `VariableBudget` is not allocated |

`int?` for `MaxDepth` (depths are small; `long` would be noise); `long?` for the two variable knobs, matching `MaxSteps`.

**Configurability gate (#1136 §3).** Each knob passes on "the value genuinely differs across hosts by design", the same test the existing three passed: trusted embedding (Mamgo `scriptservice`, `ScriptExecutor` CLI) leaves all six null; untrusted embedding (Uberkarl, #7407) sets them. Two named hosts, opposite settings, today. No telemetry-then-tune compound — no audit column, no per-execution record, nothing measuring the values.

**No defaults, so no magic numbers (#1136 §3).** The engine ships no opinion about "too deep" or "too much". §11 gives the host sizing guidance in documentation instead.

**`ScriptParser.Limits` is untouched as a surface** — it already exists and already reaches every entry point (sync, async, typed, untyped). `IScriptParser` and `IScript` are **not** modified by this design. This is the §7712 §8.1 precedent applied unchanged, and it is why this design adds zero public-interface breaks.

---

## 11. Host Contract — what the engine still does not guarantee

This section is a deliverable. It extends #7712 §10 and belongs in `docs/pooscript-language-reference.md` §12 and in #2946.

The engine now bounds recursion it drives, and sustained growth of the variable dictionaries it owns. It does **not** bound:

1. **Everything #7712 §10 items 1–3 and 6 already listed** — a blocking host method, a blocking host sequence, catastrophic regex, a detached `task.run`. Unchanged.
2. **Stack consumed by host code**, including a host method that re-enters `IScript.Execute` recursively (§7.4) and a host-supplied `IScript` implementation nested through `import` (§7.3). Depth does not cross either boundary. The stack does.
3. **Memory reachable through host-injected surfaces.** A variable holding one host object that owns 500 MB is charged an opaque constant. **This is the boundary the user drew and it is deliberate.**
4. **A single allocation between two checkpoints.** `$s = $s + $s` doubles within one statement; the guard observes it at the next checkpoint, after the allocation has happened. The guard bounds *sustained* growth, not peak.
5. **Process RSS.** The measurement approximates retained payload of script-owned variables. It is not a heap measurement and must never be documented as one.
6. **Depth accounting for a lambda a host invokes through `LambdaMethod.Invoke(params object[])`.** That overload resolves the budget from the context the lambda was *defined* in, so concurrent invocations of one shared lambda are charged to one shared counter. A host extension avoids this by declaring a `ScriptContext` parameter and calling `InvokeFrom`; a host driving a returned lambda on its own threads cannot, and must size `MaxDepth` above its own concurrency. §7.7 decides the contract; §11.6 states the host-facing rule.

> **For genuinely untrusted code, items 2–5 mean the in-process guards are a mitigation, not a boundary. Process or container isolation remains the only complete answer** — the same conclusion #7712 §10 reached, now with two fewer holes on the fast path. Uberkarl should treat the six guards as the fast path and isolation as the containment story. *(No isolation work is designed here; it is out of scope per §2.)*

### 11.1 Sizing `MaxDepth` — the usable ceiling is **single digits**, and the reason matters

> ⚠ **An earlier draft of this section said "a value in the low hundreds is a reasonable starting point". That was wrong by more than an order of magnitude, and wrong in the dangerous direction.** It has been replaced with the measured result. Anyone tempted to raise the number must read §11.2 first.

**Measured on the real interpreter** during implementation of the depth guard:

| `MaxDepth` | Result |
|---|---|
| **20** | **Genuine, uncatchable `StackOverflowException` — process death** |
| **10** | Empirically safe |
| **8** | The value the shipped test suite uses throughout (`SafeMaxDepth`) |

**Why it is this low.** Every lambda call in this interpreter — **including a direct one** — is dispatched through reflection: `ScriptMethod.ExecuteToken` → `MethodOperations.CallMethod` → `MethodInfo.Invoke` → the runtime's invoke stubs → `LambdaMethod.Invoke` → `ScriptToken.Execute` → `StatementBlock.Execute` → `ExecuteBlock` → the next statement. One *script-level* frame is therefore worth far more than ten physical frames, and the reflection stubs are the expensive part.

### 11.2 The guard can kill the process **while enforcing itself** — the property, not a footnote

The `MaxDepth = 20` overflow **did not come from the recursive descent.** It came from **unwinding**.

`MethodOperations.cs:292` is an **exception filter** (`catch (TargetInvocationException e) when (…)`). .NET uses two-pass exception handling: **pass one runs every filter on the way out while the throwing frame is still on the stack.** So an abort thrown at depth *n* causes *n* stacked filters to be evaluated **on top of the already-deepest stack**, plus *n* `TargetInvocationException` constructions and rethrows. The unwind is more stack-hungry than the descent that produced it.

The consequence is a genuine property of the feature and must be stated as one:

> **Above the safe range, `MaxDepth` is worse than no guard at all.** A host that sets a plausible-sounding value — 50, 100, anything in the "low hundreds" an earlier draft suggested — converts a *survivable* deep recursion into a *process kill at the exact moment the engine tries to save the host*. The abort's own unwind is what overflows.

This is not a tuning preference. It is a hard property of the reflected-invoke dispatch path, and it will remain one until that path changes (§11.5).

### 11.3 Calibration — the procedure, because the safe value is host-specific

The safe ceiling depends on the host's thread stack size, the TFM, the JIT and the architecture, so the engine cannot ship a correct number. It can ship a **procedure**, which is what the implementer effectively ran by bisection:

1. On the host's own build, TFM and thread configuration, run a minimal recursive-lambda script with `MaxDepth` unset, under a `try`/`catch` that will *not* save you — the point is to find where the process dies.
2. **Bisect for the largest `MaxDepth` at which the abort is raised and unwinds cleanly**, i.e. the host receives `ScriptDepthLimitExceededException` rather than losing the process. Call it `D_overflow`.
3. **Configure `MaxDepth ≈ D_overflow / 2.** The halving is the headroom: the calibration script is minimal, and a real script's expression trees add frames per script-level frame that the calibration did not exercise.
4. Re-run the calibration whenever the thread stack size, the TFM or the runtime version changes. **Treat it as a build-time check, not a one-off** — this is a property of the toolchain, not of the script.
5. If a legitimate script needs more depth than calibration allows, **raise the thread's stack size** (run the interpreter on a `Thread` constructed with an explicit larger `maxStackSize`) and re-calibrate. **Do not raise `MaxDepth` without re-calibrating** — that is precisely the footgun in §11.2.

This belongs in the language reference alongside the knob, as a procedure with the halving rule stated, not as a number.

### 11.4 Why the knob stays a free-form `int?` and the engine does not clamp it

Considered: an engine-enforced hard maximum, so a host cannot configure its own process kill.

**Rejected, and the reason is that the engine does not have the information.** The safe ceiling is a function of the host's thread stack size — a host that runs the interpreter on a 16 MB-stack thread has a legitimately much higher ceiling than one on the 1 MB default. A hard-coded engine constant would be simultaneously *too low* for the first host (breaking legitimate scripts for no reason) and, on some future runtime or architecture, potentially *too high* for the second — a clamp that is wrong in both directions is worse than an honest warning, because it looks like a guarantee.

**What ships instead of a clamp:** the measured numbers (§11.1), the self-defeat property stated as a property (§11.2), and a calibration procedure with a halving rule (§11.3) — carried into the XML docs on `MaxDepth`, into `docs/pooscript-language-reference.md` §12, and into #2946. A host that reads the knob's documentation cannot miss it.

*(Recorded explicitly per the design-review question: yes, the free-form knob lets a host misconfigure itself. The mitigation is documentation plus a runnable procedure, because a clamp would be a false guarantee.)*

### 11.5 Consequences for Uberkarl — stated, not buried

- **Recursive lambdas inside a single script are the load-bearing case, not imports.** Toni has confirmed imports are *not expected to be enabled* (§1). The import-boundary depth inheritance (§7.3) is still correct and still cheap, but it is not what protects Uberkarl — the lambda call site is.
- **A ceiling in the high single digits bounds legitimate recursive traversal hard.** Any community script doing recursive tree or graph walking will hit it. Toni's position is that recursion is not expected in community scripts but **must be assumed possible because the scripts are user-authored** — so a low ceiling is acceptable. It is nonetheless a real consequence: **in practice, configuring `MaxDepth` for an adversarial threat model means recursion is effectively unavailable to script authors.** That is a product decision the host is making, and it should be made knowingly rather than discovered.
- **The threat model is adversarial, not accidental.** Guidance is framed for an author *actively trying to kill the host*, not one who wrote an accidental infinite recursion. An adversary will find the deepest legal shape; the ceiling must hold against that, not against the average case.
- **A host extension that takes a `LambdaMethod` must take a `ScriptContext` too.** This is the one host-side rule the depth guard imposes, and it is invisible unless stated (§7.7). Uberkarl registers its own extensions; every one of them that accepts and invokes a script lambda needs the parameter, or its callbacks are charged to the defining script's counter.
- **Follow-up worth filing (out of scope here, do not design it):** the root cause of the low ceiling is that even a *direct* lambda call is dispatched through `MethodOperations.CallMethod` reflection. A fast path that invokes `LambdaMethod.Invoke` directly when the resolved target is already a `LambdaMethod` would remove both the invoke-stub frames on descent **and** the `TargetInvocationException` filter frames on unwind — plausibly raising the usable ceiling by a large factor and speeding up every lambda-heavy script as a side effect. **This is a performance/architecture task in its own right and is explicitly not part of this design.**

### 11.6 Invoking a script lambda from host code — the rule, and the §12 correction it requires

**The rule, in one line: if the engine handed you a `ScriptContext`, invoke through it.**

| You are… | Call | Because |
|---|---|---|
| a host **extension method** taking a `LambdaMethod` | declare a trailing `ScriptContext` parameter; `lambda.InvokeFrom(context, …)` | the engine injects the parameter (`MethodOperations.CreateParameters`) and it consumes **no script argument** — the script call site is unchanged. This is the only way to get correct depth accounting. |
| host **C# code** driving a lambda returned from `Execute` | `lambda.Invoke(…)` | there is no invoking context; the captured budget is the only budget. Correct — but see the residual below. |

**Residual (item 6 above):** driving one returned lambda on *M* of your own threads charges all *M* to one counter. There is no public entry point that mints a fresh budget (`InvokeOnNewStack` is internal, `TaskHost.Run`-only). Mitigation today: size `MaxDepth` above *M*. Whether this deserves public surface is **OQ-9**.

**`docs/pooscript-language-reference.md` §12 currently overstates the fix** and must be corrected — it is the section host authors read, and it currently promises a guarantee that does not extend to the overload they will actually call. Replace the final claim of the "what the engine still cannot guarantee" bullet with:

> `task.run` bodies get an independent depth budget per task. On **every engine-dispatched invocation** — `$f.invoke(…)` in script, a `task.run` body, and any host extension method that declares a `ScriptContext` parameter and invokes through it — the budget is resolved from whichever context is actually invoking the lambda at the moment of the call rather than from wherever that lambda happened to be defined, so a shared helper lambda captured outside a `task.run` body and invoked from inside it is bounded correctly too, the same as one defined inline. ⚠ **This does not extend to `LambdaMethod.Invoke(params object[])`, the overload host C# code calls directly** — that one resolves the budget from the context the lambda was *defined* in. **A host extension that accepts a `LambdaMethod` must declare a trailing `ScriptContext` parameter** — the engine injects it, it consumes no script argument, and the script-level call is unchanged — **and invoke through `InvokeFrom(context, …)`.** An extension that calls `Invoke(…)` instead charges every concurrent callback to the defining script's single counter, so *N* simultaneous non-recursive callbacks abort a `MaxDepth` of *N-1* with no recursion involved. `Invoke(…)` is correct only where there is no invoking context at all — host code or a test driving a lambda returned from `Execute`; such a host driving one lambda on several of its own threads hits the same shared-counter shape and must size `MaxDepth` above its own concurrency.

---

## 12. Backward Compatibility — structural, not conventional

**The CF-2 precedent (#7717).** #7712 §8 specified "one **immutable** options object". The implementation shipped `{ get; set; }` on a shared `static readonly None` instance, and `parserA.Limits.Timeout = 50ms` silently imposed that timeout on **every other default-configured parser in the process** — including freshly constructed, never-configured ones. The design's intent was correct; the guarantee was conventional, and a convention is not a guarantee. QA proved the fix by showing the offending code **no longer compiles** (`error CS8852`), which is the right kind of proof.

**This design's default-off guarantee rests on three structural properties, each mechanically checkable:**

| # | Property | Enforced by | Falsifiable test |
|---|---|---|---|
| S1 | The three new knobs cannot be set on the shared `ScriptLimits.None` instance | `init`-only accessors (`ScriptLimits.cs` pattern, A2). Assignment is a **compile error**, not a runtime convention. | T13a — a compile-time-shape assertion mirroring `CF2_DefaultLimitsAreNotSharedMutableState` |
| S2 | No budget object exists when its knobs are null | `GuardedExecution.Prepare` allocates `DepthBudget` only when `MaxDepth.HasValue`, and `VariableBudget` only when either variable knob has a value — mirroring the existing `StepBudget` line (`GuardedExecution.cs:49`) | T13b — assert the fields are null on a default execution |
| S3 | Neither new exception type can be produced when the knobs are null | Follows from S2: the only throw sites are inside the budget objects that were not allocated | T13c — the entire pre-existing 535-test suite runs unchanged |

**The strongest gate remains the one #7409 passed twice: no pre-existing test may be modified.** `git diff master -- Scripting.Tests/` must show only additions of new files. QA verified this in both rounds and it must hold again.

### Every observable change

| # | Change | Breaking? | Who is affected |
|---|---|---|---|
| B10 | `ScriptLimits` gains `MaxDepth`, `MaxVariables`, `MaxVariableBytes` (`init`-only, nullable) | **No** | Nobody — all default null (S1–S3) |
| B11 | `ScriptStepLimitExceededException` and `ScriptTimeoutException` re-parent from `ScriptException` to the new abstract `ScriptAbortException : ScriptException` | **No** — source- and catch-compatible | Every existing `catch` still matches; `ScriptRuntimeException` remains a sibling (QA #7719's verified property preserved). Only a consumer reflecting on `BaseType` would notice. |
| B12 | `Control/Try.cs` rethrows `ScriptAbortException` where it previously rethrew only `ScriptStepLimitExceededException` | **No** with default limits | The widened set is `{Timeout, Depth, VariableLimit}`. `ScriptTimeoutException` is unreachable inside `Try` (converted only at the `Script` boundary); the other two cannot exist unless their knob is set. **With a knob set, this is the intended behaviour** — the same B1 reasoning: a script must not be able to swallow an engine abort. |
| B13 | `ScriptMethod` (two chains) and `MethodOperations.cs:292` collapse their step-limit/timeout passthroughs into `ScriptAbortException` | **No** | Identical set of types passes through, plus the two new ones |
| B14 | `Pooshit.Scripting.Parser.VariableProvider` gains two **internal** members | **No** | Not public surface |
| B15 | `ExternalScriptMethod` shares the caller's depth budget with a nested engine `Script` | **No** with default limits | Behaviour differs only when `MaxDepth` is configured |
| B16 | `ScriptContext` gains two internal by-reference budget fields | **No** | `ScriptContext.Limits` stays public-get/private-set; the new fields are internal, matching `StepBudget` |
| B17 | `Operations/Values/ValueOperation.ExecuteToken` charges its result against the budget (M1/M2) | **No** with default limits | `VariableBudget` is null when neither variable knob is set, so the added code is one null check. With `MaxVariableBytes` set, an arithmetic result exceeding the budget now aborts — the intended behaviour. |
| B18 | `Operations/AssignableToken.Assign` charges the assigned value against the budget (M1/M2) | **No** with default limits | Same. Note this is the **base** class, so it covers variables, members, indexers and compound-assign in one place. |
| B19 | `Providers/LambdaMethod` now implements `IExternalMethod` (DiVoid #7744/#7749, CF-1/CF-5 residual fix) | **No**, additive | Not a compile break — the explicit `IExternalMethod.Invoke` implementation delegates to a new, distinctly-named public method (`InvokeFrom(ScriptContext, object[])`, DiVoid #7744 round 4 — an earlier version added a second `Invoke` overload instead, which QA test-compiled and found ambiguous, `CS0121`, for ordinary calls like `lambda.Invoke(null)`; the rename retires that ambiguity structurally rather than documenting it), and the existing `Invoke(object[])` is unchanged — **and stays unchanged, deliberately (DiVoid #7782, §7.7): it resolves the depth budget from the context the lambda was *defined* in, where the two new entry points resolve from the invoking context and from a fresh one respectively. That split is a documented property of the public surface, not an oversight; "unchanged" in this row is a decision, not an omission.** The observable behavioural change: any code doing `value is IExternalMethod` now also matches a `LambdaMethod` instance, where it previously matched only `ExternalScriptMethod`. Low risk (no known consumer does this type test outside the engine's own `ScriptMethod.ExecuteToken`, which is precisely the dispatch this exists to reach), but unrecorded until now — see the correction to A2 below. |
| B20 | `EnumerableExtensions.Where`, `.IndexOf(predicate)` and `.LastIndexOf(predicate)` each gain a trailing `ScriptContext` parameter (DiVoid #7744 CF-5) | **YES** — compile, direct C# callers only | Same class as #7409's B4 (`cancellation-support.md` row 396): a compile break for direct C# callers, script surface unaffected. Script-level `.where(...)`/`.indexof(predicate)`/`.lastindexof(predicate)` calls are unchanged — the parameter is engine-injected via `MethodOperations.CreateParameters` and invisible to script code; verified independently by QA against the eight-plus pre-existing `.where(`/`.indexof(` tests in `EnumerableExtensionTests.cs`, unmodified and passing. See the correction to `cancellation-support.md` §7.5 ("one line, one site") below, which is the claim this row falsifies. |

**Superseded, 2026-08 (DiVoid #7744/#7749).** The line below claimed zero public-interface changes across this whole design; that held for phases 1–3 as merged, until the CF-1/CF-5 residual fix (`LambdaMethod : IExternalMethod`, plus the `EnumerableExtensions` signature changes in B20) landed as part of closing out #7744/#7749. B19 is additive, not a break; B20 is a compile break, scoped to direct C# callers only, per the B4 precedent. "Zero public-interface changes" is no longer literally true and should not be quoted as such. Retained below for the historical record of the original design intent.

**Zero public-interface changes.** Unlike #7409 (which broke `IScript`, `IExternalMethod`, `EnumerableExtensions`, `TaskHost.WaitAll` and `MethodOperations.CreateParameters`), this design adds **no** compile-breaking public signature change. That is a deliberate design constraint, not a coincidence — §8.4's decision to stop the walk at foreign providers is what avoids an `IVariableProvider` break.

**Carry-over from #7719:** the still-open **W-5 / B9** row (`MethodOperations.CreateParameters` binary break) is documentation-only and belongs to #7409's release notes, not to this change. It is noted here so it is not lost; it is **not** in scope.

---

## 13. Test Plan — the implementer must hit all of these

Convention: a new `Scripting.Tests/ExecutionGuardTests.cs`, mirroring `CancellationSupportTests.cs`. **Every unbounded-script test carries `MaxTime(2000)`** (W-1, #7717). **No pre-existing test file may be modified.**

### Depth

| # | Test | Asserts |
|---|---|---|
| T1 | Recursive lambda with `MaxDepth = 32` | `ScriptDepthLimitExceededException`, `Limit == 32`, process survives |
| T2 | Same script, `MaxDepth = null` | Unchanged behaviour (use a *bounded* recursion — never actually overflow the stack in CI) |
| T3 | Deeply nested but **finite** blocks/loops (e.g. 40 nested `if`s, no calls), `MaxDepth = 8` | **Does not throw** — pins §7.1 Error 1 |
| T4 | Bounded recursion to depth `MaxDepth - 1`, then return | Completes normally — pins that `Exit` decrements |
| T5 | Recursion that unwinds through a script `throw` / `return` / `break`, repeated in a loop | No depth leak: a subsequent legal call at depth 1 succeeds — pins the `try`/`finally` pairing |
| T6 | Mutually recursive `import` chain (a→b→a), `MaxDepth = 16` | `ScriptDepthLimitExceededException` — pins §7.3 (depth crosses) |
| T7 | Imported script's own step budget with `MaxDepth` set | Nested `StepBudget` still independent — pins #7712 §7.8 is unchanged |
| **T8** | **`MaxDepth + 1` concurrent `task.run` lambda bodies, each nesting only one level** | **Must NOT throw.** Each `task.run` body runs on its own pool thread with its own stack, so it must not consume the parent's depth. **This test currently fails against the shipped implementation — see §7.6 and R11.** It is the test that pins the fix. |
| T9 | `.where($x => …)` / `.indexof(pred)` over a large collection, `MaxDepth = 4` | **Does not throw** — callback invocations are sequential, not nested. Guards against an `Enter`-without-`Exit` bug that a naive implementation would show here first. |
| **T9a** | **A *test-local* host extension declaring a trailing `ScriptContext` and calling `InvokeFrom`**, driven with `MaxDepth + 1` concurrent invocations of one shared predicate captured outside the task bodies, `Barrier`-forced, **zero recursion** | **Must NOT throw.** Pins §11.6's prescribed host-extension pattern against the exact shape QA #7744 round 3 measured. Must not use `EnumerableExtensions`. |
| **T9b** | **The same test-local extension, second method, calling `Invoke(args)` instead**, same concurrent shape | **Throws `ScriptDepthLimitExceededException`.** A *characterisation* test: it pins §7.7's decision that `Invoke`'s captured-context semantics are deliberate. Its failure means someone changed the contract; that must be a re-argued decision, not a silent one. |
| **T9c** | **Recursion routed through the test-local `Invoke(args)` extension at *every level*** (`$fac = $n=>{ if($n>0) { return($fac.<ext>($n-1)) } return(0) }`), recursion bounded to a **finite** literal well above `MaxDepth` (≈32) and well below the measured crash depth | **Throws `ScriptDepthLimitExceededException`.** The load-bearing test: it is what fails if `Invoke` is ever given a fresh budget (§7.7 option d), which would otherwise read depth 1 forever. **The finite bound is mandatory** — with an unbounded literal (`1000000`) a regression would not fail the assertion, it would kill the test runner. |

> ⚠ **Every depth test must use a `SafeMaxDepth` constant in the high single digits, never a "realistic-looking" value.** Measured: `MaxDepth = 20` produces an uncatchable `StackOverflowException` on the *unwind* path; `10` is safe; the shipped suite uses `8`. See §11.1–11.2. A test that hard-codes `MaxDepth = 50` does not fail — **it kills the test runner.**

### Variables

| # | Test | Asserts |
|---|---|---|
| T10 | `while(true) { $l.add("xxxx") }` with `MaxVariableBytes` set | `ScriptVariableLimitExceededException`, `Kind == Bytes` — the in-place-mutation shape M3 exists for (§8.2.1) — nothing at an assignment or operator site can see it |
| T11 | `$s = $s + $s` in a loop with `MaxVariableBytes` set | Same — the assignment-driven shape |
| T12 | `while(true) { $x = 1 }` with `MaxVariables` set low | **Does not throw** — pins §8.3's scope-death trap. **This test is the one that catches the naive incremental counter.** |
| T13 | Recursive lambda binding parameters, `MaxVariables` set | Throws with `Kind == Entries` — the one shape the light tier genuinely catches |
| T14 | Host-supplied root variable holding a large object, `MaxVariableBytes` set low | **Does not throw** — pins the §8.4 boundary |
| T15 | A `list` containing itself, `MaxVariableBytes` set | Terminates (does not stack-overflow in the sizer) — pins the depth cap as cycle guard |
| T16 | A host object exposing an infinite non-`ICollection` `IEnumerable` held in a variable, `MaxVariableBytes` set | Terminates — pins "never enumerated" |
| T17 | Large bounded script with `MaxVariables`/`MaxVariableBytes` set generously | Completes; wall-clock within a small multiple of the unguarded run — pins the §8.2.3 amortisation |
| T18 | `task.run` bodies writing variables while the main thread measures, `MaxVariableBytes` set | No `InvalidOperationException` escapes to the host — pins §8.7 best-effort |
| **T18a** | **`$s = $s + $s` in a loop, `MaxVariableBytes` set** | Aborts, and the observed footprint at abort is **≤ 2× the budget** — pins M1 and the §8.2.4 bound. **This is the test the sampling-only design would have failed**, because the 256-checkpoint floor allows 256 doublings. |
| **T18b** | **A single statement with a long concatenation chain** (`$s + $s + … `, ≥64 terms), `MaxVariableBytes` set | Aborts at an intermediate, **not** after the chain completes; peak allocation independent of chain length — pins the §8.2.4 chain verdict |
| **T18c** | `$i = $i + 1` in a tight loop with `MaxVariableBytes` set generously | Completes; no measurable regression vs. the unguarded run — pins that M1's type tests are free on the numeric path |
| **T18d** | Loop appending separately-produced values, `MaxVariableBytes` set | Aborts at ≤ ~2× budget — pins M2 forcing a pass ahead of the sampled interval |
| **T18e** | Compound assign `$s += $s` in a loop, `MaxVariableBytes` set | Aborts — pins that the hook is on `AssignableToken.Assign` (the base), since `+=` does not route through `ValueOperation` |

### Contract (rule #7718)

| # | Test | Asserts |
|---|---|---|
| T19 | Depth breach raised inside `$x = <recursive call>` | Reaches the host as `ScriptDepthLimitExceededException`, **not** `ScriptRuntimeException` — `AssignableToken` (the CF-1 site) |
| T20 | Depth breach raised inside `throw($recursive())` | Same — `Throw.cs`, the round-2 site |
| T21 | Depth breach inside a reflected host method that invokes a script lambda | Same — `MethodOperations.cs:292` filter |
| T22 | Depth breach inside an `import(...).invoke()` | Same — `ScriptMethod` chain |
| T23 | **`try { <depth breach> } catch($e) { }`** | The abort **propagates**; the script's `catch` does **not** run — `Try.cs`. **The highest-value test in this table.** |
| T24–T27 | T19–T23 repeated for `ScriptVariableLimitExceededException` | Same set, both new types |
| T28 | Async path: task state is **Faulted**, not Canceled, for both new types | §9.3 rows e, f |
| T29 | Sync `Execute(vars, ct)` path for both new types | §9.3 |
| T30 | Caller cancel while a depth-limited script runs | Still **Canceled** + `OperationCanceledException` — the new types do not disturb §9.1 row (a) |

### Backward compatibility

| # | Test | Asserts |
|---|---|---|
| T31a | `ScriptLimits.None` new knobs cannot be assigned | Compile-shape assertion mirroring `CF2_DefaultLimitsAreNotSharedMutableState` (S1) |
| T31b | Default execution allocates neither new budget | Both internal fields null (S2) |
| T31c | Two independently-configured parsers do not share limit state, including the new knobs | Extends the existing CF-2 regression test's shape (S1) |
| T32 | Full pre-existing suite (535 tests at `9b969ec`) | **0 failed, 0 modified test files** (S3) |
| T33 | Both TFMs build clean | `netstandard2.0` + `net8.0`, 0 errors |

---

## 14. Implementation Guidance — ordered phases

Each phase is independently reviewable and leaves the tree green. See the PR-split note below the table.

| Phase | Work | Depends on |
|---|---|---|
| **1** | `ScriptAbortException` (abstract); re-parent `ScriptStepLimitExceededException` and `ScriptTimeoutException`; collapse the four narrow enumeration sites (`Try`, two `ScriptMethod` chains, `MethodOperations.cs:292`) to the two-clause shape. **Ship this alone first and run the full suite** — it must be a pure no-op (B11–B13). | — |
| **2** | `ScriptDepthLimitExceededException`; `DepthBudget`; `MaxDepth` on `ScriptLimits`; allocation in `GuardedExecution.Prepare`; propagation on `ScriptContext`'s copy constructor. | 1 |
| **3** | `Enter`/`Exit` at `LambdaMethod.Invoke` and `ExternalScriptMethod.Invoke`; the internal nested-execution entry on `Script` that inherits the depth budget. Tests T1–T9, T19–T23 (depth arm), T28–T33. | 2 |
| **4** | `VariableSizer` + its `const`s, **unit-tested standalone** before it is wired to anything. Tests T15, T16 at this level. | PR 1 merged |
| **5** | Two internal members on `VariableProvider`; `VariableBudget` (walk, cadence, best-effort concurrency, **plus the M1 ceiling and M2 trigger helper**); `ScriptVariableLimitExceededException` + `VariableLimitKind`; the two knobs. | 4 |
| **6** | Wire the three mechanisms: the one new line in `Guard()` (M3); the shared helper call at `ValueOperation.ExecuteToken` and `AssignableToken.Assign` (M1/M2); allocation in `Prepare`. Tests T10–T18e, T24–T27. | 5 |
| **7** | Documentation: `docs/pooscript-language-reference.md` §1, §12, §14 — the three new knobs, the six-row exception table, **verbatim** the §11 host-contract residuals (the overshoot bound, the opaque-host-object under-count), and the **`MaxDepth` calibration procedure with its halving rule (§11.3) and the self-defeat warning (§11.2)**. Update #7718 with the two-clause shape and the `Try.cs` row. | 3, 6 |

**PR split (resolved).** Phases **1–3 are PR 1** (the depth guard) and **phases 4–7 are PR 2** (the variable guard). They are independently meaningful and independently valuable, and PR 1 closes the only residual that kills the host process. **PR 2 is implemented against merged code**, so it depends on PR 1 for concrete shapes it does not re-derive: `ScriptAbortException` (its exception derives from it), the `Guard()` line placement, the `GuardedExecution.Prepare` conditional-allocation pattern, and the `ScriptContext` by-reference budget-propagation convention. Nothing in PR 2 modifies PR 1's mechanism.

**Three things the implementer must not do:**

1. **Do not implement the entry count incrementally.** §8.3 — it produces a false positive on `while(true) { $x = 1 }`. T12 exists to catch this.
2. **Do not add a `catch(ScriptException){throw;}` to `ScriptMethod` or `MethodOperations` as a shortcut** for the passthrough. #7718 requires narrow passthrough there; `DebugTests.ExternalMethodFail` depends on `ScriptRuntimeException` still being re-wrapped with outer call-site context.
3. **Do not build M1/M2 as delta accounting** (charge new, un-charge old). §8.2.4 — un-charging needs the *current* size of a value that may have been mutated in place since it was charged, so the figure drifts. M2 is monotonic and reset by every M3 pass, and is never read as a live footprint.

**Bounce this design if:** phase 1 is not a pure no-op against the existing suite, or T3 / T9 / T12 / T18a cannot be made to pass — each is a load-bearing claim in §7.1, §7.2, §8.3 and §8.2.4 respectively, and a failure there means the reasoning is wrong, not the code.

---

## 15. Pre-Design Checklist (#1136 §5) — answered in order

### KISS / DRY / YAGNI

- **No new type mirroring an existing type's value-space.** `DepthBudget` and `VariableBudget` do not mirror `StepBudget` — monotonic vs. enter/exit vs. sampled are three genuinely different lifetimes (§7.5). `ScriptAbortException` is a new *category*, not a mirror. `VariableLimitKind` has two concrete values needed today.
- **No abstraction with one implementation and no second.** `ScriptAbortException` has **four** concrete subtypes on day one. No new interfaces are introduced.
- **No element justified by "we might need X later".** The one forward-looking decision — rejecting `[ThreadStatic]` for a context-carried budget — is grounded in **#7713, a filed task** that rewrites the exact call sites, not in a hypothetical (§7.1).
- **No deprecation period, feature flag, compatibility shim or transition window.** None present.
- **`block_size × site_count` quoted at every extraction decision:**
  - `ScriptAbortException`: `3 lines × 4 types × 3 catch-chain sites = 36` — **above** threshold → extract (§9.2).
  - Depth `Enter`/`Exit`: `5 × 2 = 10` — **below** threshold → no extraction beyond the budget object's own API (§7.2).
  - Variable-guard sampled checkpoint (M3): **1 line at 1 site** — `Guard()` already exists (#7712 §7.6); the guard costs one line, not a new checkpoint sweep (§6.3).
  - Variable-guard value charge (M1/M2): **1 helper call at 2 sites** — `6 × 2 = 12`, **below** the ~15–20 threshold, so `ChargeProducedValue` is the budget object's own API rather than a DRY extraction. Both sites are **shared base classes** (`ValueOperation.ExecuteToken` covers all twelve arithmetic/bitwise operators; `AssignableToken.Assign` covers every assignment form), which is what keeps the count at 2 instead of 12+ (§8.2.1).
  - Consolidating three budgets into one: rejected, cost math in §7.5.

### Existing systems first

- **Audited.** Every element lands on a surface #7409 already built: `ScriptLimits` (3 knobs → 6), `ScriptContext` budget-by-reference (1 field → 3), `Guard()` (+1 line), `GuardedExecution.Prepare` (+2 conditional allocations), `ScriptParser.Limits` (unchanged). **No new host-facing surface, no new entry point, no new options object, no public-interface change.**
- **New layers proposed, with the concrete reason each cannot live on an existing surface:**
  - `DepthBudget` — cannot be a field on `ScriptContext` because contexts are per-scope and the counter must be shared by reference across a call chain (§7.1). This is #7712 §7.6's own state-ownership rule.
  - `VariableBudget` — same, plus it owns cadence state that must survive scope derivation.
  - `VariableSizer` — a pure function; separated from `VariableBudget` only so it can be unit-tested standalone (phase 4). If the implementer prefers it as a private static on `VariableBudget`, that is acceptable and no worse.
  - `ScriptAbortException` — DRY math above.
- **No new persisted data.** Nothing is written anywhere; the guards throw and are gone.
- **Consumer chain recursed.** Every new element has a named consumer: the knobs → Uberkarl (#7407); the exceptions → Uberkarl's per-script error attribution (#7712 §9.1); the budgets → `Guard()` and the two call sites. No transitive-dead chain.

### Configurability

- **Every knob has a named host with a named opposite setting today** (§10): Uberkarl sets them; Mamgo `scriptservice` and `ScriptExecutor` leave them null. Not "for future tuning".
- **No telemetry-then-tune compound.** No audit column, no per-execution record, no measurement persisted anywhere.
- **Magic numbers stay `const`.** `MinMeasureInterval` (256), the sizer's header/opaque constants, and the sizer's depth cap (4) are named `const`s in code with no knob (§8.2, §8.5). `MaxDepth` ships **no default** — the engine has no opinion (§10, §11).

### Less is better

- **Can it be deleted?** Applied to every element. It removed the fourth-knob idea of a separate "recursion exception hierarchy", the deduplication set for aliased values (§8.6), the visited-set for cycles (folded into the depth cap, §8.5), the locking around measurement (§8.7), and a global live-scope registry (§8.7). It also removed **full delta accounting** for M1/M2 in favour of a monotonic trigger that cannot drift (§8.2.4), and an **engine-side clamp on `MaxDepth`** in favour of a calibration procedure, because a clamp would be a false guarantee (§11.4). It **did not** remove `MaxVariables` — kept, per the OQ-1 resolution, documented honestly as a rail rather than the memory guard (§8.3).
- **Can it be merged?** Yes, and it was: two variable knobs share one measurement pass, one cadence, one exception type (§8.1). The abort passthrough merged into one clause per site (§9.2).
- **Can it be inlined?** The depth `Enter`/`Exit` was checked at `5 × 2 = 10` and left as the budget object's own methods rather than extracted further.
- **Does the abstraction earn its keep?** `ScriptAbortException`: 36 duplicated lines and a rule that has been missed three times (#7718). Yes.
- **Trade-offs named explicitly** where a more complex option won: §7.1 (context-carried vs. `[ThreadStatic]` vs. `AsyncLocal`), §7.3 (one inherited ceiling vs. per-frame ceilings), §7.5 (additive vs. consolidated budgets), §8.2.2 (sampling-only vs. sampling + production/assignment checks — **a stated position change**), §8.2.3 (size-proportional vs. fixed interval, with the `n²/2N` math), §8.2.4 (growth trigger vs. delta accounting), §8.7 (best-effort vs. locking), §11.4 (documented calibration vs. an engine clamp).

### Data deliverables

- **N/A.** No SQL, no migration, no backfill, no schema identifier.

### Document discipline

- Code Contracts (#114 §0) and Design Contracts (#1136) cited as load-bearing in the header.
- Scope and out-of-scope inventories explicit (§2), including the four items the brief named.
- All seven abort-passthrough sites enumerated from the merged tree with file and line (§9.2), not from #7712's inventory — which is exactly the gap #7718 was written about.
- **No predecessor is superseded.** #7712 remains live and correct; this document *extends* it. No `SUPERSEDED` banner is required, and none is added. Where the two differ on merged-code detail (`init`-only knobs, six `Guard()` sites not sixteen, the fourth `Throw.cs` wrapper), this document states the merged reality and §9.2's table is the corrected inventory.
- **No principle-overriding decision is left paraphrase-grounded.** Every one states its math: §7.2 (`5 × 2 = 10`), §8.2.1/§15 (`6 × 2 = 12`), §8.2.3 (`n²/2N` and the amortisation bound), §8.2.4 (the overshoot table and the `n×` → `3×` chain collapse), §9.2 (`3 × 4 × 3 = 36`), §7.5 (one branch vs. three orders of magnitude), §11.1 (the measured 20 / 10 / 8).

---

## 16. Risks & Mitigations

| # | Risk | Mitigation |
|---|---|---|
| R1 | `Enter` without a matching `Exit` leaks depth; the script aborts spuriously after enough calls | `try`/`finally` at both sites, pinned by T5 (unwind through throw/return/break) and T9 (sequential callbacks) |
| R2 | The measurement walk becomes a hot-path cost on a legitimate large script | Size-proportional interval (§8.2.3), amortised ≤ 1 unit per checkpoint, pinned by T17 |
| R3 | Concurrent-modification fault escapes to the host as a script error | Best-effort abandon (§8.7), pinned by T18 |
| R4 | The sizer under-counts so badly the guard is theatre for a given host | Blind spots enumerated (§8.5) and carried verbatim into the host contract and the language reference. **The guard must never be documented as heap containment.** |
| R5 | A new exception type is swallowed at one of the seven sites | The `ScriptAbortException` collapse reduces seven inventory items to a fixed two-clause shape; T19–T27 test each site individually. #7718 is the standing rule and is updated in phase 7. |
| R6 | `MaxDepth` set too low by a host, breaking legitimate scripts | No default shipped; §11.3 gives a calibration procedure. T3/T9 pin that block nesting and sequential callbacks do not consume depth. **Low severity** — a ceiling in the high single digits is the design point, and Toni has accepted that recursion is effectively unavailable to script authors under an adversarial threat model (§11.5). |
| R7 | #7713's async rewrite invalidates the depth mechanism | Explicitly designed against (§7.1): the budget flows on `ScriptContext`, which flows with the logical call. The `finally` pairing survives `await`. Nothing to rip out. |
| R8 | Phase 1 (the exception re-parent) is not the no-op it must be | Shipped and suite-run in isolation before anything else (§14). If it is not a no-op, the reasoning in §9.2 is wrong and the design should bounce. |
| **R9** | **`MaxDepth` set too *high* by a host — the guard kills the process while enforcing itself** | **The severe one, and the only risk here with no mechanical mitigation.** Measured: `20` overflows on the *unwind* through stacked `TargetInvocationException` filter frames (§11.2). Mitigated by documentation only — the measured numbers, the self-defeat property stated as a property, and the calibration procedure with its halving rule, carried into the `MaxDepth` XML docs and the language reference. **No engine clamp**, because the safe ceiling depends on the host's thread stack size and a clamp would be a false guarantee (§11.4). Residual risk is real and accepted; the structural fix is the reflection fast path in §11.5, which is a separate task. |
| **R11** | **Concurrent `task.run` bodies share one depth counter and abort spuriously** | ~~OPEN DEFECT~~ **RESOLVED 2026-08-06** (#7749): `TaskHost.Run` → `InvokeOnNewStack`, `LambdaMethod : IExternalMethod`, `EnumerableExtensions` on `InvokeFrom`. Pinned by T8. The public-surface residual is R12. |
| **R12** | **A host extension calling `LambdaMethod.Invoke(args)` gets pre-#7749 accounting — concurrency counted as nesting on a lambda captured outside the concurrent bodies** | **Accepted as a documented property, not fixed (§7.7).** Every mechanical alternative trades this catchable false positive for an uncatchable false negative, and there is no backstop beneath `MaxDepth` (§11.2). Mitigated by: XML remarks naming the failure and the remedy, the §11.6 rule, the §12 correction, and T9a/T9b/T9c. **Residual is real:** a host that never reads the documentation and configures `MaxDepth` can still see a spurious abort. Severity is bounded — the abort is catchable and carries its `Limit`, where the alternative is process death. |
| **R10** | **M1/M2's checks regress the arithmetic hot path** | Both sites early-out on a failed type test for the numeric case (`$i = $i + 1`), and `VariableBudget` is null when unconfigured. Pinned by T18c. If T18c shows a measurable regression, narrow the M1 site to `Addition` only — losing coverage of any future size-increasing operator but preserving the hot path. |

---

## 17. Questions — Resolved and Open

### 17.1 Resolved (2026-08-05)

| # | Question | Resolution |
|---|---|---|
| **OQ-1** | Keep `MaxVariables` (the light tier)? | **Keep**, documented honestly as a cheap scope-accumulation rail, explicitly *not* the memory guard (§8.3). |
| **OQ-2** | Is an approximate, lagging, host-object-blind guard good enough? | **Build it.** The boundary is confirmed correct — *"script should handle script, not the whole engine"* — and peak overshoot is acceptable **provided the limit is a budget set with headroom, not a cliff at the system boundary** (§1, §8.2.4). The design's obligation in exchange is to **quantify** the overshoot, which §8.2.4 does: ≤2× observed, ≤3× transient, for every covered shape. |
| **OQ-2b** | Where does the check belong — the set-variable token, or sampled? | **Both, plus a third.** Position changed: §8.2.2 records the reconciliation and the defect in the sampling-only design that prompted it. |
| **OQ-3** | Ship `MaxDepth` first as its own PR? | **Split taken.** PR 1 = phases 1–3 (depth, implemented). PR 2 = phases 4–7 (memory), built against merged code (§14). |
| **OQ-4** | Does any Uberkarl script legitimately recurse? | **Assume adversarial authorship.** Imports are not expected to be enabled, so **recursive lambdas within a single script are the load-bearing case**; guidance is framed for an author actively trying to kill the host (§11.5). |
| **OQ-5** | Make #7718 an executable check? | Good idea, **out of scope here** — the operator files it separately. Not designed. |
| **Impl.** | Whose `MaxDepth` applies across an import boundary? | **One ceiling for the whole nest, outermost wins** — a single fixed `Limit` on `DepthBudget`, inherited budget beats a nested allocation. Confirms the implemented shape; §7.3 rewritten to say so unambiguously. |

### 17.2 Still open

**OQ-6 — Does the `MaxDepth` ceiling of ~8 change Uberkarl's script-authoring guidance?** A ceiling in the high single digits means **recursion is effectively unavailable to community script authors** (§11.5). That is an acceptable consequence of the adversarial threat model, but it is a *product* statement, not just an engine one: if any published example, template or tutorial script uses a recursive lambda, it will break. Worth a scan before the knob is turned on in Uberkarl.

**OQ-7 — Should the reflection fast path for direct lambda calls be filed now?** §11.5 — dispatching a direct `LambdaMethod` call without going through `MethodOperations.CallMethod` reflection would remove both the invoke-stub frames on descent and the `TargetInvocationException` filter frames on unwind, plausibly raising the usable `MaxDepth` by a large factor and speeding up every lambda-heavy script. It is the **structural** fix for R9, which currently has documentation-only mitigation. Out of scope here; recommended as a follow-up task with a measured before/after.

**OQ-9 — Does a host need a public "invoke on a new stack" entry point?** §7.7 / §11.6. A host driving a returned `LambdaMethod` on *M* of its own threads charges all *M* to one counter, has no invoking context to pass to `InvokeFrom`, and cannot reach `InvokeOnNewStack` (internal, `TaskHost.Run`-only). Making it public is additive and one method, but it is **new public surface with no named consumer today** (#1136 §2), and the library has just absorbed B19/B20. **Not designed here.** Turn it into a task only if a real host reports the shape; until then the mitigation is "size `MaxDepth` above your own concurrency", recorded in §11.6.

**OQ-8 — Does any host run the interpreter on a non-default thread stack?** §11.3 step 5 offers "raise the thread's stack size and re-calibrate" as the escape hatch for a host that needs more depth. If Uberkarl already runs scripts on a pooled thread with a known stack size, the calibration should be run there rather than on the default, or the resulting number will not transfer.
