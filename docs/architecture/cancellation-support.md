# Architectural Document: Complete Cancellation Coverage & Built-in Execution Guards

**Repo:** `Pooshit.Scripting` (project `Pooshit.Scripts/`) · branch off `master`
**DiVoid:** task **#7409** · driver **#7407** (Uberkarl) · language reference **#2946** · defect **#2899** · operator crawl **#7711**
**Contracts (load-bearing):** Design Contracts **#1136**, Code Contracts **#114 §0**, DRY threshold **#1267**
**Status:** design for implementation by john-backend-dev. No code in this document.

---

## 1. Problem Statement

Pooscript is an embeddable interpreter with an allow-list sandbox (no reflection, no file/network, no `new` unless registered). The sandbox controls *what a script can touch*; it does not control *how long a script runs*. A host today has no reliable way to stop a script that misbehaves.

The user framed the ask verbatim:

> "so we have a request to have proper cancellation support in script execution - check all tokens where it makes sense (loops etc) and make sure any script can be canceled and has no gap where it could still freeze if written improperly."

and, on the async entrypoint specifically:

> "the ExecuteAsync was written naively to have an async entrypoint - see whether there is a way to turn it into a true async state machine. If its too much work, split full rewrite to separate task. Minimal goal is still a non leaking but also non blocking behavior - cancel has to cleanly stop the script."

