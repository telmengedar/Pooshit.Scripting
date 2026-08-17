# Architectural Document: Reflected-Method Guard (closes the reflected-dispatch OOM surface)

> **Repo working-tree path (canonical):** `docs/architecture/reflected-method-guard.md`.
> **DiVoid mirror:** node **#7872** (filed against task **#7856**).
> **Source task:** #7856 (post-1.0 closure of the Category-B transient-OOM class).
> **Load-bearing references:** Design Contracts **#1136** (§1 KISS/DRY/YAGNI; §5 Pre-Design Checklist — walked in order as §17 of this document), Code Contracts **#114 §0**, DRY threshold **#1267**, anti-seed-complexity **#1184**, principles-trump-design **#1333**, real-names rule **#6836**.
> **Precedent this design extends:** the M4 pre-allocation charge (`docs/architecture/execution-guards-depth-memory.md` §8.8, DiVoid #7736) and the type-access boundary (`docs/architecture/type-access-boundary.md`, DiVoid #7793). **Neither is superseded**; this document adds a fifth mechanism beside them.
> **Enumerations this design consumes:** Linus **round-7 (#7854)** and **round-8 (#7868)** — round-8's mechanical closure is folded in throughout and is recorded in §13. Operator ruling on the 1.0 accepted limitation: #7858. New bugs round-8 filed: **#7870** (in scope, closed here), **#7869** and **#7871** (**out of scope**, routed separately — §12).
> **Governance model revised 2026-08-08** by operator ruling (§0.2): a pure identity allow-list is replaced by a **white/blacklist hybrid** — identity allow-list for the known built-in surface, **signature/shape governance** for the host-injected surface. Superseded reasoning is marked **⟲** in place per #6760, not deleted.

---

## 0. The operator's ask — verbatim

### 0.1 The original ask (2026-08-08)

> "we want to think about the last known oom surface now. currently you gave me the idea that its some string methods and the general tostring which exists everywhere. first check whether you find some more - the string methods should be trivial to control - like intercept them and check for acceptable params. tostring actually more or less the same - intercept, plain tostring is allowed, with formatter arguments we see about patterns we can allow, when in doubt disallow it - **it doesn't exist for our script when we can not control it**."

The closing clause is the policy ruling and the spine of this design: **default-deny. If we cannot control it, it does not exist for our script.** It is not re-litigated below; only the *how* is designed.

The opening clause — *"first check whether you find some more"* — has been answered: **round-8 (#7868) found more.** Its findings are folded in as data, and every place they overturned an earlier decision of mine is marked **⟲ corrected by round-8** rather than quietly rewritten. Two of round-8's findings are **not** method-dispatch problems and this design deliberately does nothing for them (§12).

### 0.2 The governance ruling (2026-08-08, on the first draft of this document)

> "method allow list sounds like whitelisting - we can not whitelist every potential (external injected) - we can only whitelist signatures and patterns of known surfaces - **so a white/blacklist hybrid**."

**What this corrects.** The first draft governed *"the full 38-type reachable closure with no carve-out"* and generated a ~300-name identity inventory from it. That closure is real and closed — **for a bare `new ScriptParser()`**, which is the posture round-8 enumerated and the posture the draft designed against. It is **not the deployment posture**. The moment a host calls `Types.AddType<T>()` the reachable set gains an arbitrary, unbounded surface that no ahead-of-time inventory can enumerate, and every real consumer does exactly that (Uberkarl registers its game types; mamgo's `scriptservice` registers its own). An identity allow-list over a set that is closed only on a bare parser **silently stops being a closed set in production** — and a whitelist that cannot enumerate its domain is not a whitelist, it is a promise it cannot keep.

**The revised model, in one sentence:** *whitelist the surface the engine hands out (enumerable, so enumerate it); govern the surface the host injects by signature and shape (not enumerable, so never pretend to enumerate it); and express the known-bad specifics as projections that refuse exactly when unaffordable.* The full model is §7.6; the ruling's two direct consequences — the `IFormattable` generalization and the value-commanded ceiling — are §9.1 and §7.6.3.

**Why this revision is cheap rather than a restart:** the shape-over-enumeration argument is one this design already made and already had validated. §9.3 chose a **shape-based** format rule over an enumerated pattern list, and round-8 then threw three unenumerated cases at it (`P`, the 2-arg `null`-provider overload, the custom-format amplifier) which it absorbed **with no change at all**. The ruling asks for exactly that reasoning, generalized one level: *the argument that beat an enumerated format list beats an enumerated method list.* §7.6 is that generalization, and §9.1 gets **simpler** as a result — the hand-listed 11-type format family collapses into a single `IFormattable` predicate that reproduces round-8's measured set exactly.

---

## 1. Problem Statement

On a bare `new ScriptParser()` — `ScriptLimits.Default`, `MaxDepth = 10`, `MaxVariableBytes = 128 MiB` — a script can drive the host process toward OOM through **reflected method dispatch**, in four distinguishable ways, none of which any existing guard stops:

| Class | Shape | Example | Measured |
|---|---|---|---|
| **A — size in a readable numeric argument** | `String.PadRight(int[,char])`, `String.PadLeft(int[,char])`, `new String(char,int)` | `"a".padright(100000000)` | 190.8 MiB, **completes** (#7854) |
| **B — size in an opaque format string** | `ToString(String[,IFormatProvider])` on the **11 numeric primitives** | `(1.0).tostring("F100000000")` | 702.8 MiB, **completes** (#7854, corrected by #7868) |
| **C — attacker-chosen amplification factor over the receiver** ⟲ NEW | `String.Replace(string,string[,StringComparison])`, `String.ReplaceLineEndings(string)`, all 8 `String.Split` overloads | `$r=new string('b',1000); (new string('a',200000)).replace("a",$r)` | 381 MiB, **1000×**, completes (#7868 F2/F3) |
| **D — bulk mutation that bypasses the guard entirely** ⟲ NEW | `List<>.AddRange` / `InsertRange` | `while($i<26) { $l.addrange($l) }` | **768 MiB live against a 128 MiB ceiling, no exception, script returns success** (#7868 F1 / bug #7870) |

Class **D** is qualitatively worse than A–C: A–C are *unchargeable transients* that eventually throw once the value is retained, whereas D is an outright **bypass** — the guard reports success while the limit is exceeded by 4–6×.

The **root cause** for A–D is structural and named in #7854 / #7856: method dispatch is *reflection over the receiver's real .NET type* with **no method allow-list** — `hosttype.GetMethods(BindingFlags.Public | BindingFlags.Instance)` at `Pooshit.Scripts/Parser/Resolvers/MethodResolver.cs:60`. This is the exact structural analogue of the RCE root cause (#7787: no *type* allow-list, closed by `TypeGuard`), one level down: no *method* allow-list, so every allocating method on every reachable type is reachable.

The existing memory guard cannot close it. M1 (per-value ceiling) fires at *assignment*, i.e. **after** the allocation — round-7 measured `$s = "a".padright(100000000)` throwing with **190.8 MiB already allocated**, and round-8 measured `$t=$s.replace("a",$r)` throwing with **291 MiB already built**. M4 (the pre-allocation charge) intercepts *collection capacity operations* only. For class D, three mechanisms miss simultaneously (#7870): `AddRange` is `void` so `ChargeProducedValue` never runs; it is absent from the M4 table; and `VariableBudget.cs:108` sets `nextMeasureAt = ticks + max(256, units)`, so **doubling-per-checkpoint outruns a checkpoint-count cadence by construction**.

**Success criteria.**

1. On a bare parser, every class A–D payload in #7854 and #7868 yields a **clean typed exception with the allocation not performed** — measured as a `GC.GetTotalAllocatedBytes` delta in the low-MB range, the same load-bearing assertion M4's T41–T45 already use.
2. The mechanism is **default-deny over the reflected surface the engine itself hands to script**, so a member nobody enumerated — including one a future .NET runtime adds — is refused rather than silently reachable.
3. `ToString()` with no argument keeps working everywhere, unconditionally.
4. Backward compatibility is a **named, bounded, host-widenable** cost — not a silent break.
5. The design does not claim to close what it cannot reach: round-8's **#7869** (parse-time stack overflow) and **#7871** (uninterruptible ReDoS) are outside method dispatch and are explicitly not addressed here (§12).

**Severity framing, unchanged from #7854/#7858/#7868:** this is a **crash / resource-exhaustion class, never a sandbox escape**. Round-8 re-verified the escape boundary as unregressed. `TypeGuard` and `ScriptType` are not touched by this design.

---

## 2. Scope & Non-Scope

### In scope

- A **white/blacklist hybrid** (§7): an **identity allow-list** over the reflected BCL surface of round-8's closed reachable-receiver set, plus **signature/shape governance** for the host-injected surface, both enforced at the interpreter's single method-resolution site.
- **Pre-allocation policies** for the nine member names that can amplify (§8) — classes A, C and D — expressed as rows in the **existing** M4 pre-allocation table.
- The **format policy** for `ToString(String[,IFormatProvider])` on the 11 numeric primitives (§9) — class B — evaluated on the **runtime value** of the format argument, covering all three syntactic forms and both arities.
- The **host widening surface** and the **failure mode** (§10).
- The residual this does *not* close, stated precisely and not over-claimed (§12).

### Explicitly out of scope

- **#7869 — the parse-time `StackOverflowException`** (`"(" × 1500` kills the process uncatchably in `ScriptParser.Parse`/`ParseBlock`, before any limit exists). Not a dispatch site; a method allow-list does nothing. Round-8 ranks it the cheapest fix with the worst consequence. **Routed separately.**
- **#7871 — uninterruptible ReDoS** (`ScriptLimits.Default` leaves `RegexTimeout` null; the backtrack runs inside one native `Regex.IsMatch` where no checkpoint and no `CancellationToken` can reach). A one-line default change, not a dispatch concern. **Routed separately.**
- **#7868 F6 — no `MaxSteps`/`Timeout` in `Default`** (unbounded run time, 12 GB of churn behind a 1.2 MB retained value). A default-profile question, and interruptible by a host `CancellationToken`. Not designed here.
- **The `VariableBudget.Measure` cadence weakness** (`VariableBudget.cs:108`, `nextMeasureAt = ticks + max(256, units)`). It is *one of three* causes of #7870, and this design closes #7870 by removing a different one (the missing pre-charge). The cadence is a **general** weakness that will re-appear for any future doubling primitive, and it belongs to `VariableBudget`, not to a method guard. **This design owns the pre-call charge; it does not own the cadence.** Named in §12 and OQ-5.
- **Properties, fields and indexers.** Decision and reasoning in §7.5 — method-only, with the one contract defect round-8 surfaced routed as its own item.
- **The compiled path (`ExpressionBuilder` / `ParseDelegate`).** No `ScriptContext`, no `ScriptLimits`, no `VariableBudget`, and it does not call `MethodResolver` (`ExpressionBuilder.cs:537` `BuildMethod`, `:555` its own `GetMethods`, `:580` `Expression.Call`). Language reference §13 and `cancellation-support.md` already state as a contract that it is **not guardable and must not be used to run untrusted code**. Only the *identity* half of the guard is implementable there — the value-dependent half cannot exist without argument values — so a partial guard would advertise coverage that does not exist. #1136 §4 delete-check: absent, nothing real breaks. **Deleted from this design; recorded as a residual (§12).**
- **Constructors other than `new String(char,int)` and the M4 capacity ctors.** `ResolveConstructor` is reachable only for `AddType`-registered types; round-8 confirmed array-`new` syntax is unsupported and `new dictionary(n)` unreachable. No constructor allow-list.
- **Extension methods.** `AddExtensions` is an explicit host/engine registration — the opt-in *is* the allow-list. Extensions bypass the guard by construction (§6.2).
- ⟲ ~~**Host-registered types and host return values.** The host's opt-in, identical to `execution-guards-depth-memory.md` §8.8.7 and to `TypeGuard`'s treatment of `AddType`.~~ **Superseded by §0.2.** Host-injected types are now **in scope**, governed by signature and shape rather than by identity (§7.6). What remains out of scope is the *semantics* of a host method's body — the engine still cannot know what `MyType.Render()` allocates internally (§12 residual 10).
- **The static BCL surface.** Already closed: `MethodResolver.cs:60` binds `Public | Instance` only, and round-8 verified `String.Join`/`Concat`/`Format`/`Create`/`Copy`/`Intern` all fail resolution cleanly. Nothing to build.
- **Process/container isolation.** The only complete answer for arbitrary host-surface allocation (§12); designed elsewhere.
- **Re-opening the escape boundary, the depth guard, the step budget, the cancellation spine, or M1/M2/M3.** Untouched.

---

## 3. Assumptions & Constraints (every row verified against the working tree or against a round-8 measurement)

| # | Statement | Verified at |
|---|---|---|
| A1 | `MethodResolver.Resolve` receives the **live receiver instance** and **already-evaluated runtime argument values** (`object[] parameters`), not tokens and not types. A guard placed there can read `parameters[0]` as the actual format string. | `MethodResolver.cs:44`; caller `ScriptMethod.cs:109` (`Parameters.Select(p => p.Execute(context))`) |
| A2 | `Resolve` caches on `MethodCacheKey(hosttype, methodname, argument **types**, refparams, genericparams)` and **returns early on a hit**. Therefore any *value-dependent* check must sit **before** the cache lookup; any *value-independent* check may sit after it, provided nothing denied is ever written to the cache. | `MethodResolver.cs:53-58`; `TypeGuard` precedent at `:47-51` with the comment *"deny before the cache"* |
| A3 | `Resolve` does **not** receive `ScriptContext`. `ScriptLimits` / `VariableBudget` are **not** reachable there, and `IMethodResolver.Resolve`'s signature is public API. | `IMethodResolver.cs`; `ScriptMethod.cs:122` |
| A4 | `ScriptContext` **is** available at every invoke site: `MethodOperations.CallMethod(…, ScriptContext context, …)`, `MethodOperations.CallConstructor(…, ScriptContext)`, `TypeInstanceProvider.Create(…, ScriptContext)`. This is why M4 hooks there. | `MethodOperations.cs:279`, `:233`; `TypeInstanceProvider.cs:32` |
| A5 | Resolution strictly precedes invocation: `ScriptMethod.ExecuteToken` calls `resolver.Resolve(...)` on line 122 and `method.Call(...)` on line 123. A throw in `Resolve` provably precedes any allocation the method would perform. | `ScriptMethod.cs:122-123` |
| A6 | **All three syntactic forms that reach `ToString(format)` converge on one `ScriptMethod` node.** The `:` format operator is a **parse-time rewrite** to `new ScriptMethod(MethodCallResolver, receiver, "ToString", [format, InvariantCulture])`; an interpolation hole `{…}` is parsed by `ParseStatementBlock` → `Parse(…)` with `suppressformat = false`, so a `:` inside a hole hits the *same* rewrite. `StringInterpolation.Execute` contains **no formatting logic** — it is `string.Join("", tokens.Select(t => t.Execute(context)?.ToString()))`. | `ScriptParser.cs:1320-1336` (rewrite at **:1334**); `ScriptParser.cs:782-798` + `:813`; `StringInterpolation.cs:25-27` |
| A7 | The parse-time format scanner accepts letters, digits, `#` and `.` with **no length or digit-count bound**, so `$"{1:D100000000}"` parses. It is **not** a bound. `.tostring($runtime)` bypasses it entirely — the format can be computed at runtime (`(1).tostring("D" + $n)`, #7854). | `ScriptParser.ParseFormatString`, `ScriptParser.cs:853-866` |
| A8 | The M4 table is a hard-coded predicate, not a data structure: `TryGetCapacityOperation(Type, MethodBase, out long bytesPerUnit)` requires `parameters.Length == 1 && parameters[0].ParameterType == typeof(int)` and every charge site reads the size from `callparameters[0]`. It cannot express any of the four new projection shapes (§8.2). | `VariableSizer.cs:189-206`; sites `TypeInstanceProvider.cs:37-38`, `MethodOperations.cs:244-245`, `MethodOperations.cs:295-298` |
| A9 | `VariableSizer.CurrentCapacity(receiver)` returns **0** when the receiver exposes neither a public `Capacity` nor the private dictionary bucket field. A `string` receiver exposes neither, so the existing delta arithmetic charges the **full** requested size for a string operation — correct, because `PadRight` allocates a *new* string rather than growing one. **No "grow vs. allocate-new" flag is needed.** | `VariableSizer.cs:172-179` |
| A10 | The relevant footprint constants already exist: `ObjectHeaderBytes = 24`, `ReferenceSize = 8`, `CollectionElementOverhead = 8`, `DictionaryEntryOverhead = 24`; the string per-character rate is the literal `2L` at two sizer sites. | `VariableSizer.cs:18-25`, `:56`, `:62` |
| A11 | `VariableBudget.ChargePreAllocation(long)` already implements the M1-shaped single-op ceiling **and** the M2-shaped accumulation gate, throws `ScriptVariableLimitExceededException(Kind = Bytes)`, and is a no-op when `MaxVariableBytes` is unset. Directly reusable. | `VariableBudget.cs:59-71` |
| A12 | `TypeGuard` is a **static** class with two hard-coded tables and **no widening API at all**; its denials throw a plain, script-catchable `ScriptRuntimeException`. `AddType`/`AddExtensions` are unrelated registration surfaces. | `TypeGuard.cs`; `MethodResolver.cs:49,51`; `TypeProvider.cs:28-41`; `ExtensionProvider.cs:36-50` |
| A13 | `ScriptLimits` is immutable (`init`-only knobs) and its doc comment names the **mixed-host process** (some trusted, some sandboxed, one process) as an explicitly served scenario. Any widening surface must therefore be **per-parser**, not process-global. | `ScriptLimits.cs:9-18` |
| A14 | `ScriptMethod.ExecuteToken` intercepts `gettype` **before** resolution, so `gettype` never reaches `Resolve`. | `ScriptMethod.cs:88-89` |
| A15 | `MethodResolver.Resolve` is called from exactly **one** production site. | `ScriptMethod.cs:122` (plus `MethodResolverTests`) |
| **A16** | **The reachable-receiver set is CLOSED and small: 38 types.** Computed mechanically from the 17 `Types.AddType` registrations plus literal-producible types, transitively through public-instance return types and the flag-less `GetProperties()`/`GetFields()` shapes, pruning `TypeGuard`'s deny-family. **`DateTime`, `TimeSpan`, `Guid`, `CultureInfo`, `StringBuilder`, `Regex` and all of `System.IO` are absent** — unreachable on a bare parser. | #7868 METHOD + triage table |
| **A17** | **The entire static BCL surface is already closed** by `MethodResolver.cs:60`'s `Public \| Instance` binding — round-8 verified `String.Join`/`Concat`/`Format`/`Create`/`Copy`/`Intern` all throw "method not found". | #7868 PASS note |
| **A18** | The **receiver** is available at the charge site: `MethodOperations.CallMethod` takes `object host` and already uses it (`VariableSizer.CurrentCapacity(host)` at `:296`). A receiver-derived projection (required for `Split`, §8.2 shape 4) is therefore implementable with **no new plumbing**. | `MethodOperations.cs:279`, `:295-296` |
| **A19** | The `ToString(String, IFormatProvider)` overload **is** reachable from script by passing a literal `null`: `(1).tostring("D100000000", null)` → 190.8 MiB. The format policy must cover both arities. | #7868 corrections |
| **A20** | `Char` and `Boolean` and `String` have **no public** `ToString(string)` overload, and the reachable enum `TypeCode` restricts formats itself. The `ToString(format)` family is **exactly the 11 numeric primitives**. | #7868 corrections |
| **A21** | **The 38-type closure is closed only on a bare parser.** `Types.AddType<T>()` (`TypeProvider.cs:33-41`) and any host method return type add arbitrary types to the reachable set at host-configuration time, i.e. *after* any inventory could have been generated. Every real consumer does this. **An identity inventory therefore cannot be the whole governance model.** | `ScriptParser.cs:45-61` is only the *engine's* seeding; `TypeProvider.AddType` is public and unbounded. Operator ruling §0.2 |
| **A22** | `IFormattable` is the **signature-level** expression of the `ToString(format)` family: any type whose values script can format through a `ToString(String, IFormatProvider)` overload implements it. The predicate is **superset-safe** — on some TFMs `Char`/`Boolean` implement `ISpanFormattable` (hence `IFormattable`) via *explicit* interface implementation, which `GetMethods(Public \| Instance)` does not surface, so resolution fails with "method not found" before the policy is consulted. Over-inclusion therefore costs nothing; under-inclusion would be a hole. **Pinned by T75, not asserted.** | `MethodResolver.cs:60` binding; A20 |
| **A23** | `VariableBudget` exposes the byte ceiling only *through* `ChargePreAllocation`, which both tests **and accumulates**. A ceiling test that must **not** accumulate (§7.6.3 — an argument value is not an allocation) needs one new non-accumulating method on the same class. | `VariableBudget.cs:59-71` |

**Constraint carried from #7736:** no new host-facing knob on `ScriptLimits`, no new abort exception type, no second copy of a magic number.

---

## 4. Architectural Overview

Two questions, asked at two different moments, because they need two different things:

```
  script:  "a".padright(1e8)   $l.addrange($l)   $s.replace("a",$r)   $s.split(",")   (1).tostring("D"+$n)   $"{$x:F2}"   1:X
                 │                    │                 │                  │                   │                  │        │
                 │                    │                 │                  │                   └────────┬─────────┴────────┘
                 │                    │                 │                  │                            │ parse-time rewrite
                 │                    │                 │                  │                            ▼ (ScriptParser.cs:1334)
                 └────────────────────┴─────────────────┴──────────────────┴──────────►  ScriptMethod  ◄──────────
                                                                                              │  args already evaluated to VALUES
                                                                                              ▼
   ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────┐
   │ MethodResolver.Resolve            (MethodResolver.cs:44)                                                  │
   │  ① TypeGuard        (existing, :47-51)     — reflective receiver / name                                   │
   │  ② MethodGuard.CheckFormatArgument         — VALUE-dependent  ⇒ PRE-CACHE                                 │  ← class B
   │     ─────────────── cache lookup (:53-58) ───────────────                                                 │
   │     candidate reflection + overload scoring (:60-101)                                                     │
   │  ③ MethodGuard.CheckAllowed(hosttype, winner, isExtension)   — identity, cache-safe                       │  ← the allow-list
   │     ─────────────── cache WRITE (:107) ───────────────                                                    │
   └──────────────────────────────────────────────────────────────────────────────────────────────────────────┘
                                                                                              │  IResolvedMethod — nothing allocated yet
                                                                                              ▼
   ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────┐
   │ MethodOperations.CallMethod       (MethodOperations.cs:279)                                               │
   │   parameter conversion (:283-293)                                                                         │
   │   ④ pre-allocation charge  (:295-298, GENERALIZED: receiver + member + args → projected bytes)            │  ← classes A, C, D
   │      VariableSizer.TryGetPreAllocationOperation(host, method, args, out bytes) → ChargePreAllocation()     │
   │   ────────────────────────────────────────────────                                                        │
   │      method.Invoke(...)   (:301)   ← the BCL allocation happens HERE                                       │
   └──────────────────────────────────────────────────────────────────────────────────────────────────────────┘

   new string('a', n)  ─► TypeInstanceProvider.Create (:32) ─► ④ (:37-38) ─► constructor.Invoke (:40)
```

- **② and ③ are the new guard object `MethodGuard`** — one class, one consultation site (`MethodResolver.Resolve`), mirroring `TypeGuard`'s position and style. ⟲ **Both governance tiers decide at these same two points**; the branch is on the receiver (tier 1 identity vs tier 2 shape, §7.6), never on the site. The §0.2 ruling changed *who governs what*, not *where the check fires*.
- ⟲ **A third check, H2 (§7.6.3), sits at ④** — the integral-argument value ceiling, which needs `MaxVariableBytes` and therefore cannot live in `Resolve` (A3). It applies only when no projection row matched.
- **④ is not a new site.** Classes A, C and D are closed at the three sites M4 already occupies. What changes is the *shape* of a table row: from `(int argument index, bytesPerUnit)` to a **projection over `(receiver, member, arguments)`** (§8.2), because round-8 proved three of the four shapes cannot be expressed as an int argument.

**Why the split is not arbitrary, stated as a rule:**

> A check that depends on **argument values** must run **before the resolution cache** (A2) and needs no budget (class B is a pure predicate on a string) → it lives in `Resolve`.
> A check that needs the **`VariableBudget`** cannot run in `Resolve` at all (A3) and must run at the invoke site (A4), where the **receiver** is also in hand (A18) → it lives where M4 already lives.

---

## 5. Components & Responsibilities

| Component | Owns | Does **not** own |
|---|---|---|
| **`MethodGuard`** (NEW — `Pooshit.Scripts/Parser/Resolvers/MethodGuard.cs`) | (a) the **tier-1 type set** (round-8's 38-type closure) and its generated per-type **allow-list of lowered method names**; (b) the **tier-2 shape rules** (H1/H3) and the per-parser host rule sets; (c) the **acceptable-format rule**; (d) the four host operations (`Allow`, `Ungovern`, `Charge`, `Deny`). | Any byte accounting. Any knowledge of `ScriptContext`, `ScriptLimits` or `VariableBudget` — **including H2, which needs the budget and therefore lives at the invoke site** (§7.6.3). Any knowledge of extensions, properties, indexers or constructors. |
| **`MethodResolver.Resolve`** (CHANGED) | Consulting `MethodGuard` at two points and throwing on denial. Nothing else. | Deciding *what* is allowed. |
| **`VariableSizer`** (CHANGED) | The **pre-allocation operation table**, now covering `String` and `List<>` bulk operations alongside the capacity ops, and computing the **projected bytes** itself from receiver + member + arguments. | Throwing. Deciding allow/deny. |
| **`VariableBudget.ChargePreAllocation`** (UNCHANGED) | The ceiling, the accumulation gate, the throw, the charge-once handoff to M3. | The `Measure` cadence weakness (§2 out-of-scope, OQ-5). |
| **`MethodOperations.CallMethod` / `CallConstructor`, `TypeInstanceProvider.Create`** (CHANGED, 2 lines each) | Calling the table with `(host, member, callparameters)` and charging what it returns. | The projection arithmetic — it moves into the table (§8.3). |
| **`ScriptParser`** (CHANGED, 2 lines) | Owning one `MethodGuard` instance and exposing it as `parser.Methods`, alongside the existing `Types` and `Extensions`. | — |

---

## 6. The Interception Points — named, and proven to fire before allocation

### 6.1 Class B — `MethodResolver.Resolve`, before the cache

**`Pooshit.Scripts/Parser/Resolvers/MethodResolver.cs` → `MethodResolver.Resolve`**, inserted immediately after the existing `TypeGuard` block (`:47-51`) and **before** the cache lookup at `:53`.

Condition (all four, evaluated on runtime values):

1. `MethodGuard.IsFormatFamily(host.GetType())` — ⟲ **now `typeof(IFormattable).IsAssignableFrom(receiverType)`**, one predicate for both tiers (§7.6.2, A22), superseding the hand-listed 11 numeric primitives, and
2. `methodname == "tostring"` (already lower-cased by `ScriptMethod.cs:27`), and
3. `parameters.Length >= 1` — **covers both the 1-arg and the 2-arg `(format, provider)` overload** (A19), and
4. `parameters[0] is string format` (non-null).

If all four hold and `MethodGuard.IsAcceptableFormat(format)` is false → throw (§10.3). Otherwise fall through unchanged.

**Why here and not at the invoke site.** Three properties are needed simultaneously and only this position has all three: the **runtime value** of the format argument (A1 — it may be computed, `"D" + $n`); **execution on every call, not once per cache key** (A2 — the cache key stores argument *types*, so `tostring(string)` would be checked once and served from cache forever); and **no `VariableBudget` requirement** (A3 — the rule is a pure predicate on a string).

**Proof it precedes allocation.** `ScriptMethod.ExecuteToken` calls `Resolve` at `ScriptMethod.cs:122` and only then `method.Call(...)` at `:123`, the only route to `MethodOperations.CallMethod` → `method.Invoke` at `MethodOperations.cs:301`. The throw unwinds before line 123 is reached, and nothing in `Resolve` allocates proportionally to the format's precision. Pinned by T54.

### 6.2 The allow-list — `MethodResolver.Resolve`, on the resolved winner

Same file, same method, after the winner is selected at `:103-104` and **before** the cache write at `:107`:

```
if (!isExtension && !MethodGuard.IsAllowed(hosttype, winner))  →  throw
```

**Why on the resolved `MethodInfo` and not on the name before the cache.** `Resolve` resolves *both* the receiver's reflected BCL methods *and* registered extension methods in one pass (`:60` reflected, `:67-100` extensions). A name-only pre-cache check cannot tell them apart, so it would refuse extension methods whose names are absent from a BCL allow-list — e.g. every `EnumerableExtensions` method on `List<>`. Checking the winner gives the `isExtension` flag for free (`MethodResolver.cs:79/97` → `ResolvedMethod`), so **extensions pass by construction**, which is correct: `AddExtensions` *is* the opt-in.

**Why placing it after the cache read is still cache-safe** (a deliberate deviation from `TypeGuard`'s pre-cache placement, justified rather than copied):

- The verdict is a pure function of `(hosttype, resolved MethodInfo, isExtension)`; the resolution is itself a pure function of the cache key. Same key ⇒ same winner ⇒ same verdict. No value dependence.
- A denied method **is never written to the cache** — the throw precedes `methodcache[cachekey] = …` at `:107`. A cache hit is therefore, by construction, an already-allowed method. `TypeGuard`'s invariant is preserved.

**Proof it precedes allocation.** Identical to §6.1.

### 6.3 Classes A, C, D — the existing M4 sites, unchanged in position

| Route | Site | Charge inserted before |
|---|---|---|
| `padright` / `padleft` / `replace` / `replacelineendings` / `split` / `addrange` / `insertrange` / `ensurecapacity` | `Pooshit.Scripts/Operations/MethodOperations.cs` → `MethodOperations.CallMethod` : **295-298** | `method.Invoke` at **:301** |
| `new string('a', n)`, `new list(n)` | `Pooshit.Scripts/Providers/TypeInstanceProvider.cs` → `TypeInstanceProvider.Create` : **37-38** | `constructor.Invoke` at **:40** |
| host constructor route | `Pooshit.Scripts/Operations/MethodOperations.cs` → `MethodOperations.CallConstructor` : **244-245** | `constructor.Invoke` at **:248** |
| `$l.capacity = n` | `Pooshit.Scripts/Tokens/ScriptMember.cs` → `ScriptMember.SetProperty` : **104-107** | `property.SetValue` at **:111** |

These are the sites M4 already occupies. **No new site is added.** The receiver is in scope at all of them (A18), which is what makes `Split`'s receiver-derived projection possible. The pre-allocation proof is the one #7736 §8.8.6 established and T41–T45 pin; §14 adds the same allocation-delta assertion for every new row.

---

## 7. What is governed, and the curation criterion

### 7.1 Tier 1 — the built-in surface: round-8's closure, without carve-outs

> **A receiver type belongs to *tier 1* iff the engine itself hands the script that type's full public BCL surface without the host opting in.**

Round-8 computed exactly that set: **the 38-type reachable closure** (A16). Tier 1 **is** that closure — `String`, the 11 numeric primitives, `Char`, `Boolean`, `List<>`, `Dictionary<,>`, `Object`, the reachable array types, and the remaining inert types (enumerators, `Rune`, `TypeCode`, `KeyValuePair`, `ReadOnlyCollection`, `CharEnumerator`, `StringRuneEnumerator`, `IEqualityComparer`, …).

Tier 1 is enumerable, so it is **enumerated**: an identity allow-list, generated (§7.3), default-deny. This is the *whitelist* half of the hybrid, and it is exactly where a whitelist is honest — the domain is closed and the engine owns it.

> **⟲ Corrected by the §0.2 governance ruling.** An earlier draft said *"**Not governed:** host-registered types and host return values … Governing them would make `AddType<T>()` useless."* That sentence was right that identity-listing a host type is useless and wrong that the alternative is *no* governance. A18–A21: the closure is closed **only on a bare parser**, and every real consumer calls `AddType<T>()`, so a design that governs tier 1 by identity and leaves everything else ungoverned describes a closed set that is not closed in production.
>
> The fix is not to extend the inventory — it *cannot* be extended to a surface that does not exist until host-configuration time. It is a **second tier with a different mechanism**: §7.6 governs host-injected types by **signature and shape**, which needs no inventory and is exactly the reasoning §9.3 already used (and round-8 already validated) one level up.

**Tier 2** is therefore everything else — host-registered types, host return types, and any type reachable only through them. §7.6.

> **⟲ Corrected by round-8.** An earlier draft of this document carved `List<>`, `Dictionary<,>`, `Object` and arrays *out* of governance on a risk argument: round-7 had answered *"NO — a script cannot make a large array/list in one op that M4 does not already cover"*, so governing them looked like ~32 allow-list names for zero benefit. **Round-8 falsified that carve-out with the single worst finding in the enumeration**: `List<>.AddRange`/`InsertRange` (#7870) is an outright guard bypass, and it sits squarely inside the carved-out set.
>
> The lesson is not "add List back". It is that **a risk-based carve-out from a default-deny criterion is a default-allow decision wearing default-deny clothing**, and it fails exactly where the enumeration was incomplete — which is the case default-deny exists to survive. The criterion is now applied **without exception**. The cost of doing so is inventory size, and §7.3 removes that cost by generating the inventory rather than hand-writing it.

**The seam:** adding a governed type later is one entry in the generated type list. Zero present cost (#1184).

### 7.2 The curation criterion — the rule that makes the list decidable

> **A member on a governed type is allow-listed only if its result size is bounded by `k × (receiver size + Σ argument sizes)` for a small constant `k`.**
>
> Receiver and arguments are themselves script values, each already charged at `≤ MaxVariableBytes` by M1, so such a member's transient peak is bounded by a small multiple of the configured budget — the guard model's existing contract.
>
> A member whose result can exceed that bound is either **(a)** allow-listed *with* a pre-allocation policy that bounds it from the runtime receiver and arguments (§8), or **(b)** allow-listed *with* the format policy (§9), or **(c)** not allow-listed at all.

Round-8 applied exactly this criterion mechanically across all 38 types and produced the partition in §7.3. Note the criterion's own limit, which round-8's `Normalize(FormKD)` row illustrates: a **fixed** constant factor (3× on U+FDFA) satisfies it; an **attacker-chosen** factor (`Replace`, 1000× measured) does not. The distinction is *who picks `k`*.

### 7.3 The default allow-list — generated, not hand-written

The partition, from round-8's measured triage:

| Governed type | Public-instance members | Allow as-is | Policy members |
|---|---|---|---|
| `String` | 110 | **102** | `padleft`, `padright`, `replace` (×2 overloads), `replacelineendings`, `split` (×8 overloads), and the ctor `.ctor(char,int)` |
| 11 numeric primitives | 13 each (22 for `Decimal`) | **12 each** | `tostring(String[,IFormatProvider])` |
| `Char` | 9 | **9** | none |
| `Boolean` | 10 | **10** | none |
| `List<>` | 54 | **51** | `addrange`, `insertrange`, plus the already-covered `.ctor(int)` / `ensurecapacity` / `capacity` |
| `Dictionary<,>` | 26 | 24 | the already-covered `.ctor(int)` / `ensurecapacity` |
| the remaining ~22 closure types | — | **all** | none |

Roughly **300 allow entries and 9 policy member names**. Three hundred names is too many to hand-write correctly, and every omission is a script break — so **the allow inventory is generated, not authored**:

> **Derivation procedure (one-off, then checked in as literal `static readonly` data).** For each governed type, enumerate `type.GetMethods(BindingFlags.Public | BindingFlags.Instance)` — the *same* binding `MethodResolver.cs:60` uses — take the distinct lower-cased names, and subtract the policy names. Emit the result as the literal table in `MethodGuard.cs`. The governed type list is itself generated by re-running round-8's closure computation (seeds = the `Types.AddType` registrations + literal-producible types; transitively follow public-instance return types, `GetProperties()` and `GetFields()`; prune `TypeGuard`'s deny-family).

**And the same code is the drift test (T68).** A test re-runs the enumeration against the running runtime and fails if the checked-in table no longer matches. This is what makes a 300-name allow-list maintainable, and it is what converts the fail-closed property from *silent* into *observable*: when a future .NET runtime adds `String.Foo`, the new member is refused (correct, fail-closed) **and** a red test tells the maintainer to decide about it. Without the test, the fail-closed behaviour is right but invisible; with it, a runtime upgrade is a review item rather than a mystery break.

**Curation is therefore not a judgement call at authoring time** — it is round-8's measurement, mechanically materialized, with 9 hand-written policy rows carrying the actual logic.

### 7.4 `replace` and `split` — ⟲ corrected by round-8

> An earlier draft allow-listed `replace` and `split` **without a policy**, on the reasoning that round-7 had classed `replace` *source-bounded 2×* and that denying two of the most-used string methods on a suspicion was an unearned break. It flagged both as *"the two names I am not certain about"* and handed them to round-8.
>
> **Round-8 measured them and the provisional call was wrong.** `Replace` amplifies by an **attacker-chosen** factor — 1000× measured (`$r=new string('b',1000); (new string('a',200000)).replace("a",$r)` → 200 M chars, 381 MiB), and `ReplaceLineEndings` reached 300× with the worst transient in the whole enumeration (1311 MiB). `Split` amplifies 9–16× **with no size argument anywhere**, and the count-capped overload does **not** bound the transient (`split(",",5)` still allocated 332 MiB).

Both are now **policy members** with pre-allocation rows (§8.1). Neither is denied: a computable bound exists for both, so the criterion's branch (a) applies and the everyday uses keep working.

**`Split` is the finding that shaped the mechanism.** Its bound is a function of the **receiver's content** (separator density), not of any argument. A guard that inspects only the parameter array cannot bound it at all. This design's charge site holds the receiver (A18), so the conservative receiver-derived projection is implementable — and this is, as round-8 puts it, the strongest single argument for default-deny: *a permit decision for `Split` is not derivable from its arguments.*

### 7.5 Methods only, not members — decided explicitly

Round-8 surfaced that `ScriptMember.cs:63/74` resolves properties and fields with **flag-less** `GetProperties()`/`GetFields()`, which reaches **static** members: `(1).maxvalue`, `(1.0).pi`, `"".empty` all work.

**Decision: the allow-list is method-only.** Three reasons:

1. Round-8 measured **zero** allocating property or field across the entire 38-type closure. There is no vector to close (#1136 §4 delete-check ⇒ do not build).
2. The one property that *is* a charge site — the `List<>.Capacity` setter — is already an M4 row and stays there. That proves property-**sets** are handled by the *charge* mechanism, not by an allow-list, so extending the allow-list to properties would not have covered it anyway.
3. Extending the inventory to properties and fields roughly doubles it for zero measured benefit.

**But the flag-less binding is a real contract defect, and it is not mine to fix here.** Reaching static members contradicts the documented contract (*"`AddType` does NOT expose static members"*, language reference §15 misconception 4) and contradicts #7793's deliberate decision to bind `Public | Instance` in `MethodResolver` precisely to align with that contract. It is a one-line binding-flags change of exactly the same shape, in a different file. **Recommend filing it as its own bug** (OQ-4) rather than smuggling it into this PR — it is a *correctness/contract* fix, not an OOM fix, and bundling it would violate one-feature-per-PR.

### 7.6 Tier 2 — the host-injected surface, governed by signature and shape

This section is the §0.2 ruling. It answers the four questions the ruling raised, in order.

#### 7.6.0 The principle, stated honestly

An inventory cannot govern a surface that does not exist when the inventory is generated. What *can* govern it is the question the ruling names: **what is the relationship between the argument values and the size of what comes back?**

But that question has a hard limit that must be stated before any rule is written, because pretending otherwise would be over-claiming of exactly the kind §12 exists to prevent:

> **The engine cannot know what a host method does.** `Report.Render()` takes no arguments and may allocate 2 GB. No shape rule sees that, and none ever will.

So tier 2 does **not** bound host code. What it bounds is **script-driven amplification** — the shapes where a *small, script-chosen argument* commands a *large* allocation. That is the actual threat model and always was: the attacker writes the script, not the host's methods. A host method that allocates heavily on its own is the host's own code, deliberately registered, and it is the pre-existing, operator-accepted boundary (`execution-guards-depth-memory.md` §8.8.7; #7736 §11 *"mitigations, not a security boundary"*).

**How the ruling's default-deny survives this.** *"It doesn't exist for our script when we cannot control it"* binds both tiers, but **who** controls differs, and that is the whole distinction:

| Tier | Who controls the surface | What "cannot control it" therefore means |
|---|---|---|
| 1 — built-in | **the engine** | anything the engine cannot enumerate is refused → identity allow-list, default-deny |
| 2 — host-injected | **the host**, by the deliberate act of `AddType<T>()` | the engine refuses the shapes that would let a **script** amplify beyond what its own arguments imply; the host's own allocation choices are the host's |

`AddType<T>()` **is** the host asserting control. That has been the posture since `TypeGuard` (#7793) and was accepted in #7736 §8.8.7; nothing in round-8 challenged it. Tier 2 does not weaken it — it adds engine-level rules *on top of* it that did not exist before.

#### 7.6.1 The vocabulary — the same triple `Split` already forced (answers Q1)

The `Split` row (§8.1 row 6) already forced the projection signature to widen from *(int argument index, bytesPerUnit)* to a projection over **`(receiver, member, arguments)`**, because a permit decision for `Split` is not derivable from arguments alone. That triple, plus the member's **parameter type profile**, is exactly the vocabulary the tier-2 rules need. **Nothing new is required.**

| Consumer | Keyed on | Returns |
|---|---|---|
| `VariableSizer.TryGetPreAllocationOperation` (tier 1 rows + host `Charge` rows) | `(receiver, member, arguments)` | a **projection** — bytes |
| `MethodGuard` tier-2 rules | `(receiver, member, arguments)` + `member.GetParameters()` | a **verdict** — permit / refuse |

One vocabulary, two consumers, split by what they can produce: where a projection exists, charge it; where it does not, decide.

#### 7.6.2 H1 — the `IFormattable` rule (the ruling's direct generalization)

The operator's original worry was *"the general tostring which exists everywhere"*. It exists on host types too, and an 11-type hand-list does not see them.

> **H1.** For **any** receiver assignable to `IFormattable`, a `tostring` call whose first runtime argument is a non-null `string` is subject to the §9 format policy.

This is a **signature** predicate, not a name list, and it **replaces** the hand-written 11-type family — §9.1 gets *simpler*, not more complex (A22). It reproduces round-8's measured set on the built-in surface and extends correctly to a host's `DateTime`, `TimeSpan`, `Guid`, custom `IFormattable` value types, and anything a future runtime adds. Over-inclusion is free: if a type has no *public* `ToString(string, …)` overload, resolution fails before the policy is consulted (A22).

#### 7.6.3 H2 — the value-commanded ceiling (answers the "how do you bound the unknowable" half)

The one thing the engine *can* say about a host member without knowing its body: **an argument value larger than the entire byte budget cannot be a legitimate size for anything the budget could hold.**

> **H2.** For any member with an **integral** parameter (`byte`…`ulong`), each integral argument's **runtime value** is checked against `MaxVariableBytes`. A value exceeding the budget is refused. H2 fires only where **no exact projection row matched** — a row is strictly stricter, so running both would be redundant.

Worked: `GetCustomer(12345)` → 12 345 ≤ 134 217 728 → permitted, unremarkable. `Repeat(2000000000)` → 2e9 > 1.34e8 → refused before the call. `StringBuilder.Append('a', 2000000000)` on a host-registered `StringBuilder` → refused.

**What H2 is and is not.** It is the *minimum-plausible-cost* assumption — one byte per unit — and it therefore closes the **literal-driven catastrophic** case (a 2e9 argument) on any host member, without pretending to know the per-unit cost. It does **not** close a host member that allocates 100 bytes per unit at a value the budget nominally admits; that is a named residual (§12 residual 11), and the host's recourse is `Charge<T>()` (§10.2), which turns the guess into an exact row.

**Placement consequence.** H2 needs `MaxVariableBytes`, which is unreachable in `MethodResolver.Resolve` (A3). It therefore lives at the **invoke site** beside the pre-allocation charge, not in `Resolve` — the same split §4 already states as a rule. It needs one new non-accumulating ceiling method on `VariableBudget` (A23): an argument value is a *bound to test*, not an allocation to accumulate, and charging it would pollute `producedSinceLastPass` on every `GetCustomer(i)` in a loop.

#### 7.6.4 H3 — permit-by-shape, and the fifty harmless getters (answers Q2)

> **H3.** A tier-2 member is **permitted** iff every parameter is either **(a)** non-integral — an aggregate whose byte footprint M1 already charged (`string`, a collection, a host object) or a non-size scalar (`bool`, `char`, enum, a reference) — or **(b)** integral with a value within H2's ceiling; **and** it is not refused by H1 or by a host `Deny` entry.

**The fifty harmless getters answer, plainly: they need no listing at all.** A zero-argument getter has no parameters, so H3(a) is vacuously satisfied and it is permitted by shape. So is `SetName(string)`, `Find(MyFilter)`, `Add(MyItem)`. A host registering an ordinary domain type writes **nothing**. That is the requirement the ruling set, and it is met.

**Is this still default-deny?** H3 is written as a **permit rule**, deliberately — a member is permitted *because* it matches a shape, not *unless* it matches a refusal. The distinction is not cosmetic: tightening the model later means **adding a condition to a permit rule**, which refuses more; under an "allow unless blacklisted" phrasing the same change means adding a name, which refuses only that name. The permit-rule phrasing is what makes tier 2 fail toward refusal as it evolves. And it is the strongest form available for a surface that cannot be enumerated — which the ruling states outright: *"we can not whitelist every potential (external injected)."*

#### 7.6.5 The blacklist half (answers Q4)

The ruling asks for *"blacklist for known-bad specifics that pattern rules would otherwise wave through."* Two owners, two different things, no overlap:

**Engine-owned: realized as projection rows, not as a name set.** Every known-bad specific we can currently name — the nine of §8.1 — is **projectable**. A projectable known-bad expressed as a row refuses **exactly when unaffordable** instead of always, which is strictly better than a name in a deny set: `"a,b".split(",")` keeps working while `(new string(',',20000000)).split(",")` does not. **So the engine's blacklist *is* the row table**, and no separate engine name-set is built — building an empty structure would be YAGNI (#1136 §4 delete-check: it has no entries today, because every entry we can name became a row).

Deliberately **not** shipped: speculative rows for BCL types a host *might* register (`StringBuilder`, `MemoryStream`, …). No named consumer (#1136 §3), and H2 already bounds their catastrophic case. A host that registers one uses `Charge<T>()`.

**Host-owned: `Deny<T>(names…)`.** This is not an extension of the engine's list — it is the host governing **its own** type, which is a different thing with a different owner. Its named consumer is the one case **no shape rule can ever see**: a **zero-argument host member that allocates unboundedly** (`Report.Render()`, §7.6.0). H1/H2/H3 are all blind to it by construction; the only entity that knows is the host, so the only workable mechanism is the host saying so.

**Why the engine blacklist is not host-extensible:** it encodes knowledge about *BCL* types that the engine has and the host has no reason to restate. Letting hosts append to it invites every host to maintain a private copy of engine knowledge — the parallel-constants smell (#1136 §6). A host that disagrees with an engine row has `Ungovern<T>()`; a host that wants to govern its own type has `Charge`/`Deny`.

#### 7.6.6 The layered model, whole

```
                             receiver type
                                   │
              ┌────────────────────┴────────────────────┐
      in the 38-type closure                    anything else
        (engine hands it out)                (host-registered / host-returned)
              │                                          │
              ▼                                          ▼
   TIER 1 · IDENTITY ALLOW-LIST              TIER 2 · SHAPE RULES
   generated inventory (§7.3)                H3 permit-by-shape (§7.6.4)
   default-deny                              host Deny entries (§7.6.5)
              │                                          │
              └────────────────────┬─────────────────────┘
                                   ▼
              UNIVERSAL SIGNATURE RULES — both tiers
              H1  IFormattable ⇒ format policy   [Resolve, pre-cache]   §7.6.2
              H2  integral arg value ≤ budget    [invoke site]          §7.6.3
                                   ▼
              PROJECTION ROWS — both tiers, strictest wins
              9 engine rows (§8.1) + host Charge rows                   [invoke site]
```

Read as the ruling's own words: **tier 1 is the whitelist of the known surface; the universal rules are the "signatures and patterns"; the projection rows are the blacklist of known-bad specifics, expressed as thresholds rather than names.**

---

## 8. Classes A, C and D — the pre-allocation policy

The operator called the string half *"trivial to control"*. For class A it is exactly that — three rows. Round-8's classes C and D cost six more rows and one honest mechanism generalization, described below rather than hidden.

### 8.1 The rows

| # | Receiver | Member | Size source | Projection (bytes) |
|---|---|---|---|---|
| 1 | `String` | `PadRight(int[,char])` | int arg 0 | `arg0 × StringCharBytes` |
| 2 | `String` | `PadLeft(int[,char])` | int arg 0 | `arg0 × StringCharBytes` |
| 3 | `String` | `.ctor(char,int)` | int arg **1** | `arg1 × StringCharBytes` |
| 4 | `String` | `Replace(string,string)`, `Replace(string,string,StringComparison)` | receiver + arg lengths | `(receiver.Length / max(1, old.Length)) × new.Length × StringCharBytes` |
| 5 | `String` | `ReplaceLineEndings(string)` | receiver + arg length | `receiver.Length × new.Length × StringCharBytes` (the replaced line ending is ≥ 1 char, so this is the conservative form of row 4) |
| 6 | `String` | `Split(…)` — all 8 overloads | **receiver only** | `receiver.Length × SplitSegmentBytes` |
| 7 | `List<>` | `AddRange(IEnumerable)` | arg's `ICollection.Count` | `count × CollectionElementOverhead` |
| 8 | `List<>` | `InsertRange(int, IEnumerable)` | arg **1**'s `ICollection.Count` | `count × CollectionElementOverhead` |
| 9–13 | `List<>` / `Dictionary<,>` | `.ctor(int)`, `EnsureCapacity(int)`, `Capacity` setter | **existing M4 rows, unchanged** | `max(0, requested − CurrentCapacity) × bytesPerUnit` |

Constants, all existing or derived from existing (A10, #1136 §3 — no new magic number):
- `StringCharBytes = 2` — promoted from the literal `2L` at `VariableSizer.cs:56` and `:62`.
- `SplitSegmentBytes = ObjectHeaderBytes + ReferenceSize = 32` — round-8 measured `Split`'s amplification as per-segment object header plus array slot against a 2-byte-per-char source, i.e. **16× of source bytes**, which `32 bytes per source char` reproduces exactly.
- `CollectionElementOverhead = 8`, `DictionaryEntryOverhead = 24` — unchanged.

`String.ctor(char[])` and the pointer overloads do not match row 3 (the `char[]` is itself an already-charged script value). Rows 4–5 guard `old.Length ≥ 1` (an empty `oldValue` makes .NET throw regardless).

**Row 7/8 and a non-countable argument.** If the argument is an `IEnumerable` that is **not** an `ICollection`, its count cannot be read before enumeration, so the projection is unknowable. Under the ruling — *"it doesn't exist for our script when we can not control it"* — the call is **refused**, with a message naming `.toarray()` as the workaround. This is an intended break (§10.4) and is the one place the ruling costs a real, plausible script shape (`$l.addrange($other.where(...))`).

### 8.2 The four projection shapes — why the table row had to change

Round-8 proved the size source is not one thing:

| Shape | Members | Readable from |
|---|---|---|
| 1. an `int` argument at a named index | rows 1–3, 9–13 | `arguments[i]` |
| 2. an argument's `ICollection.Count` | rows 7–8 | `arguments[i]` |
| 3. arithmetic over receiver length and argument lengths | rows 4–5 | `host` **and** `arguments` |
| 4. the receiver alone | row 6 | `host` |

The existing helper returns `bytesPerUnit` and leaves the arithmetic to the caller (`Math.Max(0, Convert.ToInt64(callparameters[0]) - CurrentCapacity(host)) * bytesPerUnit`, duplicated at three sites). With four shapes, **the caller can no longer perform the arithmetic, because it cannot know which shape applies.** That is the reason for the change — not line count.

### 8.3 The generalization in `VariableSizer` (supersedes the earlier "add an argument index" plan)

1. **One signature, projected bytes out:**
   `internal static bool TryGetPreAllocationOperation(object host, MethodBase member, object[] arguments, out long projectedBytes)`.
   The projection arithmetic moves *inside*; the three method/ctor charge sites each collapse to a uniform two lines:
   ```
   if (VariableSizer.TryGetPreAllocationOperation(host, method, callparameters, out long projected))
       context?.VariableBudget?.ChargePreAllocation(projected);
   ```
   **DRY math (#1267):** the projection block is ~2 lines duplicated at **3** sites today (`MethodOperations.cs:245`, `:296`, and the property variant at `ScriptMember.cs:105`), i.e. `2 × 3 = 6` — *below* the ~15–20 threshold, so line count alone would not justify extraction. The justification is §8.2's: with four shapes the arithmetic is no longer expressible at the call site. The named-helper test passes (`TryGetPreAllocationOperation`, one phrase).
2. **The property-set overload keeps its own shape** (`TryGetPreAllocationOperation(Type, PropertyInfo, out long bytesPerUnit)`) — a property set has a single assigned value and no argument array. Row 11 is unchanged; `ScriptMember.cs:104-107` needs **no edit**.
3. **`StringCharBytes` and `SplitSegmentBytes`** as `internal const`, with `StringCharBytes` replacing the two existing `2L` literals so there is one source of truth.
4. **`CurrentCapacity` needs nothing.** A `string` receiver exposes neither accessor, so it returns 0 (A9). Rows 1–6 are allocate-new operations and charge their full projection; only rows 9–13 subtract a current capacity. **Do not add a "grow vs. allocate-new" flag** — the distinction lives in each row's projection, where it belongs.
5. **Rename.** `TryGetCapacityOperation` → `TryGetPreAllocationOperation`, and the doc comments' *"capacity-operation table"* → *"pre-allocation operation table"*. `internal`, 4 call sites plus tests; leaving the word *Capacity* on a table that now matches `Split` is a name that lies (#6836).

### 8.4 What "an acceptable param" means — decided and justified

**An acceptable size argument is one the memory budget can afford.** Every projection is handed to the **existing** `VariableBudget.ChargePreAllocation` (A11), which applies the single-op ceiling and the accumulation gate and throws `ScriptVariableLimitExceededException(Kind = Bytes)`.

Rejected alternative: **a standalone constant cap** (e.g. "no pad beyond 1e6 chars"). It would introduce a *second* memory-ish threshold beside `MaxVariableBytes`, unrelated to the host's configured budget — a magic number with an indirection layer (#1136 §3) and a mirror of an existing concept (#1136 §6 *parallel constants class*). The budget already expresses what the host means by "how much memory may this script move"; an operation is affordable iff the budget can pay for it. **One coherent budget governs.**

Consequences, all correct and all falling out for free:
- `"a".padright(1000)` → 2 KB → completes. `"a".padright(100000000)` → 200 MB > 128 MiB → clean throw, nothing allocated.
- `while($i<26) { $l.addrange($l) }` → the charge is `Count × 8` on every iteration and refuses at the first doubling that cannot be afforded. The #7870 bypass closes **without** touching the `Measure` cadence.
- A host on `ScriptLimits.None` (`MaxVariableBytes` null) gets `ChargePreAllocation` as a no-op — the documented pre-1.0 unbounded behaviour, unchanged.
- Every projection reconciles with M3's `Measure` at the same per-unit rate the sizer uses, so §8.8.5's charge-once handoff holds verbatim.

### 8.5 Two named conservatisms (not defects)

1. **`Split`'s receiver-derived bound over-charges.** A string is refused for splitting once `receiver.Length × 32 > MaxVariableBytes` — about **4 M characters (8 MB of text) at a 128 MiB budget**, regardless of how few separators it actually contains. Counting separators first would be exact but adds an O(n) scan and a per-member special case; over-charging is the safe direction for a guard (§8.8.4) and it refuses exactly round-8's shape (10 M–30 M-character receivers) while permitting every realistic split. **Named, accepted.** The alternative — denying `Split` outright — is strictly worse and was rejected.
2. **Double-count until the next `Measure`.** When the result is assigned, the pre-allocation charge and M1's `ChargeProducedValue` both land in `producedSinceLastPass` until the next `Measure` resets it — the value counts twice for the *trigger*, never for the authoritative footprint. This is the **existing** behaviour of `new list(n)` (§8.8.5), not something this design introduces. Under a 128 MiB budget a *legitimate* result above ~64 MiB aborts where a strict accountant would allow it. Over-charging can only trip **earlier, never miss**. **Named, accepted, not fixed** (#1136 §4).

---

## 9. Class B — the format policy

### 9.1 The rule

Applied at §6.1 to the **runtime value** of `parameters[0]` when it is a non-null `string`, the method name is `tostring`, and the receiver is ⟲ **assignable to `IFormattable`** (H1, §7.6.2).

> **⟲ Corrected by the §0.2 governance ruling.** The first draft keyed this on a hand-written list of *"the 11 numeric primitives"* (A20). That list is round-8's **measurement of a bare parser** and misses every `IFormattable` a host injects — a host returning a `DateTime`, `TimeSpan`, `Guid`, or its own formattable value type reached `ToString(format)` completely ungoverned, which is precisely the *"tostring which exists everywhere"* the operator named in §0.1. The `IFormattable` predicate reproduces the measured 11-type set on the built-in surface **and** covers the host surface, so this correction makes §9 **shorter**, not longer: one predicate replaces a list.

| | Rule | Bound it establishes |
|---|---|---|
| **R0** | `ToString()` with **no** arguments, or with a first argument that is not a string (e.g. `ToString(IFormatProvider)`), or with a `null` format → **always allowed**, no check. | The operator's *"plain tostring is allowed"*, unconditionally. |
| **R1** | `format.Length > 64` → **deny**. | A custom format string expands to at most `k ×` its own length, so output ≤ low thousands of characters. |
| **R2** | If `format` is a **standard format string** — exactly one letter followed by only digits, `^[A-Za-z][0-9]*$` — and the digit count is **> 2** → **deny**. | Precision ≤ **99**. Every standard specifier at precision ≤ 99 produces at most a few hundred characters for any primitive. |
| **R3** | Otherwise → **allow**. | |

Both rules are needed and neither subsumes the other, and round-8 supplied the measurement for each:

- **R2 catches the standard-specifier amplifier:** `"D100000000"` is 9 characters (passes R1) and demands 1e8 chars — round-8 calls this *~9,000,000×*.
- **R1 catches the custom-format amplifier:** `(1).tostring(new string('0',80000000))` → 817.3 MiB. The format argument is opaque in **two independent ways**, and a rule that only bounded the precision digits would miss this one entirely. (It is now doubly covered: building that 80 M-character format string is itself refused by row 3 of §8.1 — 160 MB > 128 MiB.)

### 9.2 Why precision ≤ 99, and why 64

**99 is not an invented number.** The precision specifier of a .NET standard numeric format string was documented and enforced as **0–99** through .NET Framework. .NET Core 3.0 lifted the cap to `Int32.MaxValue` — *that lift is precisely the amplifier* both enumerations measured. R2 restores the documented historical range. Nothing a script legitimately writes (`F2`, `N0`, `D5`, `X8`, `C2`, `P1`) is lost; a two-digit precision is the entire real-world vocabulary.

**64** is the smallest round bound comfortably above every realistic custom format string: `"yyyy-MM-ddTHH:mm:ss.fffffffzzz"` = 29, `"dddd, dd MMMM yyyy HH:mm:ss"` = 27, `"#,##0.00"` = 8. It is a `const` in `MethodGuard`, not a knob (#1136 §3 — no named operator would tune it, and a host that needs more has §10.2).

### 9.3 Why an allow-list of *shapes*, not an enumeration of *patterns* — validated by round-8

An enumerated pattern list (`"G"`, `"F2"`, `"N0"`, `"X"`, …) built from round-7's specifier set was considered and rejected on design grounds: it needs a per-type table, it buys nothing the shape rules do not already bound, and it answers a narrower question.

**Round-8 then proved the rejection was load-bearing, twice over.** Round-7's amplifying set was `D/F/N/C/E/X`; round-8 added **`P`** (`(1.0).tostring("P90000000")` → 683.7 MiB). A pattern list keyed on round-7's letters would have shipped with a hole on day one. And a pattern list keyed on `[letter][digits]` at all would have missed the custom-format amplifier entirely (§9.1). **The shape rules required no change to absorb either finding** — that is the property being bought.

Equally rejected: **predicting output length from arbitrary format strings.** Round-7's explicit finding: not a table row, it is reimplementing format-length prediction against the full standard + custom .NET grammar, forever, across runtimes.

The rule is two `if`s over a string. Everything outside the two shapes is denied, satisfying *"when in doubt disallow it"*.

### 9.4 All three syntactic forms, and both arities — covered by one check, proven

| # | Form | How it reaches the check | Evidence |
|---|---|---|---|
| a | `(1).tostring("D100000000")` | `ParseMember` emits `ScriptMethod(…, "tostring", [format])` → `ExecuteToken` → `Resolve` | `ScriptParser.cs:901`; `ScriptMethod.cs:122` |
| b | `1:D100000000` (format operator) | **Parse-time rewrite** to `new ScriptMethod(MethodCallResolver, receiver, "ToString", [format, InvariantCulture])` — *the identical node type as (a)* | `ScriptParser.cs:1320-1336`, rewrite at **:1334** |
| c | `$"{1:D100000000}"` (interpolation) | The hole is parsed by `ParseStatementBlock` → `Parse(…)` with `suppressformat = false`, so the `:` inside it hits case (b) and produces the same node. `StringInterpolation.Execute` has **no formatting logic of its own**. | `ScriptParser.cs:782-798`, `:813`; `StringInterpolation.cs:25-27` |
| — | `(1).tostring("D100000000", null)` (2-arg overload) | Same node as (a) with two arguments; the check keys on `parameters.Length >= 1 && parameters[0] is string`, so arity is irrelevant. | A19 (#7868) |

**All three forms converge on one `ScriptMethod` node calling `tostring`, therefore on one `MethodResolver.Resolve` call.** Forms (b) and (c) are sugar for (a), resolved at parse time. One check covers all three and both arities; there is no second interception site to build and no hole. Pinned by T55–T57 and T72.

### 9.5 The runtime-value trap, stated explicitly

The check is on the **value**, never on the source literal, because the format can be built at runtime — all confirmed in #7854: `(1).tostring("D" + "100000000")`, `$w = "D100000000"; (1).tostring($w)`, `$n = 50000000 * 2; (1).tostring("D" + $n)`. A1 guarantees `Resolve` sees the evaluated value; A2 guarantees the check is not short-circuited by the cache on the second and subsequent calls.

**A2 is the trap that makes a naive placement wrong**: a value check placed *after* the cache lookup would validate `(1).tostring("F2")` once and then serve `(1).tostring($evil)` from the cache unchecked, because the cache key stores only the argument *type* `System.String`. The parse-time character class (A7) is likewise **not** a bound and must not be relied on.

---

## 10. Backward compatibility

### 10.1 What the default allow-list contains

§7.3 — roughly **300 allow entries** across the 38 governed types, **generated** from round-8's measured inventory and drift-tested (T68), plus **9 policy member names** carrying the actual logic.

### 10.2 How a host widens

`MethodGuard` is a **per-parser instance**, exposed as `parser.Methods` beside the existing `parser.Types` and `parser.Extensions`, with exactly two host operations:

| Operation | Tier | Meaning |
|---|---|---|
| `Allow<T>(params string[] methodNames)` | **1** | Add names to a tier-1 type's generated inventory. The precise, surgical widening of the whitelist. |
| `Ungovern<T>()` | **1 + 2** | Remove a type from governance entirely — its full public surface returns. The **trusted-host escape hatch**. |
| ⟲ **`Charge<T>(string methodName, int sizeArgIndex, long bytesPerUnit)`** | **2** | Register a **projection row** for a member of the host's own type that is size-parameterized. The host expressing a *rule*, not a list. |
| ⟲ **`Deny<T>(params string[] methodNames)`** | **2** | Refuse members of the host's own type outright. The only mechanism that can reach a **zero-argument host member that allocates unboundedly** (§7.6.5). |

> **⟲ Added by the §0.2 governance ruling (answers its Q3).** The first draft offered only `Allow` and `Ungovern`. Both are **tier-1 shaped**: `Allow` widens an inventory, and a host type has no inventory to widen — *"explicit per-name widening does not scale to host types; a host probably needs to express a rule, not a list."* `Charge` and `Deny` are those rules, and they are the two the surface actually needs:
>
> - **`Charge`** is the tier-2 counterpart of an engine projection row, and it is the *only* way a host can convert H2's one-byte-per-unit guess (§7.6.3) into an exact bound for a member it knows the cost of. **This also resolves `execution-guards-depth-memory.md` §8.8.4 OQ-13**, which deferred a public capacity-registration API as *"YAGNI — no named consumer"*. There is now a named consumer: a host whose registered type has a size-parameterized member, which the §0.2 ruling establishes as the normal case rather than a hypothetical. OQ-13 resolves **in the affirmative**.
> - **`Deny`** exists because §7.6.0's limit is real: a zero-argument allocator is invisible to H1, H2 and H3 alike. Without `Deny`, a host that *knows* about such a member has no recourse but `Ungovern` (which removes all governance) or not registering the type at all. One method, one named case.
>
> `Allow` is deliberately **not** extended to tier 2 — there is no inventory there to add to, and offering it would imply one exists.

All four are explicit host opt-ins. **None is a default**, and there is no global "off" — consistent with the ruling that default-deny wins ties.

**Why per-parser and not `static`, deviating from `TypeGuard` (A12).** `TypeGuard` needs no widening (reflection is never legitimate) and is consulted from six sites including the static compiled path, so `static` is right for it. `MethodGuard` needs widening and has exactly one consultation site (A15). More decisively, `ScriptLimits`'s own doc comment (A13) names the **mixed-host process** as an explicitly served scenario; a process-global widening surface (the `Converter.RegisterConverter` shape, already flagged as a wart in language reference §14) would break it. Wiring: `ScriptParser` constructs `Methods` and passes it to `new MethodResolver(Extensions, Methods)`; the constructor's new parameter is optional and, when omitted, the resolver constructs its **own** default-populated guard — so a directly-constructed `MethodResolver` (e.g. `TypeProvider.cs:18`) is governed too, secure by default, with no shared mutable state.

**`Ungovern`'s named consumer** (#1136 §3's test): the Uberkarl **P1–P3** tiers per #7858 — curated, author-controlled behaviours where the guard is pure friction. P4 (untrusted community scripts) keeps governance on.

**Not tied to `ScriptLimits`.** The allow-list is a containment boundary like `TypeGuard`, not a runtime bound; coupling them would mean `ScriptLimits.None` silently reopens the method surface — a surprising, undocumented coupling. No new `ScriptLimits` knob is added.

### 10.3 Failure mode

A plain, script-catchable **`ScriptRuntimeException`**, mirroring `TypeGuard`'s denials exactly (A12) — **no new exception type** (#1136 §1: no type proliferation for one shared reason; and a denial is not an *abort* — nothing was allocated and a script may legitimately recover). `ScriptMethod.cs:131-133` re-wraps it with the call site, so message and source position both reach the host.

Messages name the blocked thing and the remedy:

- allow-list: *"Method 'foo' on 'String' is not permitted from script (not on the method allow-list); a host may allow it with `parser.Methods.Allow<string>(\"foo\")`"*
- format: *"Format string 'D1000000…' is not permitted (precision above 99); a standard format specifier may carry at most 2 precision digits and a format string may be at most 64 characters"*
- non-countable bulk argument: *"`addrange` requires an argument whose element count is known before enumeration; materialise it first (e.g. `.toarray()`)"*

⚠ **Truncate the echoed format string to 32 characters in the message.** The format argument may itself be up to `MaxVariableBytes` long; a guard that builds a 128 MiB error message defeats itself.

Every pre-allocation charge reuses `ScriptVariableLimitExceededException(Kind = Bytes)` — an existing `ScriptAbortException`, uncatchable from script `try`/`catch`, riding every dispatch wrapper by base type. The asymmetry is deliberate: *"you called something that isn't there"* is an ordinary script error; *"you breached the memory budget"* is an engine abort.

### 10.4 Intended breaks

| Break | Remedy for a host that needs it |
|---|---|
| Any method on a governed type absent from the generated inventory | `parser.Methods.Allow<T>("name")` |
| `tostring` with a standard-format precision > 99, or any format string > 64 chars | none — this is the vector being closed |
| `padleft` / `padright` / `new string(char,n)` / `replace` / `replacelineendings` / `split` / `addrange` / `insertrange` whose projected bytes exceed the budget | raise `MaxVariableBytes`, or `ScriptLimits.None` |
| **`split` on a receiver longer than ~4 M characters**, regardless of separator density (§8.5 conservatism) | raise `MaxVariableBytes` |
| **`addrange` / `insertrange` with a lazy, non-`ICollection` argument** (e.g. the result of `.where(...)`) | materialise first: `.toarray()` |
| A host that constructs its own `MethodResolver` now gets governance | pass a `MethodGuard`, or call `Ungovern` |

**The audit is a test-suite sweep, not a paper exercise:** run `Scripting.Tests` in full; classify every new red as a curation gap (fix the generated inventory) or an intended break (record here).

---

## 11. Alternatives considered and rejected — on record

| Alternative | Why rejected |
|---|---|
| ⟲ **Identity allow-list for host-injected types too** (the first draft's implicit position: govern tier 1 by identity, leave tier 2 ungoverned) | Rejected by the §0.2 ruling and by A21: the closure is closed **only on a bare parser**, so an inventory generated at build time cannot cover a surface created at host-configuration time. *"We can not whitelist every potential (external injected)."* The alternative is not a bigger list — it is a different mechanism (§7.6). |
| ⟲ **Require hosts to enumerate their own type's members** (`Allow<MyType>("getName", "getAge", …)`) | Unusable, and the ruling's Q2 names why: an ordinary domain type has dozens of harmless getters, and a model that makes a host list them by hand will simply be switched off with `Ungovern` — converting a guard into a checkbox. H3's permit-by-shape (§7.6.4) requires the host to write **nothing** for that case. |
| ⟲ **Refuse every tier-2 member with an integral parameter** (the strict reading of "refuse shapes that can amplify") | Structurally sound and practically fatal: `GetCustomer(int id)` is indistinguishable from `Repeat(int n)` by signature, so this refuses the single most common host method shape in existence. H2 (§7.6.3) keeps the refusal but moves it from the *parameter's existence* to the *argument's value*, which is the part the script actually controls. |
| ⟲ **A separate engine-owned deny name-set** for known-bad specifics | Every known-bad specific we can name today is **projectable**, and a projection refuses exactly when unaffordable rather than always (`"a,b".split(",")` keeps working). So the entries all became §8.1 rows and the name-set would ship empty — #1136 §4 delete-check. If a non-projectable engine-level known-bad is ever found, promoting it is a one-line addition; §7.6.5 records the criterion. |
| **A deny-list of the 9 measured amplifiers** (now *possible*, because round-8 enumerated the closure exhaustively) | The tempting one: ~9 deny entries against ~300 allow entries. Rejected on two grounds. **(1) It fails open on a runtime upgrade.** The allow-list refuses a member .NET 10 adds; a deny-list admits it silently — and round-8's own `P` correction is the proof that a snapshot of "the amplifiers" goes stale between two enumerations of the *same* runtime, let alone across one. **(2) It is default-allow**, contradicting the operator's ruling, which is not mine to overturn (#1333). The ~300-entry cost is removed by generating the inventory (§7.3), so the count argument does not survive either. |
| **Extend M4 alone (class A only)** — #7854 HARDENING item 1 | Closes pad/`new string` and leaves B, C and D open. Round-7's own verdict: *"closing Category A does NOT achieve 'no transient OOM'"*. Also default-allow. It is a *component* of this design (§8), not a substitute. |
| **A risk-based carve-out from the governed set** (the earlier draft's exclusion of `List<>`/`Dictionary<,>`/arrays/`object`) | Falsified by round-8 — the single worst finding (#7870) sat inside the carve-out. §7.1. |
| **Post-call result-size ceiling on reflected string/array returns** — #7854 HARDENING item 2(b) | Cheaper, and it bounds the *retained* footprint — but it runs **after** the BCL call has built the value. `"a".padright(2000000000)` is a 4 GB transient and the process is dead when the ceiling looks; round-8 measured the same shape for `replace` (291 MiB already built at throw time). It solves the wrong half, and it cannot distinguish a legitimate large result from an attack. |
| **Memory-capped worker process** — #7854 HARDENING item 2(a) | The only *complete* answer, and it remains the recommendation for genuinely hostile input (§12) — but it is process isolation, out of scope for an in-process library change, and it does not remove the value of making the in-process default sane. Complementary, not competing. |
| **Predict output length from arbitrary format strings** | Round-7's explicit finding: reimplementing format-length prediction against the full .NET grammar, forever, across runtimes. §9.3. |
| **Enumerate allowed format *patterns*** | Would have shipped with a hole for `P` and missed the custom-format amplifier entirely — round-8 proved both. §9.3. |
| **Deny `Split` outright** rather than charging it conservatively | Strictly worse: the conservative receiver-derived bound refuses exactly round-8's shape while permitting every realistic split. §8.5. |
| **Count `Split`'s separators before charging** (exact instead of conservative) | An O(n) scan plus a per-member special case, to avoid over-charging on strings above 4 M characters. Over-charge is the safe direction for a guard (§8.8.4) and the affected shape is not a real use case. |
| **Name-only allow-list checked before the cache** | Cannot distinguish reflected BCL methods from registered extension methods, so it would refuse the entire `EnumerableExtensions` surface. §6.2. |
| **A standalone constant cap on sizes** | A second memory threshold beside `MaxVariableBytes`; a magic number behind an indirection (#1136 §3) and a conceptual mirror of an existing knob (#1136 §6). §8.4. |
| **Put the format check at the invoke site with the charge** | It needs the `MethodGuard` instance inside a `static` `MethodOperations` method with no route to it, and would sit *after* the cache — splitting one guard object across two files for no gain. `Resolve` has the values (A1), runs pre-cache (A2), and needs no budget. |
| **Extend the allow-list to properties and fields** | Round-8 measured zero allocating property or field across the closure, and the one property that *is* a charge site is already an M4 row. Doubles the inventory for no measured benefit. The flag-less-binding contract defect it surfaced is routed separately. §7.5. |
| **Also fix the `Measure` cadence (`VariableBudget.cs:108`) here** | It is one of three causes of #7870 and this design removes a different one, so #7870 closes either way. The cadence is a general `VariableBudget` weakness affecting any future doubling primitive — a different owner, a different PR. §2, OQ-5. |
| **Also guard the compiled `ExpressionBuilder` path** | Only the identity half is implementable there (no argument values, no context), so the result would advertise coverage that does not exist, on a path already contractually forbidden for untrusted code. §2, §12. |

---

## 12. The residual — precisely, not over-claimed

After this change, on a bare `new ScriptParser()`, a script **cannot**: call a method the engine never curated on any of the 38 reachable receiver types; drive any of the nine amplifying members past the byte budget; or drive a `ToString` allocation from a precision specifier or an oversized custom format, in **any** of the three syntactic forms, at either arity, whether the format is a literal or computed at runtime. The #7870 bypass closes.

It **does not** bound, and must not be documented as bounding:

1. **#7869 — the parse-time `StackOverflowException`.** ~3 KB of nested parens kills the process uncatchably before any limit exists. Not a dispatch site. **Routed separately; round-8 ranks it the highest-value fix in the whole enumeration.**
2. **#7871 — uninterruptible ReDoS.** `RegexTimeout` is null in `ScriptLimits.Default`; the backtrack runs inside one native call where no checkpoint and no `CancellationToken` reach. Not a dispatch site. **Routed separately.**
3. **#7868 F6 — no `MaxSteps`/`Timeout` in `Default`.** Unbounded run time and unbounded GC churn behind a small retained value. Interruptible by a host `CancellationToken`; a default-profile question, not a mechanism gap.
4. **The `VariableBudget.Measure` cadence** (`VariableBudget.cs:108`). Fixed cadence growth is a general weakness for any future doubling primitive; this design closes #7870 by a different route and does not repair it. OQ-5. **Owned by `docs/architecture/variable-budget-cadence.md` (DiVoid #7877), which adds an allocation-denominated growth trigger to `VariableBudget.Observe` alongside the tick cadence.**
5. **The compiled path** (`ParseDelegate` / `ExpressionBuilder`). No guard of any kind, unchanged. The existing contract stands: *never run untrusted code through the compiled path.*
6. **Allocation inside an arbitrary host method.** A host type registered via `AddType`, or returned from a host method, exposes its own public surface. §8.8.7's residual — **narrowed, not removed**, by tier 2: H1 now governs its `IFormattable.ToString(format)`, H2 bounds its integral arguments, and `Charge`/`Deny` let the host tighten it further. What remains is below.
10. **A zero-argument host member that allocates unboundedly** (`Report.Render()`). Invisible to H1, H2 and H3 by construction — there is no argument to inspect and no signature that distinguishes it from a cheap getter. The host's `Deny<T>()` is the only mechanism, and only the host knows to use it (§7.6.0, §7.6.5).
11. **A host member whose per-unit cost exceeds one byte, at an argument value within the budget.** H2 assumes the minimum plausible cost, so `MakeBuffers(100000000)` at 100 bytes/unit passes H2 and allocates far beyond the budget. The host's recourse is `Charge<T>("makeBuffers", 0, 100)`, which turns the guess into an exact row (§7.6.3).
12. **A host member with a `string` parameter it interprets as a size-bearing pattern** — the tier-2 analogue of the format amplifier, on a member H1 does not match because the type is not `IFormattable`. The output is bounded by `k × MaxVariableBytes` for an unknown `k` (the pattern is itself a script value M1 already charged), which is a bound but not a small one. `Charge`/`Deny` are the recourse.
7. **A host-supplied `ITypeInstanceProvider`** whose `Create` allocates without consulting the budget.
8. **Governance a host has explicitly removed** via `Ungovern` — by definition.
9. **The `ScriptMember` flag-less property/field binding**, which reaches static members and contradicts the documented no-static-members contract. No allocation vector today; routed as its own item (§7.5, OQ-4).

**The non-over-claiming line for the language reference** (replacing the corresponding clause in §12 of `docs/pooscript-language-reference.md`, currently at line 231):

> *The reflected-method guard closes the reflected BCL calls that allocate from a size, format or bulk argument: on the engine's own reachable types, only curated methods dispatch; `PadLeft`/`PadRight`/`new string(char,n)`, `Replace`/`ReplaceLineEndings`/`Split`, and `List.AddRange`/`InsertRange` are charged against the byte budget before anything is built; and `ToString` with a format argument accepts only a standard specifier of at most two precision digits or a custom format of at most 64 characters — in the method form, the `:` operator and interpolation alike, at either arity, on the runtime value of the format. It does not bound allocation inside a host-registered type's own methods, it does not apply to the compiled path, and it does not address parse-time recursion or regex backtracking, which are guarded elsewhere. Process or container isolation remains the only complete answer for a genuinely hostile author.*

---

## 13. Round-8 (#7868) — what landed, and what it changed

Round-8 answered *"first check whether you find some more"* with a mechanical closure computation rather than probing. It is folded in throughout; this section is the ledger.

**What it confirmed (no design change):**
- Class A is exactly the three members round-7 named — mechanically complete.
- The static BCL surface is already closed by `Public | Instance` binding (A17). A whole hypothesised ring is empty.
- `DateTime` / `TimeSpan` / `Guid` / `StringBuilder` / `Regex` are **unreachable** on a bare parser (A16) — the "which types have a dangerous `ToString(format)`" question is moot.
- The escape boundary is unregressed. M4's capacity rows work perfectly (`list.capacity=1e8` → throws with `allocDelta = 0.0 MiB`). String doubling by `+` is caught at ~2× the limit.

**What it corrected in this design — each marked ⟲ in place:**

| Round-8 finding | What changed here |
|---|---|
| **F1 / #7870** — `List.AddRange`/`InsertRange` is an outright **guard bypass** | §7.1: the `List<>`/`Dictionary<,>`/array carve-out is **removed** and the governed set is now the full 38-type closure. §8.1 rows 7–8. The carve-out's failure is analysed rather than quietly patched. |
| **F2** — `Replace`/`ReplaceLineEndings` amplify by an **attacker-chosen** factor (1000× measured); round-7's *"source-bounded 2×"* was an under-report | §7.4: my provisional *"allow, no policy"* call is **overturned**. §8.1 rows 4–5. |
| **F3** — `Split` amplifies 9–16× with **no size argument**; the count cap does not bound the transient | §8.1 row 6, and §8.2/§8.3: **the mechanism generalizes** — the table row becomes a projection over `(receiver, member, arguments)` because a permit decision here is not derivable from arguments at all. A18 confirms the receiver is already in scope. |
| Category B is exactly the **11 numeric primitives**, not `char`/`bool`/`string`/`TypeCode` | §6.1 and §9.1 key on `IsFormatFamily` (11 types), and A20 records why. |
| **`P` amplifies too**; `R`/`G` do not | No change required — §9.3's shape rule absorbed it, which is the retrospective validation of rejecting a pattern list. |
| `ToString(format, IFormatProvider)` is reachable with a literal `null` | A19; §6.1 condition 3 and §9.4's fourth row make both arities explicit. |
| **Custom** format strings drive width independently (`new string('0',80000000)` → 817 MiB) | §9.1 R1 already covered it; now stated explicitly as the non-obvious case, with the double-coverage note. |
| Flag-less `GetProperties()`/`GetFields()` reaches static members | §7.5: the method-only scope is now an **explicit, reasoned decision** rather than an omission, and the contract defect is routed as its own item. |
| **F4 (#7869)** parse-time stack overflow, **F5 (#7871)** ReDoS, **F6** default profile | §2 and §12: named, out of scope, designed-for by nobody here — so the document does not read as closing the whole class. |
| Measured allow-list inventory (102 / 12 / 9 / 10 / 51 …) | §7.3: the inventory is **generated** from it and drift-tested, which is what makes ~300 entries maintainable. |

**Nothing in round-8 weakened default-deny.** F3 (`Split`) is the strongest argument for it yet produced: a permit decision that cannot be derived from arguments must default to refuse.

---

## 14. Test plan

`Scripting.Tests/ExecutionGuardTests.cs` (guard mechanics) and `Scripting.Tests/SecureByDefaultTests.cs` (bare-default repros), following established conventions: `[Test, Parallelizable]`, `[MaxTime(2000)]`, a `[Description(...)]` citing this document's section and the DiVoid id, `T<NN>_<Behavior>` naming, and — for every pre-allocation claim — the existing `MaxPreAllocationGuardDeltaBytes` allocation-delta assertion. Payloads bounded to round-8's measured sizes; **never the 40-iteration `addrange` extrapolation (16 GiB)**.

| # | Test | Asserts |
|---|---|---|
| T50 | `$s = "a".padright(100000000)` | `ScriptVariableLimitExceededException`, `Kind == Bytes`, **allocation delta < a few MB** — the load-bearing proof the throw preceded `method.Invoke`. |
| T51 | `"a".padleft(100000000)`, `"a".padright(100000000,'b')`, **unassigned** | same — round-7's unassigned expressions must now throw, not complete. |
| T52 | `new string('a',100000000)` | same; pins the **arg-index-1** row through `TypeInstanceProvider.Create`. |
| T53 | `"a".padright(1000)`, `"ab".padleft(10,'x')`, `new string('a',1000)` | **all complete, no false abort**. |
| T54 | `(1).tostring("D100000000")`, `(1.0).tostring("F100000000")`, `(1).tostring("N50000000")`, `(1).tostring("X100000000")`, **`(1.0).tostring("P90000000")`** | `ScriptRuntimeException`; allocation delta < a few MB. The `P` case is round-8's addition. |
| T55 | `1:D100000000` (format operator) | same — pins form (b). |
| T56 | `$"{1:D100000000}"` (interpolation) | same — pins form (c). **A guard covering only the method call would leave this green-and-wrong.** |
| T57 | `$n = 50000000 * 2; (1).tostring("D" + $n)` and `$w = "D100000000"; (1).tostring($w)` | same — pins that the check is on the **runtime value**. |
| T58 | `(1).tostring(...)` twice in one script, benign format then hostile | pins the **pre-cache** placement (A2): a post-cache check would let the second through. |
| T59 | `(1.5).tostring()`, `(1.5).tostring("F2")`, `$"{1234.5:N0}"`, `(255).tostring("X8")`, `1:F2`, `(1).tostring("G")`, `(1.0).tostring("R")` | **all complete** — the everyday vocabulary is untouched. |
| **T68** | **Allow-list drift**: re-run the §7.3 derivation against the running runtime and compare to the checked-in table | fails if the runtime's member surface has changed — makes the fail-closed property observable rather than silent. |
| **T69** | **#7870 repro**: `$l=new list(); $l.add(1); $i=0; while($i<26) { $l.addrange($l); $i=$i+1 }` | `ScriptVariableLimitExceededException`, `Kind == Bytes`, allocation delta bounded — **the bypass is closed**. Control: the linear `add` loop still throws as it does today. |
| **T70** | **#7868 F2 repro**: `$r=new string('b',1000); (new string('a',200000)).replace("a",$r)` and the `replacelineendings` variant | throws before allocation; `$s.replace("a","bb")` on a small string still completes. |
| **T71** | **#7868 F3 repro**: `(new string(',',20000000)).split(",")`, the `char` overload, and `split(",",5)` (the count cap that does *not* bound the transient) | all three throw before allocation; `"a,b,c".split(",")` completes. |
| **T72** | `(1).tostring("D100000000", null)` — the 2-arg overload reachable with a literal null (A19) | throws — pins that the check is arity-independent. |
| **T73** | `(1).tostring(new string('0',80000000))` — the custom-format amplifier | throws (and note the format string itself is separately refused by row 3). |
| **T74** | `$l.addrange($other.where($x => true))` — a lazy, non-`ICollection` argument | `ScriptRuntimeException` naming `.toarray()` — pins the §8.1 refusal as an intended, discoverable break. |
| T60 | A governed-type method absent from the inventory | `ScriptRuntimeException` naming the method **and** the `parser.Methods.Allow<T>(...)` remedy. |
| T61 | `parser.Methods.Allow<string>(...)` then the same call | completes — widening works. |
| T62 | `parser.Methods.Ungovern<double>(); (1.0).tostring("F100000000")` (bounded) | completes — the escape hatch is real. |
| T63 | Two parsers in one process, one `Ungovern`ed | the widening does **not** leak — pins per-parser isolation (A13). |
| T64 | `$l.where(...)`, `$l.order(...)`, the common `EnumerableExtensions` surface | **all complete** — pins that extension methods bypass the allow-list (§6.2). Highest-value regression test in the set. |
| T65 | `SandboxEscapeTests` | unchanged green — the escape boundary is untouched. |
| T66 | Direct `MethodGuard` unit pins on `IsAcceptableFormat`: `""`, `"G"`, `"F2"`, `"D99"`, `"D100"`, `"P90000000"`, `"X8"`, a 64-char custom format, a 65-char custom format, `null` | the two shape rules, exhaustively, without the engine in the loop. |
| T67 | `(1).tostring(new string('D',100000))` | throws, and the exception **message length is bounded** (< 512 chars) — pins the §10.3 truncation. |
| **T75** | **`IFormattable` family pin** (A22): assert on the running TFM which of `String`/`Char`/`Boolean`/the 11 numerics/`DateTime`/`TimeSpan`/`Guid` satisfy `IFormattable.IsAssignableFrom`, **and** that for any that do without a public `ToString(string,…)` overload, the script call fails with "method not found" rather than a format denial | pins the predicate against a TFM change instead of asserting a reflection fact in prose. |
| **T76** | Host registers a type with a `DateTime` property; script calls `$h.when.tostring("D100000000")` | throws — pins **H1 on the host surface** (§7.6.2). Under the first draft's 11-type list this was green-and-wrong. |
| **T77** | Host registers a type with `Repeat(int n)` returning a string; script calls `$h.repeat(2000000000)` | throws before the call — pins **H2** (§7.6.3). Control: `$h.repeat(1000)` completes. |
| **T78** | Host registers an ordinary domain type with ~10 zero-arg getters, `SetName(string)`, `Find(MyFilter)`, `GetById(int)` with a normal id; script exercises all of them with **no** `Allow`/`Charge`/`Deny` calls | **all complete** — pins **H3 permit-by-shape** (§7.6.4). This is the usability test the ruling's Q2 demanded; if it fails, the model is unusable and must be bounced. |
| **T79** | `parser.Methods.Charge<MyType>("makeBuffers", 0, 100)`; script calls `$h.makebuffers(100000000)` | throws with `Kind == Bytes` and a bounded allocation delta — pins the host `Charge` row overriding H2's one-byte assumption (§10.2, residual 11). |
| **T80** | `parser.Methods.Deny<MyType>("render")`; script calls `$h.render()` | `ScriptRuntimeException` — pins `Deny` reaching the zero-argument allocator no shape rule can see (§7.6.5). |
| **T81** | Two parsers, one with `Charge`/`Deny` on the same host type, one without | the tier-2 rules do **not** leak between parsers — the per-parser property (A13) holds for the new APIs too. |

**Bounce this design if** T50–T57 or T69–T73 cannot be made to abort with an allocation delta in the low-MB range. A large delta means the throw fired *after* the allocation — the exact failure this document exists to prevent.

---

## 15. Implementation Guidance — ordered units

**One feature per PR** (#1165 / global PR-scope rule). Two units, in dependency order; Unit A is independently valuable and independently shippable, and it alone closes #7870.

### Unit A — the pre-allocation policy: nine rows and the projection generalization

*Closes classes A, C and D. No new class, no new concept, no public surface change.*

1. `VariableSizer`: promote the `2L` literals (`:56`, `:62`) to `internal const long StringCharBytes = 2`; add `internal const long SplitSegmentBytes = ObjectHeaderBytes + ReferenceSize`.
2. `VariableSizer`: rename `TryGetCapacityOperation` → `TryGetPreAllocationOperation` (both overloads, `internal`; 4 call sites + tests); update the "capacity-operation table" wording.
3. `VariableSizer`: change the method/ctor overload to `TryGetPreAllocationOperation(object host, MethodBase member, object[] arguments, out long projectedBytes)` and implement the nine rows of §8.1 with their four projection shapes (§8.2). The existing capacity rows keep their `max(0, requested − CurrentCapacity)` delta; the new rows charge their full projection.
4. Collapse `MethodOperations.cs:244-245`, `MethodOperations.cs:295-298` and `TypeInstanceProvider.cs:37-38` to the uniform two-line call (§8.3).
5. Leave `ScriptMember.cs:104-107` **unchanged** — the property-set overload keeps its own shape.
6. Refuse a non-`ICollection` bulk argument on rows 7–8 with the §10.3 message.
7. Tests T50–T53, T69–T71, T74; re-run T41–T48 unchanged.

### Unit B — the `MethodGuard`: allow-list + format policy

1. Write the **generator** (§7.3) as a test-project utility: compute the tier-1 type closure, enumerate `Public | Instance` methods per type, lower-case, distinct, subtract the 9 policy names, emit the literal table. Run it once, check the output into `MethodGuard.cs` as `static readonly` data. **Tier 1 only** — there is nothing to generate for tier 2.
2. New `Pooshit.Scripts/Parser/Resolvers/MethodGuard.cs`, styled on `TypeGuard.cs`:
   - **tier 1:** the closure type set, the generated per-type name sets, `IsTier1`, `IsAllowed`.
   - **universal:** `IsFormatFamily` = `typeof(IFormattable).IsAssignableFrom(t)` (§7.6.2 — *not* a hand-list), `IsAcceptableFormat`, `MaxFormatLength = 64` and `MaxPrecisionDigits = 2` as `const`.
   - **tier 2:** `IsPermittedByShape(member, arguments)` implementing H3 (§7.6.4) over `member.GetParameters()`, and the per-parser host rule sets backing `Charge` / `Deny`.
   - **host API:** `Allow`, `Ungovern`, `Charge`, `Deny` (§10.2).
   Names matched **lower-cased ordinal** (values already lowered by `ScriptMethod.cs:27`).
3. `MethodResolver`: optional second constructor parameter (`MethodGuard methods = null`, falling back to a **new** instance — never a shared one, §10.2); the format check (H1) after `:51` and **before** `:53`; the tier-1 allow-list / tier-2 shape verdict after the winner at `:104` and **before** the cache write at `:107`. **Both tiers decide at the same two points** — the branch is on the receiver, not on the site.
4. `VariableBudget`: add the non-accumulating ceiling method H2 needs (A23), extracted from `ChargePreAllocation`'s existing first check so there is one comparison, not two.
5. `MethodOperations.CallMethod`: after the projection charge, apply **H2** to each integral argument **only when no projection row matched** (§7.6.3). `Charge`-registered host rows participate in the projection lookup, so a host row suppresses H2 for that member automatically.
6. `ScriptParser`: construct `Methods` and pass it to `new MethodResolver(Extensions, Methods)` at `:42`; expose `public MethodGuard Methods { get; }` beside `Types`/`Extensions`.
7. Message shaping per §10.3, **including the 32-character format truncation**.
8. Tests T54–T68, T72–T73, **T75–T81**. **T78 is the gate**: if an ordinary host domain type does not work with zero host configuration, bounce the design.
7. **Run the full `Scripting.Tests` suite** and classify every new red per §10.4 — this *is* the backward-compat audit.
8. Docs, same PR: replace the residual clause at `docs/pooscript-language-reference.md:231` with §12's text; add the guard and `parser.Methods` to §12's guard list; add the intended breaks to §14 *Known limitations*; cross-reference `docs/architecture/execution-guards-depth-memory.md` §8.8.7 with which residual bullets this closes.

### Do **not**

- Design or fix **#7869** (parse-time stack overflow), **#7871** (ReDoS / `RegexTimeout` default), F6 (`MaxSteps`/`Timeout` defaults), or the `VariableBudget.Measure` cadence (§2, §12). They are routed separately.
- Touch `ExpressionBuilder` / the compiled path.
- Change `ScriptMember`'s binding flags in this PR (§7.5 — file it separately).
- Add a `ScriptLimits` knob, a new exception type, a new budget, or a second table.
- Hand-write the allow inventory (§7.3 — generate it).
- Build a format-length predictor or a per-type format-pattern table (§9.3).

---

## 16. Open Questions

| # | Question | My recommendation | Gating? |
|---|---|---|---|
| **OQ-1** ✅ *resolved* | ~~Allow-list `replace`/`split` or deny them?~~ | **Resolved by round-8 data:** both amplify by attacker-chosen / receiver-driven factors; both are now charged rows (§8.1 rows 4–6). No operator decision needed. | No |
| **OQ-2** | Should an allow-list denial be a **new** exception type (`ScriptMethodNotPermittedException`) so a host can detect it programmatically, rather than reusing `ScriptRuntimeException`? | **Reuse `ScriptRuntimeException`**, mirroring `TypeGuard`. A new type for one shared reason is proliferation (#1136 §1). Flagged because a host doing telemetry on blocked methods might want it. | No |
| **OQ-3** | Is `Ungovern<T>()` wanted, or should a trusted host enumerate names via `Allow`? | **Keep it**, with Uberkarl P1–P3 (#7858) as the named consumer (#1136 §3). | No — but its delete-check depends on the operator agreeing the tier is real. |
| **OQ-4** ✅ *resolved* | `ScriptMember.cs:63/74` binds property/field lookup **flag-lessly** and so reaches static members, contradicting the documented no-static-members contract and #7793's `Public \| Instance` decision. No allocation vector (round-8), but it is a real contract defect. | **Resolved:** fixed by PR #26 (`355354c`, merged 2026-08-17), which bound `ScriptMember.cs:63/74/146/150` and `MethodResolver.cs:60` to `Public \| Instance`. | No — resolved. |
| **OQ-5** ✅ *resolved* | The `VariableBudget.Measure` cadence (`VariableBudget.cs:108`, `nextMeasureAt = ticks + max(256, units)`) is one of three causes of #7870. This design removes a different cause, so #7870 closes — but the cadence weakness remains for any future doubling primitive. Who owns it? | **Resolved:** owned by `docs/architecture/variable-budget-cadence.md` (DiVoid #7877), which adds an allocation-denominated growth trigger to `VariableBudget.Observe` — a second, independent trigger alongside the unchanged tick cadence, forcing a measurement once the process has allocated `MaxVariableBytes` since the last pass regardless of checkpoint count. | No — resolved, filed as its own PR per the recommendation below. |
| **OQ-6** | `split` on a receiver longer than ~4 M characters is refused regardless of separator density (§8.5), and `addrange` with a lazy non-`ICollection` argument is refused (§8.1). Both are conservative-direction breaks that a real script could hit. Accept? | **Accept.** Both have a one-call workaround (`raise MaxVariableBytes` / `.toarray()`), both refuse exactly round-8's measured shapes, and the exact alternatives (separator scan, eager enumeration) cost more than they buy. | No — but the operator may want to know these are the two breaks most likely to be noticed. |
| **OQ-7** ⟲ *new* | **H2's ceiling is `MaxVariableBytes` — the minimum-plausible-cost (1 byte/unit) assumption.** A stricter divisor (say `budget / 8`) would catch more host amplifiers but starts refusing large-but-legitimate integral arguments (ids, timestamps, ticks). Is 1 byte/unit the right assumption, or should the ceiling be tighter by default? | **Keep 1 byte/unit.** It is the only divisor that is *provably* never wrong in the refusing direction — anything larger refuses a value that some member could legitimately consume. A host that knows the real cost has `Charge<T>()`, which is exact rather than a better guess. Residual 11 records what this leaves open. | No — but it is the one tier-2 number the operator may want to overrule. |
| **OQ-8** ⟲ *new* | **§7.6.5 does not build a separate engine-owned deny name-set**, because every known-bad specific we can name today is projectable and became a §8.1 row instead. The §0.2 ruling explicitly asks for a blacklist. Is "the blacklist is the row table" an acceptable reading of the ruling? | **Yes, and it is strictly better** — a threshold refuses `split` on a 20 M-char receiver while permitting `"a,b".split(",")`, where a name would refuse both. But this is me interpreting the ruling rather than following it literally, so it is flagged rather than assumed. If the operator wants a literal name-set, it is a small addition; the criterion for entries is in §7.6.5. | No — but it is the one place I read the ruling rather than executed it. |

---

## 17. Pre-Design Checklist (#1136 §5) — answered in order

**KISS / DRY / YAGNI**

- **No new type mirroring an existing one.** One new class (`MethodGuard`), modelled on `TypeGuard` and doing something `TypeGuard` does not. Classes A/C/D add **no** type — nine rows in the table M4 already owns. Reusing `ScriptVariableLimitExceededException` and `ScriptRuntimeException` means **no new exception type**. A second parallel "member → projected bytes" table was explicitly avoided (#1136 §6 *parallel constants class*).
- **No abstraction with one implementation.** `MethodGuard` is a concrete class; no interface, no provider, no factory, no registry.
- **No element justified by "we might need X later."** Every row in §8.1 corresponds to a **measured** round-7 or round-8 payload. The generator and drift test exist because a 300-name table is otherwise unmaintainable — a present cost, not a future one. `Ungovern`'s consumer is named (Uberkarl P1–P3).
- **No deprecation period, feature flag, compatibility shim or transition window.** Governance is on from the first commit; widening is a host call, not a migration mode.
- **DRY / KISS math, every overriding decision:**
  - The projection extraction (§8.3) is `2 lines × 3 sites = 6`, **below** the #1267 threshold — so it is justified **not** by line count but by §8.2: with four projection shapes the caller cannot know which arithmetic applies. Stated as math plus the structural reason, not a paraphrase (#1333).
  - The governed-set carve-out was removed because round-8 falsified it; the replacement cost (~300 entries) is neutralized by generation, so the KISS objection to the criterion no longer holds (§7.1, §7.3).
  - The deny-list alternative is priced explicitly — ~9 entries vs ~300 — and rejected on fail-open-on-upgrade plus the ruling, not on taste (§11).
  - `StringCharBytes`: the literal `2L` exists at 2 sites and a 3rd was being added → one `const` (#1136 §3). `SplitSegmentBytes` is derived from two existing consts, not a new number.
  - **The §0.2 revision is net-simplifying where it touches existing text.** §9.1's hand-written 11-type family collapses to one `IFormattable` predicate — a list replaced by a rule, covering strictly more (§7.6.2). Tier 2 adds **no** inventory, **no** new interception site, and reuses the `(receiver, member, arguments)` vocabulary `Split` already forced (§7.6.1). New surface: three rules (H1 replaces a list, H2, H3), two host methods (`Charge`, `Deny` — each with a named consumer), one `VariableBudget` method (A23). Each passed the delete-check: without H1 a host's `DateTime.ToString(format)` is ungoverned (T76); without H2 a host `Repeat(2e9)` is unbounded (T77); without H3 an ordinary domain type needs hand-listing and the guard gets switched off (T78); without `Deny` a zero-arg host allocator has no recourse (§7.6.5); without `Charge` H2's guess cannot be made exact (residual 11).
  - **Deliberately not built** in the revision: a separate engine deny name-set (empty today — §7.6.5, OQ-8), speculative rows for BCL types a host *might* register (no named consumer, #1136 §3), and `Allow` for tier 2 (there is no inventory to add to, and offering it would imply one exists).

**Existing systems first**

- Audited: `TypeGuard` (position, style, exception, cache ordering — mirrored, with the one deviation justified in §10.2), M4 (`VariableSizer` table + `ChargePreAllocation` + the four charge sites — extended, not duplicated), `ScriptLimits` (deliberately **not** extended), `AddType`/`AddExtensions` (the widening idiom — mirrored as `parser.Methods`), the exception hierarchy (reused), `MethodOperations`' existing receiver access (A18 — no new plumbing for `Split`).
- **New surface justified concretely:** `MethodGuard` cannot live on `TypeGuard` (static, non-widenable, semantically about the reflection family) nor on `VariableSizer` (which knows bytes, not allow-lists). Different **security boundary**, different **lifecycle** (per-parser, host-mutable) — #1136 §2's bar, met and named.
- **No new persisted data.** Not applicable (library, no schema).
- **Consumer chain recursed:** `parser.Methods` has a named consumer (a host widening after a break); `Ungovern` has a named consumer (Uberkarl P1–P3); the drift test has a named consumer (the maintainer on a runtime upgrade).

**Configurability**

- **No new `ScriptLimits` knob**, and no knob at all: `MaxFormatLength = 64`, `MaxPrecisionDigits = 2`, `SplitSegmentBytes` are `const` engine opinion (#1136 §3). None has a named operator who would tune it; a host that needs more has §10.2.
- `parser.Methods` is not a tuning knob — it is the backward-compatibility escape hatch #7856 explicitly requires, with named consumers.
- No telemetry-then-tune compound.

**Less is better**

- **Deleted from this design:** guarding the compiled path; a constructor allow-list; a property/field allow-list (§7.5, with round-8's zero-vector measurement as the delete-check); an enumerated format-pattern table (§9.3, twice-validated); a format-length predictor; a standalone size cap (§8.4); a "grow vs. allocate-new" table field (§8.3); a separator-counting scan for `Split` (§11); a new exception type; a global off-switch; a `ScriptLimits` coupling; a hand-written allow inventory (§7.3); the second interception site the three syntactic forms *appeared* to need (A6/§9.4); the `Measure` cadence fix (§2 — a different owner); #7869/#7871/F6 (§2 — different mechanisms entirely).
- **Trade-offs named with their arithmetic:** §7.1 (why the carve-out failed and what replaces it); §8.5 (the 4 M-character `Split` ceiling and the ~64 MiB double-count, both with numbers); §9.2 (99 = the documented pre-.NET-Core-3.0 range; 64 vs. the 29-character worst real format); §10.2 (per-parser vs. static, decided by A13); §11 (~9 vs ~300 entries).
- **Radical-clean where nothing is consumed:** the compiled-path guard has no permitted consumer → not built at all, rather than half-built.

**Document discipline**

- Cites #114 and #1136 as load-bearing (header); operator quote verbatim (§0).
- Scope **and** non-scope explicit (§2), including the three round-8 findings this design deliberately does not touch.
- Residual precise and non-over-claiming (§12), with the exact language-reference sentence supplied.
- **Every reversal is marked ⟲ and analysed in place** (§7.1, §7.4, §13) rather than left as a stale claim beside a correction — the #1136 §6 *superseded-design-left-live* anti-pattern applied at paragraph granularity.
- **Nothing superseded.** This extends §8.8 of `execution-guards-depth-memory.md` and sits beside `type-access-boundary.md`; both stay live and both get a cross-reference in the same PR.

---

## 18. Audit

| Item | ✓ | Location |
|---|---|---|
| Operator's verbatim quote in the doc | ✓ | §0 |
| Pre-Design Checklist (#1136 §5) answered in order as doc sections | ✓ | §17 |
| Every principle-overriding decision states its math (or the conflict was surfaced) | ✓ | §7.1, §8.3 (`2 × 3 = 6` + the structural reason), §8.5, §9.2, §11 (9 vs 300), §17 |
| In-scope and out-of-scope both named | ✓ | §2 (including #7869 / #7871 / F6 / the cadence) |
| Interception point named as `file:symbol`, proven to fire before allocation | ✓ | §6.1 (`MethodResolver.cs → MethodResolver.Resolve`, proof via `ScriptMethod.cs:122-123`), §6.3 (the four charge sites with their invoke lines) |
| All three syntactic forms reaching `ToString(format)` covered — and both arities | ✓ | §9.4 with file:line evidence; T55/T56/T72 |
| Backward-compat story concrete (default list, widening, failure mode) | ✓ | §10.1–§10.4 |
| Readable as John with no build-blocking gaps | ✓ | §15 (ordered units, exact files, exact lines, explicit do-nots) |
| Alternatives rejected on record | ✓ | §11 |
| Round-8 folded in as data, with every reversal marked and analysed | ✓ | §13 ledger; ⟲ marks at §1, §7.1, §7.4 |
| **§0.2 governance ruling applied; superseded reasoning marked in place per #6760, not deleted** | ✓ | ⟲ marks at §2 (host types now in scope), §4, §7.1 (the tier-1/tier-2 split), §9.1 (the 11-type list → `IFormattable`), §10.2 (the two new host operations), §11 (four newly-rejected alternatives) |
| **The ruling's four questions answered explicitly** | ✓ | Q1 vocabulary §7.6.1 · Q2 default + fifty getters §7.6.4 (+ gate test T78) · Q3 `Allow`/`Ungovern` → `Charge`/`Deny` §10.2 · Q4 blacklist ownership §7.6.5 |
| Design does not claim to close what it cannot reach | ✓ | §2, §12 items 1–4 and **10–12** (the tier-2 residuals the shape rules cannot see) |
| Round-8-settled decisions not re-opened | ✓ | interception points (§6), pre-cache format check (§6.1), receiver-derived `Split` bound (§8.1 row 6) all unchanged |
| Filed to working tree and DiVoid | ✓ | header pointer + DiVoid #7872 |