**Business driver.** Uberkarl (#7407) will run **untrusted community scripts** as level behaviour, wrapped in a watchdog (`ExecuteAsync` + time budget → cancel). That watchdog is only as good as the engine's weakest observation point. Mamgo's `scriptservice` and the `ScriptExecutor` CLI run *trusted* scripts and must see **zero** behaviour change unless they opt in.

**Success criteria.**

| # | Criterion | Verified by |
|---|---|---|
| S1 | Every engine-controlled loop, block, blocking wait and callback observes the cancellation token. | Bounded-time tests §12 |
| S2 | Script `try/catch` cannot swallow an engine cancellation. | Test T3/T4 |
| S3 | Cancellation reaches code the sync `Execute` path runs, and imported scripts. | Tests T9, T11 |
| S4 | A host can bound execution by wall-clock time, by step count, and bound regex matching — all opt-in, all defaulting to today's behaviour. | Tests T8–T10, T13 |
| S5 | On cancel, `ExecuteAsync` completes promptly (non-blocking) **and** the worker unwinds (non-leaking). | Test T14 |
| S6 | Existing hosts observe no change unless they set limits, except the one intentional `try/catch` break. | Full existing suite green |

---

## 2. Scope & Non-Scope

### In scope

1. Closing all eight confirmed token-observation gaps (§5) plus the three found by the independent crawl (§6).
2. Three opt-in host guards: **execution timeout**, **step limit**, **regex match timeout** (§8).
3. Making engine cancellation **uncatchable by script code** (§7.2) — an intentional behaviour change.
4. Adding cancellation to the **sync** `IScript` surface (§7.7).
5. A `ScriptContext` injection seam so engine-shipped and host-supplied methods can observe the token (§7.5).
6. The exception contract a host sees (§9) and the host-side contract for what the engine *cannot* guarantee (§10).
7. Test plan (§12).

### Explicitly out of scope

| Item | Why |
|---|---|
| **Defect #2896** (`foreach` + `continue`, `Foreach.cs:60` tests `value` instead of `bodyvalue`) | Different bug, different PR. **No structural collision:** it lives in the loop-control branch after the guard call; this design only replaces line 47's check and does not touch lines 53–66. |
| **True async state machine** rewrite of the interpreter | Split to a follow-up task — see §11 for the verdict and the task spec. |
| **Compiled path** (`ParseDelegate`, `Expressions/ExpressionBuilder.cs`) | See §6.3 — consciously out, with reason. |
| **Memory exhaustion** guards (`$s = $s + $s` in a loop; `new string('x', huge)`) | No cheap in-process mechanism in .NET; a step limit does not help because a single allocation blows the heap. Residual documented in §10. |
| **Stack overflow** from recursive lambdas / recursive imports | `StackOverflowException` is uncatchable and kills the process. A recursion-depth guard is a fourth knob with no named consumer *yet*; see Open Question OQ-2. |
| **Process/AppDomain isolation** | The only complete answer to "untrusted code cannot wedge my process". Architecturally a host concern, not an engine concern. §10. |
| **`Converter.RegisterConverter` thread-safety** (#2909) | Separate defect. |

---

## 3. Assumptions & Constraints

| # | Assumption / constraint | Confidence |
|---|---|---|
| A1 | Target frameworks are `netstandard2.0` and `net8.0`. Every mechanism named here exists on both: `CancellationToken.WaitHandle`, `CancellationTokenSource.CreateLinkedTokenSource`, `Task.Wait(CancellationToken)`, `Task.WaitAll(Task[], CancellationToken)`, `Regex.IsMatch(input, pattern, options, matchTimeout)`. | Verified against the csproj |
| A2 | `IScript` and `IExternalMethod` have no external implementers. In-repo, `IScript` is implemented only by the internal `Script` class. `IExternalMethod` was implemented only by `ExternalScriptMethod` at the time this row was written; **superseded 2026-08 (DiVoid #7744/#7749)** — `Providers/LambdaMethod` now implements it too, added additively as part of the depth-guard CF-1/CF-5 residual fix (see `execution-guards-depth-memory.md` §12 B19). Still no *external* (out-of-repo) implementers known. | Verified by grep across `Pooshit.Scripts/`, `Scripting.Tests/`, `ScriptExecutor/` at the time; re-verify before trusting the "only `ExternalScriptMethod`" half of this claim anywhere else in this document |
| A3 | Library is pre-1.0 (`0.18.31`); additive interface changes are acceptable when called out. | Verified in csproj |
| A4 | `EnumerableExtensions` is **not** registered by default — a host must call `parser.Extensions.AddExtensions<EnumerableExtensions>()`. Its exposure is therefore host-elected. | Verified: no registration in the `ScriptParser` ctor |
| A5 | Mamgo's `scriptservice` does not wire an `ImportProvider` (#2946 §11.5), so `IExternalMethod` signature churn has no known downstream consumer. | From #2946; not independently re-verified against the Mamgo repo — see OQ-1 |
| A6 | The interpreter is a tree walker; a single "step" costs orders of magnitude more than an interlocked increment. | Structural, from the token model |
| C1 | Guards must default to **off**; no silent behaviour change for existing hosts. | User decision |
| C2 | Engine cancellation must be **uncatchable** by script `try/catch`. | User decision |
| C3 | One design per task; the async rewrite must be split if too large. | User decision |

---

## 4. Architectural Overview

The design rests on one idea: **the engine guarantees an interruption checkpoint at every point where control returns to the engine, and it guarantees nothing inside a single host call.** Everything follows from that line — it is both the design principle and the honest statement of the ceiling.

Two mechanisms realise it:

**(1) One checkpoint primitive.** Today four sites duplicate `context.CancellationToken.ThrowIfCancellationRequested()`. They collapse into a single `ScriptContext.Guard()` that performs the token check *and* the step tick. Every new checkpoint added by this design calls the same primitive.

**(2) Guards ride on the token wherever possible.** The execution timeout is not a new observation mechanism — it is a linked `CancellationTokenSource` with a deadline. It therefore inherits, for free and with zero new checks, the entire cancellation coverage this design establishes, and it works identically on the sync path. Only the step limit needs its own counter; only the regex timeout needs its own plumbing, because a runaway regex is inside a single host call and no token can reach it.

```
        host                                Script (engine boundary)
  ┌───────────────────┐            ┌──────────────────────────────────────┐
  │ parser.Limits     │──parse──▶  │ RunGuarded:                          │
  │  Timeout          │            │  ├ link(callerToken, deadline) ─┐    │
  │  MaxSteps         │            │  ├ build StepBudget? ───────────┤    │
  │  RegexTimeout     │            │  └ build ScriptContext ◀────────┘    │
  └───────────────────┘            │        │                             │
  ┌───────────────────┐            │        ▼                             │
  │ Execute(vars, ct) │──────────▶ │   token tree walk                    │
  │ ExecuteAsync(ct)  │            │        │                             │
  └───────────────────┘            │        └─ OperationCanceledException │
          ▲                        │            │                          │
          │                        │            ├ caller ct set  → rethrow │
          └── exception contract ──┤            └ deadline only  → Script- │
                   §9              │                              Timeout  │
                                   └──────────────────────────────────────┘

   ScriptContext  ──derives──▶ ScriptContext  ──derives──▶ ScriptContext
   (token, Limits, StepBudget reference — all propagate by reference)
        │
        └─ Guard() ─── called at: StatementBlock (per statement)
                                  While / For / Foreach (per iteration)
                                  LambdaMethod.Invoke (per invocation)
                                  EnumerableExtensions iterators (per element)
```

---

## 5. The Eight Confirmed Gaps — Decision Per Gap

Every gap from the operator crawl (#7711) was re-verified against the source. Each has an explicit decision.

| # | Gap (verified location) | Decision | Design |
|---|---|---|---|
| **G1** | `Control/Wait.cs:38,53,57,59` — four `Thread.Sleep` calls, no token. (#2899) | **Fix** | §7.1 |
| **G2** | `Control/Try.cs:22` — `catch(Exception)` swallows cancellation | **Fix (breaking, intentional)** | §7.2 |
| **G3** | `Tokens/Await.cs:40` — `task.Wait()`, no token | **Fix** | §7.3 |
| **G4** | `Hosts/TaskHost.cs:46` — `Task.WaitAll(...)`, no token | **Fix** (depends on the §7.5 injection seam) | §7.4 |
| **G5** | `Extensions/Script/EnumerableExtensions.cs` — host enumeration and script-lambda callbacks with zero checks | **Fix, in two halves** | §7.5 |
| **G6** | `Operations/Comparision/Matches.cs:23`, `MatchesNot.cs:23` — `Regex.IsMatch` with no `matchTimeout` | **Fix (opt-in guard)** | §8.3 |
| **G7** | Sync path has no token — `Script.cs:69` builds the 2-arg `ScriptContext` → `CancellationToken.None` | **Fix** | §7.7 |
| **G8** | No built-in timeout or step budget | **Fix (opt-in guards)** | §8 |

**Confirmed already-correct — do not touch** (the ticket's item 3 is a null result): `While.cs:28`, `For.cs:36`, `Foreach.cs:47` each check per iteration; `StatementBlock.cs:54` checks before every statement; `ScriptToken.cs:19` and `StatementBlock.cs:39,59` already rethrow `OperationCanceledException` rather than wrapping it; `ScriptContext.cs:16` propagates the token to every derived context. These four check sites are **retargeted** to `Guard()` (§7.6), not added.

---

## 6. Independent Crawl — Surfaces the Operator's List Did Not Cover

I walked every looping, blocking, allocating and re-entrant surface in `Pooshit.Scripts/`. Three genuine finds, plus a set of surfaces confirmed clean.

### 6.1 F1 — Imported scripts execute with **no token at all** (in scope, must fix)

`Data/ExternalScriptMethod.cs:21` calls `script.Execute(new VariableProvider(new Variable("arguments", arguments)))` — the parameterless-token `Execute` overload, which builds the 2-arg `ScriptContext` → `CancellationToken.None`.

**Consequence:** `$m = import("x"); $m()` where `x` contains `while(true)` is **completely uncancellable**, even from `ExecuteAsync`. This is a strictly worse case than G7, because the host did nothing wrong — it used the async API and still got an uncancellable region. Uberkarl's community-script model, where a shared library script is imported by many level scripts, makes this the highest-risk find of the whole crawl.

**Fix:** §7.8.

### 6.2 F2 — `Using`'s `finally` can swallow cancellation (in scope, must fix)

`Control/Using.cs:35-48`: the `finally` block collects dispose failures and, if any occurred, `throw`s a new `ScriptRuntimeException`. A `throw` from a `finally` **replaces** the in-flight exception. So if the body is cancelled *and* any `Dispose()` then fails, the `OperationCanceledException` is destroyed and the host sees an ordinary runtime error. Rare, but it is precisely the "cancellation silently downgraded" class that G2 belongs to, and it defeats the same watchdog.

**Fix:** §7.9.

### 6.3 F3 — Compiled path has no cancellation at all (consciously **out of scope**)

`Expressions/ExpressionBuilder.cs:351,359,371,407` emit raw `Expression.Loop` constructs with **no** cancellation check, and line 344 emits `Expression.Catch(typeof(Exception))` — the compiled equivalent of G2.

**Out of scope, with reason.** The compiled path has no `ScriptContext` plumbing whatsoever: `ParseDelegate<T>` produces a delegate whose signature is entirely host-defined (`LambdaParameter[]`), so there is no parameter through which a token could arrive. Making it cancellable means adding a synthetic token parameter to every generated delegate and changing the `ParseDelegate` contract — a separate design with its own compatibility story. Per #2946 §13 the compiled path already omits `wait`, `await`, lambdas, `import` and regex, so five of the eight gaps do not exist there; what remains is unbounded loops.

**Required deliverable instead:** a documentation change (§13, phase 6) stating plainly that `ParseDelegate` output is **not** cancellable and must not be used for untrusted code. Uberkarl must use the interpreter path. This is recorded as OQ-3.

### 6.4 F4 — Detached tasks survive cancellation (host contract, not a fix)

`Hosts/TaskHost.cs:19` — `task.run([] => {…})` returns a `Task` the engine does not track. If the script is cancelled without awaiting it, the lambda keeps running. With §7.5's `LambdaMethod.Invoke` checkpoint the lambda body *does* observe the token and unwinds on its own, so this is bounded rather than leaked — but the engine cannot join it. Documented in the host contract (§10), no code change.

### 6.5 Surfaces confirmed clean — no change needed

| Surface | Finding |
|---|---|
| `Control/Switch.cs:68`, `Control/Case.cs:39` | Bounded by the number of parsed cases/conditions. Case bodies run through `StatementBlock`, which checks. |
| `Control/Internal/ListStatementBlock.cs:15` | `Execute` throws `NotImplementedException`; `Switch` dispatches to cases directly and never executes this block. Dead for execution purposes. |
| `Control/Import.cs:32` | The `import(...)` *resolution* call is a single host call (`IImportProvider.Import`) — same category as any host method, covered by the §10 contract. The *executed body* is F1. |
| `Tokens/ScriptArray.cs`, `DictionaryToken`, `StringInterpolation` | Bounded by the literal's parsed element count. |
| `Operations/Values/*`, `Operations/Unary/*` | Single `dynamic` operations, no iteration. Unbounded *allocation* is possible (`$s + $s` in a loop) — memory residual, §10. |
| `Extern/Converter`, `Parser/Resolvers/MethodResolver` | Bounded work per call. `Converter` has a separate thread-safety defect (#2909), unrelated. |
| `Visitors/*`, `Formatters/*` | Parse/analysis-time only, never on the execution path. |
| `Foreach.cs:47` uses `context` rather than `loopcontext` | Same token, same guard reference — cosmetic. Normalised to `loopcontext.Guard()` while the line is being touched anyway. |

---

## 7. Components & Responsibilities — the Fixes

### 7.1 `Wait` — cancellable sleep (G1)

**Responsibility:** convert the argument to a duration, then block *interruptibly* for that duration.

**Design.** The four `Thread.Sleep` call sites collapse to one. The token conversion logic runs first and produces a single `TimeSpan`; a single wait call follows. The wait blocks on the context token's wait handle with that timeout: if the handle signals, the token was cancelled and the cancellation exception is thrown; if the timeout elapses, the wait completed normally.

- `CancellationToken.None` exposes a never-signalled handle, so the no-token case degrades to a plain sleep — identical observable behaviour to today for hosts that never cancel.
- Durations exceeding the platform's single-wait maximum must be handled (chunked into repeated bounded waits, or clamped) — the implementer must not let a large `wait` argument throw where it previously slept.
- **This does not free the thread.** On a synchronous interpreter the wait must block *something*; the fix makes it interruptible, not non-blocking. Freeing the pooled thread is the async-rewrite follow-up (§11).

**KISS note (merge check, #1136 §4):** 4 `Thread.Sleep` sites → 1 wait site. This is a deletion, not an addition.

### 7.2 `Try` — cancellation is uncatchable (G2)

**Responsibility:** catch script-level errors; never catch an engine abort.

**Design.** `Try` gains exactly two guard clauses ahead of its existing `catch(Exception)`:

1. Rethrow an `OperationCanceledException` **when the context's token is cancellation-requested**.
2. Rethrow a step-limit exception unconditionally.

**Why the token-state predicate rather than blanket rethrow.** The user's rule is that cancellation "originating from the context token" must not be swallowable. A blanket `catch(OperationCanceledException) { throw; }` would over-reach: `Await` unwraps an `AggregateException` and rethrows the inner exception, so a *host* task that was cancelled by the host's own unrelated token surfaces as `TaskCanceledException` inside the script — legitimate, ordinary, catchable script control flow today, and used that way by trusted hosts. Testing the **context token's state** rather than the exception's own token is one condition, needs no token-identity comparison, and behaves correctly with linked tokens (which the timeout guard creates). If the engine's token is cancelled we are unwinding and nothing may intercept; if it is not, an `OperationCanceledException` is an ordinary error.

**No new marker base type.** Two explicit catch clauses are cheaper than an abstract `ScriptAbortException` hierarchy that exists only to be caught once (#1136 §4 — can-it-be-deleted).

The step-limit exception derives from `ScriptException`, so `StatementBlock.cs:42,62` and `ScriptToken.cs:23` already rethrow it unwrapped — no change needed there.

> **Superseded on this point, 2026-08-05.** `docs/architecture/execution-guards-depth-memory.md` §9.2 reintroduced exactly this marker when a third abort type (`ScriptDepthLimitExceededException`) arrived: at three abort types the enumeration math flips from "two catch clauses are cheaper" to "36 lines of duplicated enumeration across four catch-chain sites, above the extraction threshold" (§9.2's own `3 × 4 × 3` accounting). `ScriptStepLimitExceededException` and `ScriptTimeoutException` now both derive from the new abstract `Errors.ScriptAbortException : ScriptException`, not from `ScriptException` directly. The rejection above was correct for two types; it stopped being correct once a third abort type existed. No text elsewhere in this document is rewritten — this note is the pointer.

### 7.3 `Await` — token-aware wait (G3)

**Design.** Wait on the task with the context token. Semantics on cancel: the script unwinds; the awaited task keeps running because the **host** owns it — the engine never created it and must not attempt to cancel it. The existing `AggregateException` unwrap remains for genuine task faults; a token-driven cancellation surfaces as `OperationCanceledException` directly (not wrapped in `AggregateException`), so it flows past the unwrap block untouched and is rethrown by `ScriptToken.Execute`. The implementer must verify that ordering, not assume it.

### 7.4 `TaskHost.WaitAll` — token-aware wait (G4)

**Design.** `WaitAll` takes the execution context via the §7.5 injection seam and passes the token to the wait. Same cancel semantics as §7.3: the script unwinds, the tasks are the host's problem.

`TaskHost.Run` and `FromResult` are unchanged — neither blocks.

### 7.5 `ScriptContext` injection seam + `EnumerableExtensions` (G4, G5)

**The problem.** `Operations/MethodOperations.cs:228` (`CallMethod`) binds script arguments to host method parameters by position and type. There is **no** mechanism by which a host method or a registered extension can receive the execution context. `EnumerableExtensions` methods are `public static` (required by `ExtensionProvider.AddExtensions`), and `TaskHost` methods are plain instance methods — neither can reach a token. This is the structural blocker behind both G4 and G5.

**Design — parameter injection.** Method binding learns one rule: **a parameter of type `ScriptContext` is supplied by the engine and consumes no script argument.** Three touch points in `MethodOperations`:

| Method | Change |
|---|---|
| `MatchesParameterCount` (line 182) | Exclude `ScriptContext` parameters from the required/maximum counts. |
| `GetMethodMatchValue` (line 34) | Skip `ScriptContext` parameters without advancing the source-argument index (the method already tracks `i` and `index` separately, so this is a `continue`). |
| `CreateParameters` (line 349) | When the target parameter is a `ScriptContext`, yield the current context and do not consume a source parameter. |

Constraint the implementer must honour: **`ScriptContext` may not be the first parameter of an extension method**, because `ExtensionProvider.AddExtensions` uses `parameters[0].ParameterType` as the extended host type. Place it last.

Known edge, documented not defended: a host that declares both `Foo(int)` and `Foo(int, ScriptContext)` creates an ambiguous match. This is an authoring error; document it, do not add tie-breaking logic (#1136 §6 — no defensive code for scenarios the surrounding rules already exclude).

**Why this is not YAGNI.** Three named present consumers, not hypothetical future ones: `TaskHost.WaitAll` (G4), the `EnumerableExtensions` iterators (G5), and Uberkarl's own host functions (#7407), which must be cancellable for the untrusted-script model to hold. Without the seam, the "no gap" guarantee is false for every surface the engine itself ships.

**`EnumerableExtensions` — the fix has two halves.**

*Half A — script-callback iteration.* `Providers/LambdaMethod.cs:31` (`Invoke`) gains a `Guard()` call. **One line, one site**, and it covers every path where the engine hands control back to script code: `Where`, `IndexOf(predicate)`, `LastIndexOf(predicate)`, `TaskHost.Run` bodies, and **every host-registered extension anyone ever writes that calls a script lambda**. This is the highest-leverage single line in the whole design.

> **Correction, 2026-08 (DiVoid #7744/#7749).** The claim above — that `Where`, `IndexOf(predicate)` and `LastIndexOf(predicate)` needed no changes of their own because `LambdaMethod.Invoke`'s one-line `Guard()` covers them — held for the cancellation/step-budget dimension this design shipped and does not hold for the recursion-depth dimension the depth-guard design (`execution-guards-depth-memory.md`) added later. `StepBudget`/cancellation are propagated uniformly through the context copy-constructor chain, so which specific context object a lambda body executes under doesn't matter for them. `DepthBudget` resolution turned out to depend on *which* context invoked the lambda (captured-at-definition vs. invoking-at-call-time, DiVoid #7749's CF-1/CF-5 finding) — a distinction this design never needed to draw. All three of these methods ended up gaining their own `ScriptContext` parameter and an explicit invoking-context call (`LambdaMethod.InvokeFrom`) after all — see `execution-guards-depth-memory.md` §12 B20. The one-line, one-site claim was correct for what this design was solving; it is not a general property of `LambdaMethod.Invoke`.

*Half B — pure host iteration.* The methods that iterate without invoking script code — `Count`, `Order`, `OrderDesc`, `Min`, `Max`, `ToArray`, `Last`, `LastOrDefault`, `IndexOf(value)`, `LastIndexOf(value)` — take a trailing `ScriptContext` and call `Guard()` per element. `First`, `FirstOrDefault` consume at most one element and are left alone (#1136 §4 — can-it-be-deleted: the check would never fire).

Half B forces the eagerly-materialising methods to be rewritten from LINQ one-liners into explicit loops (or to enumerate through a guarded wrapper). Prefer a single internal guarded-enumeration helper over ten hand-rolled loops — **DRY math (#1267):** a hand-rolled guarded loop is ~6 lines × 10 sites = 60 lines, well above the ~15–20 threshold; the helper is nameable in one word (`Guarded`), so the extraction earns its keep.

### 7.6 `ScriptContext.Guard()` — the single checkpoint primitive

**Responsibility:** the one place that decides whether execution may continue.

`Guard()` performs the token check, then — only when a step budget exists — consumes one step and throws the step-limit exception if the budget is exhausted.

**State ownership.** `ScriptContext` is created fresh per scope (`ScriptContext.cs:15` derives a child for every block, loop, lambda and catch). A step counter must therefore live in a **separate small object held by reference** and propagated by the derived constructor alongside the token. That object (`StepBudget`) holds the limit and the running count and is `null` when no limit is configured — so the no-limits hot path is one null check.

`ScriptContext` gains three propagated members: the existing token, a `Limits` reference (never null; a shared "no limits" instance), and the nullable `StepBudget`.

**Concurrency.** `task.run` lambdas share the parent context and therefore the budget across threads. Use an interlocked increment. Cost argument (#1136 §4, explicit trade-off): an uncontended interlocked increment is single-digit nanoseconds; one interpreter step is a virtual dispatch plus dictionary lookups plus boxing — three or more orders of magnitude more. The correctness of the guarantee is worth noise-level cost. A plain increment would race to an undercount and make the limit non-deterministic, which is worse than the atomic on a safety mechanism.

**DRY math for the primitive itself (#1267).** The inline alternative at each checkpoint is: token check, budget null check, increment, compare, throw ≈ 5 lines. Checkpoint sites after this design: `StatementBlock`, `While`, `For`, `Foreach`, `LambdaMethod.Invoke`, plus ~10 `EnumerableExtensions` iterators = **16 sites**. `5 × 16 = 80` lines inlined versus one nameable method (`Guard`, one word) called 16 times. Far above the ~15–20 threshold — extract. This also *retires* the existing 4-site duplication rather than adding to it.

### 7.7 Cancellation on the sync path (G7)

`IScript` gains two overloads:

- `object Execute(IVariableProvider variables, CancellationToken cancellationToken)`
- `T Execute<T>(IVariableProvider variables, CancellationToken cancellationToken)`

**Named consumer:** Uberkarl runs behaviour scripts on the frame loop and does not want a `Task` allocation per tick; a watchdog thread cancels the token and the frame thread unwinds. Without a sync token overload that host has no cancellation at all.

The `<T>` variant is included for symmetry with the four existing `Execute`/`ExecuteAsync` shapes and costs a two-line delegation through the existing `ConvertResult<T>`; the asymmetry would cost more in confusion than the two lines cost in surface.

**Note on the timeout guard:** hosts that only want a deadline (not caller-driven cancellation) get it on the sync path *without* these overloads, because `Limits.Timeout` is applied inside `Execute` regardless of which overload was used.

**Compatibility:** additive to a public interface — a compile break for any external `IScript` implementer. Per A2 there are none in-repo; per A3 the library is pre-1.0. Called out in §9.3.

### 7.8 Imported scripts inherit the token (F1)

**Design.** `IExternalMethod.Invoke` changes its first parameter from `IVariableProvider parentvariables` to `ScriptContext context`. No information is lost — `context.Arguments` *is* the parent variable provider. `ScriptMethod.cs:80` already holds the context and passes it. `ExternalScriptMethod` then executes the imported script through the new `Execute(IVariableProvider, CancellationToken)` overload from §7.7, handing down `context.CancellationToken`.

**What propagates and what does not — state it precisely, do not overclaim:**

| Property | Propagates into an imported script? | Why |
|---|---|---|
| Caller cancellation | **Yes** | Carried by the token. |
| Execution timeout | **Yes** | The deadline is realised *as* a linked token (§8.1), so it is the same token. |
| Step budget | **No** — the imported script starts a fresh budget from its own parser's limits | The counter lives in the parent's `StepBudget`, which is not threaded through the `IScript` boundary. |

The step-budget residual is **bounded, not unbounded**: an import called inside a loop still ticks the *caller's* budget once per call, so total work is bounded by `caller_budget × per_import_budget`. An imported `while(true)` is still stopped by cancellation and by the timeout. Passing the parent budget across would require a third `IScript` addition exposing `ScriptContext` on the public interface; the cost is not worth closing a residual that the timeout already covers (#1136 §4 — can-it-be-deleted). Documented, not hidden.

**Compatibility:** breaking for external `IExternalMethod` implementers. Per A2/A5 none are known. `FileMethodProvider`, `ResourceScriptMethodProvider` and the `ScriptExecutor` CLI's `ImportProvider` implement `IImportProvider`, which is **unchanged** — they return objects. **Stale as of 2026-08 (DiVoid #7744/#7749; see the A2 correction above):** the last clause ("only `ExternalScriptMethod` implements `IExternalMethod`") is no longer true — `Providers/LambdaMethod` implements it too, added additively (not a break) as part of the depth-guard's CF-1/CF-5 residual fix.

### 7.9 `Using` must not swallow the in-flight exception (F2)

**Design.** The dispose-failure report must never replace an exception that is already unwinding. `Using` tracks whether the body completed normally; the aggregated dispose error is thrown **only** when no exception is in flight. When the body threw, dispose failures are still attempted for every resource and are attached to the original exception (as inner/context data) or dropped — the implementer picks the cheaper of the two, but the original exception **must** be the one that reaches the host.

This is a correctness fix for all exception types, not only cancellation; it happens to be required for the cancellation guarantee.

---

## 8. The Three Guards

`ScriptLimits` — one immutable options object, three nullable knobs, all `null` by default.

| Knob | Type | Default | Effect when null |
|---|---|---|---|
| `Timeout` | `TimeSpan?` | `null` | No engine deadline (today's behaviour) |
| `MaxSteps` | `long?` | `null` | No step budget; `StepBudget` is not allocated |
| `RegexTimeout` | `TimeSpan?` | `null` | Infinite match timeout (today's behaviour, byte-identical) |

**No default values, so no magic numbers** (#1136 §3). The engine ships no opinion about what "too long" means; the host decides or gets today's behaviour.

### 8.1 Configuration surface — one shape, justified

**Decision: `ScriptParser.Limits`, captured by the `Script` at parse time.**

Considered and rejected:

| Alternative | Rejected because |
|---|---|
| Parameters on `ExecuteAsync` | Multiplies four existing overloads into eight or more, on a *public interface*. Largest surface cost of the three. |
| A `ScriptLimits` parameter on `ScriptContext`'s public constructor as the host entry point | Hosts do not construct contexts; they have no `ITypeProvider`. Not a host-facing surface. |
| A per-execution `ScriptLimits` argument | Would need to reach both sync and async paths → same overload explosion. |

**Parser-level wins because it reaches every entry point — sync, async, typed, untyped — with zero additions to `IScript` for the guards.** It matches the established idiom: `ControlTokensEnabled`, `TypeInstanceProvidersEnabled`, `TypeCastsEnabled`, `ImportsEnabled` are all `ScriptParser` properties (#2946 §12), and deliberately **not** on `IScriptParser`. `Limits` follows exactly that precedent, so `IScriptParser` is untouched.

**Trade-off, stated (#1136 §4).** A host wanting two limit profiles (Uberkarl: a tight budget for per-frame behaviour scripts, a loose one for level-init scripts) must keep two parser instances. Cost: one extra parser object plus duplicate type registration at startup. Probability: high for Uberkarl, zero for Mamgo. Compared against the present cost of the alternative — eight-plus overloads on a public interface, forever — two parser instances is the cheaper side by a wide margin. And a host that wants a *per-execution* deadline already has one without any engine support: `CancellationTokenSource.CancelAfter` on the token it passes.

**Configurability gate (#1136 §3).** Each knob passes on "the value genuinely differs across hosts by design", not "we might tune it later": trusted embedding (Mamgo `scriptservice`, `ScriptExecutor` CLI) leaves all three null; untrusted embedding (Uberkarl, #7407) sets all three. Two named hosts, opposite settings, today. No telemetry-then-tune compound: there is no audit column, no per-execution record, nothing measuring the values.

### 8.2 Execution timeout — realised as a token, not as a new mechanism

When `Limits.Timeout` is set, `Script` creates a `CancellationTokenSource` linked to the caller's token, arms it with the deadline, and puts the **linked** token in the `ScriptContext`. That is the entire mechanism.

Consequences, all of them free:
- Every checkpoint this design establishes already observes it. Zero new checks.
- It works on the **sync** path, closing half of G7 for hosts that never adopt the new overloads.
- It composes with a caller token: whichever fires first wins.
- The linked source must be disposed on every exit path.

**Distinguishing timeout from caller-cancel.** Mid-flight both are an `OperationCanceledException` on the linked token, which is correct — nothing should treat them differently while unwinding. At the `Script` boundary, exactly one place converts: if the *caller's* token is cancellation-requested, the exception is rethrown as-is; otherwise the deadline fired alone and a `ScriptTimeoutException` is thrown.

### 8.3 Regex match timeout

`Matches.cs:23` and `MatchesNot.cs:23` pass `Limits.RegexTimeout` to the match call; when null they pass the infinite-timeout value, making the default path byte-identical to today. Both tokens reach `Limits` through the `ScriptContext` they already receive (`Comparator.Compare(lhs, rhs, context)`).

**Why this guard cannot be replaced by cancellation.** Catastrophic backtracking runs inside a single `Regex` call. No token, no step counter and no watchdog thread can interrupt it. The match timeout is the *only* in-process mechanism, and it is the strongest argument for the guards existing at all.

On timeout the regex engine raises `RegexMatchTimeoutException`, which `Comparator.ExecuteToken` does not catch (it only catches `RuntimeBinderException`), so it propagates to `ScriptToken.Execute` and is wrapped as a `ScriptRuntimeException` carrying the offending `~~` token and its source position — exactly the diagnosis a script author needs. **No new exception type; the existing error path already does the right thing.**

### 8.4 Step limit

A "step" is deliberately **not** "a token executed" — it is an **engine checkpoint**: one statement in a block, one loop iteration, one lambda invocation, one enumerated element. The name `MaxSteps` reflects that; do not document it as an instruction count, because it is not one.

Overrun throws `ScriptStepLimitExceededException` (deriving from `ScriptException`), carrying the configured limit in its message. It carries **no source position**: the throwing site is a checkpoint, not a semantic error, and `ScriptContext` has no token reference. A host that needs to know *which* script overran already knows — it chose the script it executed.

**Honest limitation, stated up front:** the step limit does not bound a single expensive host call, a single huge allocation, or a runaway regex. It bounds *script-driven* looping. It is a complement to the timeout, not a substitute — and where the two disagree, the timeout is the one that actually protects the host.

---

## 9. Contracts

### 9.1 The exception contract a host sees

| # | Scenario | `await ExecuteAsync(ct)` throws | Task state | Sync `Execute` throws |
|---|---|---|---|---|
| a | Caller cancels `ct` | `OperationCanceledException` / `TaskCanceledException`, `.CancellationToken == ct` | **Canceled** | same (token overload only) |
| b | `Limits.Timeout` elapsed | `ScriptTimeoutException` (message names the budget) | **Faulted** | `ScriptTimeoutException` |
| c | `Limits.MaxSteps` exceeded | `ScriptStepLimitExceededException` | **Faulted** | same |
| d | `Limits.RegexTimeout` elapsed | `ScriptRuntimeException` wrapping `RegexMatchTimeoutException`, `.Token` = the `~~`/`!~` operation | **Faulted** | same |

**All four are distinguishable. Why the split between Canceled and Faulted:** (a) is *the host asked to stop* — `Canceled` is the .NET-correct outcome and the one a watchdog's `try/catch (OperationCanceledException)` already expects. (b) and (c) are *the script misbehaved* — a Faulted task with a typed exception is what a host wants to log, alert on, and attribute to a specific community script. Collapsing (b) and (c) into `Canceled` would make "the level author wrote an infinite loop" indistinguishable from "we shut the level down", which is exactly the signal Uberkarl needs.

(d) is a *script-authoring* error, reported through the ordinary runtime-error channel with source position, like any other bad expression.

### 9.2 Component interaction summary

| Producer | Consumer | Carries |
|---|---|---|
| `ScriptParser.Limits` | `Script` (captured at `Parse`) | The three knobs |
| `Script.RunGuarded` | `ScriptContext` | Effective (possibly linked) token, `Limits`, `StepBudget?` |
| `ScriptContext` (derived ctor) | child `ScriptContext` | All three, by reference |
| `ScriptContext.Guard()` | every checkpoint | Continue, or throw |
| `ScriptContext` | `Matches`/`MatchesNot` | `Limits.RegexTimeout` |
| `ScriptContext` | injected host method parameters | The whole context |
| `ScriptMethod` | `IExternalMethod.Invoke` | The whole context |

### 9.3 Backward compatibility — every observable change

| # | Change | Breaking? | Who is affected |
|---|---|---|---|
| B1 | **`try/catch` no longer swallows engine cancellation** | **YES — behaviour, intentional, user-approved** | Any script wrapping cancellable work in `try/catch`. Previously `ExecuteAsync` could complete *successfully* after a cancel; now it does not. This is the hole that defeats a watchdog, so the break is the point. |
| B2 | `IExternalMethod.Invoke` parameter changes to `ScriptContext` | YES — compile, external implementers only | None known (A2, A5) |
| B3 | `IScript` gains two sync overloads | YES — compile, external implementers only | None known (A2) |
| B4 | `EnumerableExtensions` iterating methods gain a trailing `ScriptContext` | YES — compile, direct C# callers only | Script-facing surface; direct C# use unlikely. Script-level calls are unchanged: the parameter is engine-supplied and invisible to script code. |
| B5 | `TaskHost.WaitAll` gains a trailing `ScriptContext` | YES — compile, direct C# callers only | Same as B4 |
| B6 | `wait` becomes interruptible | Behaviour, safe direction | A cancelling host now gets what it asked for |
| B7 | `Using` no longer replaces an in-flight exception with a dispose-failure report | Behaviour, safe direction | A host previously seeing the dispose error now sees the real cause |
| B8 | Guards (timeout / steps / regex) | **No** — all default null | Mamgo `scriptservice` and `ScriptExecutor` CLI are unaffected |
| B9 | Both `MethodOperations.CreateParameters` overloads (`MethodOperations.cs:311`, `:413`) gain a trailing optional `ScriptContext context = null` | YES — **binary**-breaking, source-compatible; direct C# callers only | Public static members on a public class. No known external caller; in-repo callers are source-compatible via the default. Surfaced by QA review #7717 (W-5), not by the original A2 sweep. |

**Existing hosts audited.** `ScriptExecutor/Program.cs:48` uses `script.Execute(IDictionary)` with no limits — unaffected. Mamgo's `scriptservice` per #2946/#548 uses the parser without an `ImportProvider` and does not implement `IScript` or `IExternalMethod` — unaffected except by B1. **B1 must be called out in the release notes and in #2946.**

---

## 10. Host Contract — what the engine does **not** guarantee

This section is a deliverable, not a disclaimer. It belongs in `docs/pooscript-language-reference.md` §12 and in #2946.

> **The engine guarantees an interruption checkpoint at every point where control returns to the engine. It guarantees nothing inside a single host call.**

Concretely, the engine **cannot** interrupt:

1. **A host-supplied method that blocks** — synchronous IO, a lock, a `Thread.Sleep` in host code. .NET Core has no thread abort. Hosts binding objects into an untrusted script must ensure every reachable method returns promptly, or accept the wedge. *(This is the residual the user explicitly asked to be honest about.)*
2. **Enumeration of a host-supplied infinite or unbounded-latency sequence by a non-callback method.** The `Guard()` calls added in §7.5 fire per element, so an infinite *cheap* sequence is bounded — but a sequence whose single `MoveNext` blocks is case 1.
3. **Catastrophic regex backtracking** — bounded only by `Limits.RegexTimeout`, which is why that guard exists.
4. **Unbounded memory allocation** — `$s = $s + $s` in a loop, or a huge single allocation. Each *iteration* ticks the guard, but a single allocation that exhausts the heap does not return control first.
5. **Stack overflow** from unbounded recursion (recursive lambdas, mutually recursive imports). `StackOverflowException` cannot be caught and terminates the process.
6. **A detached `task.run(...)`** the script never awaits (F4). Its body observes the token and unwinds on its own, but the engine cannot join it.

**For genuinely untrusted code, cases 4–5 mean in-process guards are a mitigation, not a boundary.** Process or container isolation is the only complete answer. Uberkarl should treat the guards as the fast path and process isolation as the eventual containment story — recorded as OQ-2.

---

## 11. Verdict: True Async State Machine — **SPLIT**

**Recommendation: split to a separate task. Do not attempt it here.**

### Cost

Turning the interpreter into a true async state machine means changing `IScriptToken.Execute(ScriptContext)` into an async-returning method. That is:

- **A public interface implemented by roughly 90 token classes** (`Pooshit.Scripts/Tokens/`, `Control/`, `Operations/`), each needing rewriting.
- `ScriptToken` base, `StatementBlock`, `ControlToken`, `Comparator`, `ValueOperation`, `UnaryOperator`, `AssignableToken` — the entire dispatch spine.
- `MethodOperations.CallMethod`, which invokes host methods by reflection. Host methods are synchronous `MethodInfo` handles; the async chain **terminates at the first host call regardless**.
- `LambdaMethod`, which is handed to host code as a synchronous callback (LINQ predicates in `EnumerableExtensions.Where`, `TaskHost.Run`). You cannot await inside a synchronous delegate the host invokes — every lambda boundary needs a sync bridge, reintroducing exactly the blocking this rewrite is meant to remove.
- `Expressions/ExpressionBuilder.cs` (the compiled path), all visitors, all formatters, and the full test suite.

### Why it must not ride in this PR

**It does not improve cancellation.** After this design, the synchronous interpreter observes the token at every engine-controlled point — checkpoints, waits, awaits, callbacks, enumerations, imports. Async-ifying the tree walk changes **thread economy**, not interruptibility: a `wait(60000)` or `await(task)` would release the pooled thread instead of blocking it. That is a scalability improvement, and it should not be bundled with a safety change (#1136 / PR-scope discipline: one feature, one PR). Reviewing a 90-class mechanical rewrite tangled with a behaviour break (`try/catch`) is exactly the tangled review the PR-scope rule exists to prevent.

### Minimal goal — how it is met **without** the rewrite

The user's floor: *"non leaking but also non blocking behavior - cancel has to cleanly stop the script."*

`ExecuteAsync` stays `Task.Run` over the synchronous walk, and that is now sufficient, because:

- **Non-blocking for the caller.** With complete checkpoint coverage the worker throws `OperationCanceledException` promptly on cancel, the `Task` transitions to `Canceled`, and the caller's `await` returns. The bound is one checkpoint interval — a statement, an iteration, or a now-interruptible `wait`.
- **Non-leaking.** The worker actually unwinds; `Using` blocks dispose; the thread returns to the pool. Test T14 proves the unwind observably rather than assuming it.

**Explicitly rejected: `Task.WhenAny(worker, cancellationTask)`.** It would unblock the caller even when the worker is wedged — but it converts a detectable hang into a *silent abandoned thread*, which violates non-leaking outright. Given the only wedge left is a host-supplied blocking call (§10 case 1), abandonment would hide a host bug rather than fix it. Failing loudly beats leaking quietly.

`ExecuteAsync` does become a genuine `async` method here, but only so the linked timeout source can be disposed in a `finally` — not as a step toward the rewrite.

### Follow-up task spec (for the operator to file)

> **Title:** Pooscript: convert the interpreter to a true async execution path (`IScriptToken.ExecuteAsync`)
>
> **Scope.** Introduce an async execution path through the token tree so `wait`, `await` and host-async methods release the calling thread instead of blocking it. Covers: async-returning `IScriptToken` dispatch across all token classes; `Wait` on a delay rather than a wait handle; `Await` on the task rather than a blocking wait; `TaskHost.WaitAll` on the awaitable; a defined sync bridge for host-invoked lambdas; `ExecuteAsync` no longer wrapping in `Task.Run`; the compiled-path story.
>
> **Explicitly out of scope.** Cancellation semantics, the guards, and the exception contract — all established by this design and must be preserved unchanged.
>
> **Why separable.** It is a thread-economy change, not a safety change. Safety is complete without it; it is a mechanical rewrite across ~90 classes touching a public interface; and it has a genuine open design question (the sync-lambda bridge) that deserves its own design pass.
>
> **Depends on:** this task (#7409). **Links:** `depends-on` → #7409; `implements` → #7407 (scalability half).

---

## 12. Test Plan — the implementer must hit all of these

Every cancellation test carries a hard time bound (`MaxTime`) **at least 20× shorter than the script's nominal runtime**, so that a passing test is evidence of interruption rather than of a short sleep. The existing `ScriptExecutionTests.CancelAsyncScript` (`while(true) wait(100)`, `MaxTime(10000)`) does **not** meet that bar — it passes today purely because the sleep granularity is 100 ms. Keep it, but it proves nothing; the tests below replace it as the real gate.

| # | Scenario | Script / setup | Assertion | Proves |
|---|---|---|---|---|
| T1 | Tight loop | `while(true) { }` | cancel at 200 ms; task `Canceled` within 2 s | Baseline |
| T2 | **Long** blocking wait | `while(true) { wait(60000) }` | cancel at 200 ms; `Canceled` within 2 s | **G1** |
| T3 | try/catch around a tight loop | `try { while(true) { } } catch($e) { }` | cancel; task is `Canceled`, **not** `RanToCompletion` | **G2** |
| T4 | try/catch around a long wait | `try { wait(60000) } catch($e) { }` | as T3 | G1+G2 |
| T5 | await a never-completing task | host binds a `Task` from an unset `TaskCompletionSource`; `await($never)` | cancel; `Canceled` within 2 s | **G3** |
| T6 | `waitall` on a never-completing task | `task.waitall([$never])` | cancel; `Canceled` within 2 s | **G4** |
| T7a | Enumerable + script lambda | host binds a lazy infinite sequence; `$src.where($x => $x > 0).count()` | cancel; `Canceled` within 2 s | **G5 half A** |
| T7b | Enumerable, no lambda | `$src.count()` over the same source | cancel; `Canceled` within 2 s | **G5 half B** + injection seam |
| T8 | Catastrophic regex | `Limits.RegexTimeout = 100 ms`; `"aaaaaaaaaaaaaaaaaaaaaaaaaaaa!" ~~ "^(a+)+$"` | `ScriptRuntimeException` with inner `RegexMatchTimeoutException` within 2 s | **G6** |
| T9 | **Sync** path timeout | `Limits.Timeout = 200 ms`; `while(true) { }`; call `Execute()` (no token) | `ScriptTimeoutException` within 2 s | **G7 + G8** |
| T10 | Step limit | `Limits.MaxSteps = 10000`; `while(true) { }` | `ScriptStepLimitExceededException` | G8 |
| T11 | Imported script | import a script containing `while(true) { }`; invoke it | cancel; `Canceled` within 2 s | **F1** |
| T12 | `using` + cancel + failing dispose | host disposable whose `Dispose` throws; `using($d) { while(true) { } }` | the host sees `OperationCanceledException`, **not** the dispose `ScriptRuntimeException` | **F2** |
| T13a | **Regression — no limits** | default parser; a finite loop of ~1e6 iterations | completes normally, no exception | S6 |
| T13b | **Regression — regex default** | default parser; an ordinary `~~` match | matches as before | S6 |
| T13c | **Regression — under the limit** | `Limits.MaxSteps = 1e6`; a 100-iteration loop | completes normally | S6 |
| T14 | **Non-leak proof** | `using($host.resource()) { while(true) { wait(60000) } }`, host records `Dispose` | after cancel, `Dispose` was observed within the bound → the worker genuinely unwound | **S5** |
| T15 | Timeout vs cancel are distinguishable | (a) caller cancels; (b) `Limits.Timeout` fires with an uncancelled caller token | (a) `OperationCanceledException`, task `Canceled`; (b) `ScriptTimeoutException`, task `Faulted` | §9.1 |

**Test hygiene.** T7a/T7b need a host-supplied lazy infinite sequence — a generator yielding indefinitely, bound as a variable, with `parser.Extensions.AddExtensions<EnumerableExtensions>()` (A4: not registered by default). T5/T6 need an unset `TaskCompletionSource` so the task genuinely never completes. Prefer deterministic waits over sleeps in assertions where possible.

---

## 13. Implementation Guidance — ordered phases

Each phase is independently buildable and testable. **All of this is one PR** — it is one feature (host-interruptible scripts) with one coherent behaviour contract; splitting the checkpoint primitive from the sites that call it would produce a PR that compiles but changes nothing.

| Phase | Work | Gates cleared |
|---|---|---|
| **1 — Primitive** | `ScriptLimits` (three nullable knobs + a shared no-limits instance); `StepBudget` (internal, interlocked); `ScriptContext` gains `Limits`, the budget reference and `Guard()`; derived ctor propagates all three. `ScriptStepLimitExceededException`, `ScriptTimeoutException` (both `: ScriptException` — re-parented to `: ScriptAbortException : ScriptException`, see the §7.2 superseded-note above). | — |
| **2 — Retarget existing checkpoints** | Replace the four existing `ThrowIfCancellationRequested()` calls (`While:28`, `For:36`, `Foreach:47`, `StatementBlock:54`) with `Guard()`. Normalise `Foreach` to `loopcontext`. | Full suite still green — pure refactor |
| **3 — Boundary** | `ScriptParser.Limits`; `Script` captures it at `Parse`; a single private `RunGuarded` helper builds the linked source + budget + context, runs the body, converts a deadline-only cancel into `ScriptTimeoutException`, and disposes the source. All `Execute`/`ExecuteAsync` overloads route through it; `ExecuteAsync` becomes `async` for the `finally`. Add the two sync token overloads to `IScript`. **DRY:** the boundary block is ~14 lines and would otherwise appear on both the sync and async paths (14 × 2 = 28, above threshold) — extract. | T9, T10, T13, T15 |
| **4 — Blocking sites** | `Wait` (four sleeps → one interruptible wait, with the large-duration case handled); `Await`; `Try` (two guard clauses); `Using` (finally must not replace an in-flight exception). | T1–T5, T12 |
| **5 — Injection seam & callbacks** | The three `MethodOperations` changes; `LambdaMethod.Invoke` gains `Guard()`; `TaskHost.WaitAll` and the ~10 `EnumerableExtensions` iterators take a trailing `ScriptContext` (via one guarded-enumeration helper, not ten loops); `IExternalMethod.Invoke` takes the context and `ExternalScriptMethod` passes the token down. | T6, T7, T11 |
| **6 — Docs** | `docs/pooscript-language-reference.md` §12 "Execution safety": rewrite. State the guards, the exception contract (§9.1), the host contract (§10) verbatim, the `try/catch` break (B1), and that `ParseDelegate` output is **not** cancellable (F3). Update the §1 quick-answer table row "built-in execution timeout ❌". | — |
| **7 — Tests** | All of §12. | S1–S6 |

**Implementer notes.**
- Phase 2 must be a behaviour-preserving refactor; if the suite goes red there, `Guard()` is wrong before any site depends on it.
- Verify A1 on **both** target frameworks — `netstandard2.0` is the constraining one.
- Do not add token overloads to `IScriptParser`; the §12 toggles precedent puts configuration on the concrete `ScriptParser` (§8.1).
- Do not touch `Foreach.cs:53-66` (defect #2896 territory, §2).

---

## 14. Pre-Design Checklist (#1136 §5) — answered in order

**KISS / DRY / YAGNI**
- *No new type mirroring an existing type's value-space.* Four new types: `ScriptLimits`, `StepBudget` (internal), and two exceptions. None mirrors an existing type; both exceptions extend the existing `ScriptException` hierarchy rather than paralleling it. The abstract `ScriptAbortException` marker was considered and **deleted** — two catch clauses in `Try` are cheaper than a hierarchy that exists to be caught once (§7.2). Superseded 2026-08-05: a third abort type made the marker worth its cost; see the §7.2 note.
- *No abstraction with one implementation and no second planned.* No new interfaces. The `ScriptContext` injection seam is a binding **rule**, not a type.
- *No element justified by "we might need X later".* The three guards each have two named hosts with opposite settings today (§8.1). The sync token overloads have a named consumer (Uberkarl's frame loop, §7.7). `Execute<T>(…, ct)` is justified by symmetry at a two-line cost, stated openly rather than hidden.
- *No deprecation period / feature flag / compatibility shim.* Breaking changes B1–B5 and B9 are made directly. No transition window, no shim overloads, no obsoletion cycle.
- *DRY math quoted for every extract/inline decision.* `Guard()`: `5 × 16 = 80` → extract (§7.6). Guarded enumeration helper: `6 × 10 = 60` → extract (§7.5). `RunGuarded`: `14 × 2 = 28` → extract (phase 3). Retiring the existing 4-site duplication is a **net deletion** at the checkpoint sites, not a new abstraction layered over them.

**Existing systems first**
- *Audited whether an existing surface covers the concern.* Yes, and it changed the design twice: the timeout is realised through the **existing** cancellation mechanism rather than a new deadline check (§8.2), and the regex timeout needs **no new exception type** because `ScriptToken.Execute`'s existing wrap already produces a positioned `ScriptRuntimeException` (§8.3).
- *New layers name their concrete reason.* The injection seam is the only structural addition, and its reason is concrete and verified: `MethodOperations.CallMethod` has no mechanism to pass context, so `TaskHost.WaitAll` and every `static` extension are structurally unable to see a token (§7.5).
- *New persisted data.* None. No storage, no audit trail, no telemetry.
- *Consumer chain recursed.* Every added surface has a named terminal consumer: `Limits` → Uberkarl's watchdog (#7407); sync overloads → Uberkarl's frame loop; injection seam → `TaskHost.WaitAll` + `EnumerableExtensions` + Uberkarl host functions. Nothing is written that nothing reads.

**Configurability**
- *Every knob has a named operator or environment difference.* Three knobs, two named hosts, opposite settings (§8.1). Not "for future tuning".
- *No telemetry-then-tune compound.* No knob is justified by "we'll tune from prod telemetry"; no audit column accompanies any of them.
- *Magic numbers stay magic.* **There are none.** All three knobs default to `null` = today's behaviour. The engine ships no opinion about what "too long" means.

**Less is better**
- *Delete / merge / inline check run on every element.* Deleted: the `ScriptAbortException` marker (§7.2); a `ScriptContext`-taking `IScript` overload for imports (§7.8); guard checks on `First`/`FirstOrDefault` (§7.5); `Task.WhenAny` abandonment (§11). Merged: four `Thread.Sleep` sites into one (§7.1); the cancel check and step tick into one primitive (§7.6); the sync and async boundary logic into one `RunGuarded` (phase 3).
- *Trade-offs named where a complex design wins.* §8.1 (parser-level vs per-execution limits, with the cost of two parser instances weighed against eight-plus public overloads); §7.6 (interlocked vs plain increment, with the cost argument); §11 (why `Task.Run` is kept); §7.8 (why the step budget does not cross the import boundary).
- *Radical-clean chosen where no consumer exists.* `IExternalMethod.Invoke` changes signature outright rather than gaining a parallel overload — no known consumer, so no compromise shape (B2).
- *Reader inventories cover AST and string-literal references.* N/A — no field or symbol is renamed; no string-literal field references exist in this engine.
- *Carrier-swap tables enumerate every affected surface.* §9.3 enumerates **all eight** observable changes, and §7.5 names **all ten** `EnumerableExtensions` methods that change signature plus the two that deliberately do not.

**Data deliverables** — N/A. No SQL, no migration, no backfill.

**Document discipline**
- Code Contracts (#114 §0) and Design Contracts (#1136) cited as load-bearing (header).
- Scope inventories explicit: §5 (all eight confirmed gaps), §6 (independent crawl, including the clean surfaces), §9.3 (all compatibility changes).
- Out-of-scope items listed explicitly, with reasons (§2).
- No multi-paragraph "rationale for keeping X" sections.
- Supersedes nothing; no predecessor doc needs a banner. This document **amends** #2946 §12/§14, which phase 6 updates in the same PR.

---

## 15. Risks & Mitigations

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| R1 | B1 (`try/catch` no longer swallows cancellation) breaks a live Mamgo script that relies on catching a cancel | Low — cancellation is barely used in trusted hosts today | Release-note it; update #2946 §12/§14; T3/T4 make the new behaviour explicit and testable |
| R2 | Injection seam perturbs overload resolution for an existing host method | Low — the changes are additive skips, and no shipped method has a `ScriptContext` parameter today | Full existing `MethodCallTests` / `MethodResolverTests` / `MethodParameterTests` suites must stay green as the gate for phase 5 |
| R3 | `Guard()` measurably slows the interpreter | Low — one null check plus one interlocked increment per checkpoint, against a virtual-dispatch tree walk | Budget is `null` when unconfigured, so the default path adds only a null check to what was already a token check |
| R4 | Interruptible `wait` mis-handles very large durations and throws where it used to sleep | Medium — a genuine platform edge | Explicitly called out in §7.1; add a test for a duration beyond the single-wait maximum |
| R5 | A host relies on `ParseDelegate` for untrusted code, believing it is cancellable | Medium if undocumented | Phase 6 documents it plainly (F3); OQ-3 asks whether Uberkarl needs it |
| R6 | Step-limit counting races under `task.run` produce a non-deterministic limit | Low | Interlocked increment (§7.6) |
| R7 | The linked `CancellationTokenSource` leaks when a script throws | Low | `RunGuarded` disposes in a `finally` on every path; `ExecuteAsync` becomes `async` specifically to allow it |

---

## 16. Open Questions

| # | Question | Blocking? | Default if unanswered |
|---|---|---|---|
| **OQ-1** | A5 is taken from #2946 rather than re-verified against the Mamgo repo. Does `scriptservice` (or any other host) implement `IExternalMethod`, `IScript`, or call `EnumerableExtensions` / `TaskHost.WaitAll` directly from C#? | **No** — B2/B4/B5 are compile breaks, caught at build time, not silent | Proceed; a compile break surfaces immediately |
| **OQ-2** | Untrusted community scripts can still kill the host process via stack overflow (unbounded recursion) or heap exhaustion. Neither is addressable by any guard in this design. Does Uberkarl want (a) a recursion-depth guard as a fourth knob, (b) process isolation, or (c) accept the risk for now? | No | (c) — accept and document (§10). A fourth knob has no named consumer until Uberkarl asks. |
| **OQ-3** | Does Uberkarl intend to use `ParseDelegate` (the compiled path) for community scripts? If yes, F3 is not optional and needs its own task. | No | Assume interpreter only; document that compiled output is not cancellable |
| **OQ-4** | Should the release ship as `0.19.0` given B1's intentional behaviour break, rather than a `0.18.x` patch? | No — packaging is the operator's call | `0.19.0` |

---

*Design by sarah-software-architect, 2026-08-05. Implements DiVoid #7409. Load-bearing contracts: Code Contracts #114 §0, Design Contracts #1136, DRY threshold #1267.*
