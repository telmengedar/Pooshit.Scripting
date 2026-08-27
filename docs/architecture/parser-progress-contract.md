# Architectural Document: The Parser Progress Contract

> **Repo working-tree path:** `C:\dev\claude\Pooshit.Scripting\docs\architecture\parser-progress-contract.md`
> **DiVoid:** documentation node **#9432**, linked to task **#9426**, project **#5**, diagnosis **#9341**, QA adjudication **#9397**, driver bug **#9350**, doc-polish task **#9412** (superseded — §12), repo map **#2676**, sibling defect **#2901** (§11).
> **Standards:** Design Contracts **#1136** — §1 (KISS/DRY/YAGNI) load-bearing, §5 Pre-Design Checklist walked in §15. Code Contracts **#114 §0** governs the implementation. Test-impossible exception: **#275**.
> **Baseline:** `master` @ `7a51a66`. Every number in this document was measured on that head, not inferred.
> **Status:** design only. No branch, no PR. Implementer: `john-backend-dev`.
>
> **Revision 2 — 2026-08-24.** Amended after Toni's RULING on #9426. **F1 is DECIDED: tighten.** `)` and `]` stop being interchangeable closers and `ParseParameters` folds into `ParseTokenList`. This closes the §8 deviation revision 1 declared, dissolves revision 1's second open question, and makes the change a **net reduction** of the parser (−25 production lines, one fewer construct loop, one fewer method). §7.4, §8, §10 and §13 are rewritten; §4.6–§4.8 are new. Revision 1's rejections of directions 1, 2, 3 and 5 stand unchanged.

---

## 0. The operator statement this design is built to satisfy

> *"that seems like a fix for the symptom, not for the cause. We are talking about a parser which freezes. That should be pretty much a design failure. A parser reads text and has to progress the pointer, translating the text in the process. A freeze here probably means the pointer is not progressed under certain circumstances. Now there is a bit of state and peeking and whatnot involved, so the issue surely is not super straightforward, but just saying - parsing has a timeout and thus can not freeze anymore is a hack and not a fix - even though a timeout is okay as a last resort for unknown issues."*
> — Toni, 2026-08-24

Two clauses bound this design.

**"The pointer is not progressed under certain circumstances"** is the defect statement, and it is precise. This document treats it literally: the unit of the invariant is the *pointer advancing*, and the place it can be violated is *a loop that repeats a call which advanced it by zero*.

**"A timeout is okay as a last resort for unknown issues"** is explicit permission to keep `ScriptLimits.ParseTimeout`. It stays, default-on, unchanged. What changes is its job description: it must stop being the thing that prevents freezes we already know how to prevent, and go back to being the backstop for the ones we have not foreseen.

---

## 1. Problem Statement

`ScriptParser` is a recursive-descent parser over a `ref int index` cursor. Six of its nine recursive `Parse(` call sites sit inside loops. Each such loop is only live — guaranteed to terminate — if every iteration advances `index`. Nothing in the type system, the compiler, or any shared construct expresses that obligation. It exists only in whatever each loop's author remembered.

PR #30 closed the two instances that had been found, by writing the same before/after progress check at three sites and adding a wall-clock parse deadline for the sites nobody had looked at. That is one defect fixed three times plus a net thrown over the remainder. The remaining structural gaps are:

1. **`ParseParameters` (`ScriptParser.cs:1002-1027`) has no bound on progress at all.** QA #9397 upheld it as *"structurally unreachable"*, correctly. But "unreachable" is a property of today's grammar, held together by a coincidence between two `switch` statements in different methods plus a backstop in a third. Nothing pins it.
2. **`ParseDictionary` (`:1601`) is the parser's only `while(true)`** — it carries no range bound in its header at all, and survives only because `ParseDictionaryKey` happens to return `null` at end of input.
3. **Nothing in the repository asserts termination as a standing property.** #9341 named this as the reason 893 tests and an 8/8 headless probe all missed an infinite loop: *"every test in the suite calls a validator that returns. A test process cannot distinguish 'returns an error' from 'never returns' unless it is written with a timeout."*

**Success criteria.** (a) No construct loop's liveness depends on a fact stated in another method. (b) A *seventh* construct loop cannot reintroduce a freeze without something going red — where "something" is a named mechanism, not an author's memory. (c) No *unintended* behavioural change against the #9350 differential baseline; the one intended change is the ruled grammar tightening, enumerated exhaustively in §10. (d) `ParseTimeout` remains, and stops being load-bearing for any known freeze.

---

## 2. Scope & Non-Scope

### In scope

- The liveness (termination) invariant of the construct loops in `ScriptParser`.
- The standing mechanism by which a future loop inherits that invariant.
- **The `ParseParameters` → `ParseTokenList` fold and the mismatched-closer grammar tightening** (ruled on #9426, 2026-08-24).
- The 1.2.0 break table for that tightening (§10).
- The fate of `docs/architecture/parse-core-no-progress-coupling.md` and task #9412.

### Explicitly out of scope

- **#2901** — parse-time raw runtime exceptions at EOF. §11 states whether this design closes it. (It does not, though the fold incidentally closes nine members — §4.8.)
- The `Malformed dictionary` / `Unterminated dictionary` message split. Author-facing, correct, untouched.
- `ScriptLimits.ParseTimeout` and `MaxParseDepth` semantics. Untouched.
- Execution-time guards (`Timeout`, `MaxSteps`, `MaxDepth`, `MaxVariables`) — a different subsystem.
- The `ParseArray` / `ParseTokenList` comment asymmetry (§4.5). Not a liveness defect; filed as F3.

---

## 3. Assumptions & Constraints

| # | Constraint | Source | Consequence for this design |
|---|---|---|---|
| C1 | No *unintended* behavioural regression against the #9350 differential baseline (3,335 prefix+deletion variants, compared by exception type **and message**). | #9426, #9397 | Every candidate is checked by differential sweep. §4.6 enumerates every one of the 967 differences the ruled change produces and shows none is a loosening or a hang. |
| C2 | `ScriptLimits.ParseTimeout` stays, default-on. | Toni, §0 | Untouched. The suite guard in §6.2 runs with `ScriptLimits.None` so the timeout cannot mask a structural hang. |
| C3 | `Malformed` / `Unterminated` distinction is author-facing and correct. | #9426 | Any shared mechanism must carry a **per-site** diagnostic. Decisive against direction 2 — §7.2. |
| C4 | Library at 1.1.0 with published consumers. | #9426 | The liveness work is entirely private to `ScriptParser`. The **grammar tightening is author-facing and breaking** — break table in §10, ships in **1.2.0** alongside `ParseTimeout` being default-on. |
| C5 | KISS bar: the parser already has three local checks + a `ParseCore` backstop + a wall-clock deadline. A fourth *mechanism* beside them is probably a failure; the goal is fewer moving parts than today. | #9426 | §7 rejects four of six candidate directions. §8 shows the ruled design holds the liveness-mechanism count **flat at 7** while removing a loop, a method and 25 lines — the deviation revision 1 declared is closed. |
| C6 | **The surviving progress guard must be load-bearing under ordinary single-site mutation.** #275 reserves the test-impossible exception to Toni personally; neither implementer, reviewer nor operator may wave it through. | RULING, #275 | §7.6 records a trap found by measurement: the obvious form of the mismatched-closer check **shadows** the guard and silently recreates the unpinnable-guard problem. The design narrows the check specifically to avoid this, and §4.7 is the measurement proving the guard is load-bearing. |

**Assumption A1.** `ScriptParser` is single-threaded per parse; `index` is a `ref int` local threaded through the call graph, not shared state. Verified by reading.

---

## 4. What was measured

This section is the evidence base. All measurements were taken on `master` @ `7a51a66` in scratch worktrees; the operator's tree was never modified.

### 4.1 Correction to the measured structure in #9426

#9426 records **nine** `done = true` sites in `ParseCore` (`:1054, :1330, :1337, :1343, :1364, :1385, :1458, :1468, :1512`). `:1054` is not one of them — it belongs to `ParseOperator`'s own local `done` (`ScriptParser.cs:1030-1058`), a separate and independently bounded loop. **`ParseCore` has eight escapes, not nine.** `ParseOperator`'s loop is bounded by construction: every non-`done` case advances `index` unconditionally.

### 4.2 Which escapes actually return without consuming

| site | trigger | advances? | can return no-progress from entry? |
|---|---|---|---|
| `:1330` | comment consumed, then stop | yes — comment consumed | no |
| `:1337` | `ParseOperator` returned null | yes on the paths that reach it | no |
| `:1343` | pre-increment/decrement, `index -= 2` rewind | net zero **this iteration**, but requires `concat == false`, hence a prior advancing iteration | no |
| `:1364` | `':'` under `suppressformat` | **no** | **yes** |
| `:1385` | `'$'` with `!concat` | no, but requires a prior advancing iteration | no |
| `:1458` | `','` or `']'` | **no** | **yes** |
| `:1468` | `';'` | yes — `++index` | no |
| `:1512` | `default` with `!concat` | no, but requires a prior advancing iteration | no |

**Only two of the eight escapes can produce a no-progress return from entry** — `','`/`']'` and `':'` under `suppressformat` — plus the end-of-input case. Three items, not nine.

Note `')'` is **not** among them: its case is commented out at `:1455`, so `)` falls through to `default` → `ParseToken` and is caught by `ParseCore`'s own backstop *inside* `ParseCore`. This asymmetry between `]` and `)` is what §7.6 turns on.

### 4.3 The decisive experiment — is `ParseParameters`' coincidence load-bearing?

QA #9397 upheld `ParseParameters` as safe and Design Contracts §6 correctly forbade adding a guard for an impossible scenario. The open question was never whether the claim is *true today* — it is — but whether anything holds it true tomorrow.

**Method.** Add one line to `ParseCore`'s switch: a new `case '@':` joining the existing `','`/`']'` escape group (`:1456-1459`) — the most ordinary way anyone extends the grammar. Then parse one input per construct loop.

| input | construct loop exercised | outcome with the new escape |
|---|---|---|
| `$a[@]` | **`ParseParameters`** (indexer args) | ***HANG*** — 100% CPU, unkillable |
| `if(@)$x=1` | **`ParseParameters`** (control-statement args) | ***HANG*** — 100% CPU, unkillable |
| `$x = foo(@)` | `ParseTokenList` | `Unexpected token in parameter list, expected ')'.` |
| `[@]` | `ParseArray` | `Invalid array specification` |
| `{@}` | `ParseDictionaryKey` | `Malformed dictionary` |
| `{"a":@}` | `ParseDictionary` (value) | `Malformed dictionary` |
| `$x=1; @` | `ParseStatementBlock` | `Unable to parse code` |

**One line of ordinary grammar work reopens an unkillable host freeze at exactly the two `ParseParameters` call sites, while all five guarded loops degrade cleanly and the suite stays green.** The coincidence was one line from failing. This is the measurement that put F1 to Toni and produced the ruling.

### 4.4 The pre-ruling minimal change (superseded by §4.6, retained for the audit trail)

Revision 1 proposed guarding `ParseParameters` in place. Measured: 4 insertions / 1 deletion, **781 passed / 0 failed**, and **0 differences** across a 15,407-input differential. It worked — but the guard was unreachable on today's grammar and therefore unpinnable, which is what §7.4 and the ruling replaced.

### 4.5 Latent quirks found while measuring

- **`ParseParameters` accepts mismatched terminators.** `$a[1)` and `if(1]$x=1` parse successfully today (`:1013-1015` treats `)` and `]` as interchangeable). **This is what the ruling tightens.**
- **`ParseArray` and the parameter-list family disagree about comments.** `[/*c*/1]` throws `Invalid array specification`; `$a[/*c*/1]` parses, storing a `null` in the argument list. `ParseArray`'s null-check is stricter than a progress check; the parameter-list family has none. **Preserved exactly by the fold** — `ParseTokenList` also stores nulls. Filed as F3.

### 4.6 The ruled change, measured end to end

The fold plus the tightening plus the `while(true)` normalisation, applied together and swept against `master`:

| measurement | result |
|---|---|
| Production diff | **7 insertions, 32 deletions** — **net −25 lines**, one file |
| `dotnet test -c Release` | **780 passed / 1 failed** — the single failure is `StrayClosingBracketInCallArguments_ThrowsWithinBound`, which pins the old message text; it is updated as part of the change (§10 row 3) |
| Differential vs `master`, 15,407 inputs, by exception type **and** message | **967 differences — every one accounted for below** |
| **Loosenings (rejected → accepted)** | **0** |
| **Hangs, either head** | **0** |

**Every difference, classified:**

| count | transition | meaning |
|---|---|---|
| **57** | `OK` → `ScriptParserException` | **the tightening.** Exclusively `$a[…)` (39) and `if(…]` (18) — precisely the two former `ParseParameters` call sites, nothing else |
| 655 | message: `Parameter list not terminated` → `Expected ']' \| ')' to end the parameter list.` | EOF in indexer/control args now reports `ParseTokenList`'s message |
| 182 | message: `Unexpected token in parameter list, expected 'X'.` → `Unexpected token 'Y' in parameter list, expected 'X'.` | progress-guard message now names the offending character |
| 33 | message: `Right hand side operand expected` → `Mismatched closing token ')' …` | the closer is now rejected before expression folding sees a dangling operator |
| 32 | message: `A parameter was expected` (`:220`) → `Unexpected token ']' in parameter list …` | control-statement args now fail earlier, in the list parser, with a more specific message |
| **9** | `IndexOutOfRangeException` / `ArgumentOutOfRangeException` → `ScriptParserException` | **improvement** — nine raw-runtime-exception leaks become clean parse errors (§4.8) |
| 39 | other message changes within `ScriptParserException`, all `$a[…)` shapes | the closer check now fires ahead of a later, less specific failure |

The critical rows are the ones that are **zero**: no input that `master` rejects becomes accepted, and no input hangs on either head. The 57 newly-rejected inputs are exactly the ruled tightening, and they are confined to the two call sites the fold touches.

### 4.7 The guard is load-bearing — measured, per C6

Single-site mutation on the final design: **delete `ParseTokenList`'s progress guard, change nothing else.**

| head | failures |
|---|---|
| final design, unmutated | 1 (the message-text test of §10 row 3) |
| final design, progress guard deleted | **2** — `Sample_LukasCode` **additionally** fails |

The guard's removal produces a **new, independent failure on a real sample script**. It is load-bearing under ordinary single-site mutation. **No #275 test-impossible sign-off is required, and none was given.**

§7.6 records the intermediate design where this was *not* true, and why.

### 4.8 Unplanned finding — the scale of the #2901 safety family

The differential harness records exception *type*, so it incidentally censused the whole corpus:

| head | `ScriptParserException` | `IndexOutOfRangeException` | `ArgumentOutOfRangeException` | parses |
|---|---|---|---|---|
| `master` | 12,271 | **1,359** | **1,185** | 592 |
| final design | 12,337 | 1,356 | 1,179 | 535 |

**2,544 of 15,407 inputs — 16.5% — make `ScriptParser.Parse` throw a raw runtime exception on `master`.** The three shortest are the single characters **`(`**, **`.`** and **`'`**.

#2901 is filed as *"parse-time `IndexOutOfRange`/NRE at EOF in the `new` branch"*. That description understates it by a wide margin: it is a large family with one-character members, spread across at least the `$a[`, `{"a`, `$a=`, `f(1`, `$a.`, `if(` and `$x=` contexts. This is not in scope here, but it is the single most actionable thing the measurement turned up, it directly validates the F2 recommendation (§11), and it tells the red-team battery where the mass is.

---

## 5. Architectural Overview — three legs, one of which is not code

```
  +- LEG 1 -- the invariant, stated where it is violated ----------------+
  |  Every construct loop carries a bound on progress in the loop it     |
  |  owns. The unguarded one is REMOVED rather than guarded:             |
  |  ParseParameters folds into ParseTokenList, which already has the    |
  |  guard. The only while(true) gains a range bound.                    |
  |  -> loops 6 -> 5, methods 2 -> 1, net -25 lines.                     |
  |  -> the ruled grammar tightening rides here (breaking, 1.2.0).       |
  +----------------------------------------------------------------------+
                                    |
                                    | does NOT answer: "how does loop #7 inherit?"
                                    v
  +- LEG 2 -- the invariant, enforced without the author's cooperation ---+
  |  One standing suite guard asserts TERMINATION as a whole-parser      |
  |  property over a fixed input sweep, run with ScriptLimits.None so    |
  |  ParseTimeout cannot mask a structural hang.                         |
  |  -> knows nothing about how many loops exist. A seventh loop that    |
  |     can spin goes red without anyone having told the test about it.  |
  |  -> lives in the TEST project. Not a runtime mechanism.              |
  +----------------------------------------------------------------------+
                                    |
                                    | does NOT answer: "what about what we did not foresee?"
                                    v
  +- LEG 3 -- ScriptLimits.ParseTimeout, unchanged, default-on ----------+
  |  The last resort for unknown issues, exactly as the operator         |
  |  scoped it. After legs 1 and 2, no KNOWN freeze depends on it.       |
  +----------------------------------------------------------------------+
```

**Legs 1 and 2 are not two implementations of one idea.** Leg 1 makes each loop locally correct. Leg 2 makes the *property* survive people. Only leg 2 answers the seventh-loop question.

The ruling changed Leg 1's shape in the best available direction: the unguarded loop is **deleted**, not guarded. A loop that does not exist cannot spin, needs no guard, and needs no test to pin the guard.

---

## 6. Components & Responsibilities

### 6.1 Leg 1 — the loop-level bound, after the fold

| Loop | Bound today (`master`) | After |
|---|---|---|
| `ParseArray` (`:948`) | `element == null` → `Invalid array specification`. **Stricter** than a progress check. | unchanged |
| `ParseTokenList` (`:977`) | explicit before/after check (`:992-994`) | **unchanged in mechanism**; message names the offending character; gains the mismatched-closer rule (§6.4); now serves the indexer and control-statement call sites too |
| `ParseParameters` (`:1007`) | **none** | **DELETED** — folded into `ParseTokenList` |
| `ParseDictionary` (`:1601`) | **`while(true)`** — no header bound | **header normalised to a range bound**; body unchanged |
| `ParseDictionaryKey` (`:1629`) | explicit check (`:1638-1641`) | unchanged |
| `ParseStatementBlock` (`:1676`) | explicit check (`:1681-1682`) | unchanged |

**Construct loops: 6 → 5. Parameter-list methods: 2 → 1. Explicit progress checks: 3 → 3.**

**What `ParseCore`'s backstop (`:1523`) owns, and why it stays exactly where it is.** It guards a *different granularity*: one switch dispatch, not one construct-loop iteration. It is why `$a[)]` reports an error rather than spinning. Not a duplicate; must not be merged (§7.5).

### 6.2 Leg 2 — the standing termination guard

A single fixture in `Scripting.Tests`, on the existing `BoundedSweep` harness.

| Property | Specification |
|---|---|
| **Asserts** | Every input in the sweep produces a *terminating* parse — a script or a throw — within a per-input bound. |
| **Input set** | All 1- and 2-character strings over the alphabet, **plus** a fixed context list (`$a[`, `f(`, `{`, `[`, `{"a":1,`, `$a[1,`, `f(1,`, `$a=$b<`, `$a[1:`, `new list<`, `{"a":`, `$a.b(`, `if(`, `$a=>{`, `$x=1;`) × every 2-character suffix. |
| **Alphabet** | Structural characters, one letter, one digit, whitespace, **and at least one character outside the grammar** — load-bearing, it models the §4.3 "someone added an escape" case. |
| **Limits** | **`ScriptLimits.None`** — no `ParseTimeout`. The single most important parameter of the design: it forces the sweep to test the *structural* invariant. With default limits the sweep would pass on a genuinely spinning parser. |
| **Bound mechanism** | The existing dedicated-thread + `Join(bound)` construct in `BoundedSweep`. A hang fails an assertion naming the input; it is never waited on. |
| **Measured cost** | ~15,400 inputs, **under two seconds**. |
| **Owns / does not own** | Liveness only. Not message correctness, not parse-result validity. |

> **⚠ The alphabet and context list above are PROVISIONAL.** They are derived from QA #9397's probe set, not from an enumeration of the parser's actual dispatch table. A red-team battery is enumerating the true dispatch alphabet against `master` in parallel with this design. **Reconcile this set against that enumeration before M3 is considered done** — it is a small follow-up, not a redesign, and it directly bounds R1.

**Why this answers "how does a seventh construct loop inherit the invariant".** The sweep contains no knowledge of how many loops exist, what they are called, or how they are shaped. It asserts the property the loops exist to satisfy. A seventh loop that can be made to spin goes red on the run that introduces it, with the input printed. Nobody has to have remembered anything.

### 6.3 Leg 3 — `ScriptLimits.ParseTimeout`

Unchanged; default-on. Its architectural status changes from *"the thing standing between a malformed script and a frozen host"* to *"the last resort for unknown issues"*.

### 6.4 The mismatched-closer rule (new, ruled)

`ParseTokenList` gains one grammar rule and one message refinement. Both are author-facing surface; both are in §10's break table.

| | Trigger | Diagnostic | Why here |
|---|---|---|---|
| **Pre-dispatch closer check** | the character is **`)`** and `)` is not this list's terminator | `Mismatched closing token ')' in parameter list, expected '{terminator}'.` | `)` is the **only** closer `ParseCore` does not treat as a no-progress escape (§4.2). Without this check it falls to `default` → `ParseToken` and surfaces as `Unable to parse code` — outside the `Malformed`/`Unterminated` family the ruling requires. |
| **Progress-guard message** | no progress after the recursive call | `Unexpected token '{character}' in parameter list, expected '{terminator}'.` | `]` reaches the guard (it *is* an escape), so the mismatch is reported here. Naming the character gives the author the same information the closer check gives, without the guard claiming a classification it did not make. |

**The check is deliberately narrowed to `)` alone.** Extending it to `]` is the obvious form and it is **wrong** — §7.6 records the measurement.

---

## 7. Decisions — the candidate directions

#9426 sketched three directions. Three more surfaced during analysis. **One is adopted (7.4, by ruling); five are rejected**, each on a measurement.

### 7.1 REJECTED — "Type the no-progress outcome"

**The distinguishing information the type would carry is read by nobody.** All six construct loops detect *"this character is not mine"* by **peeking the character themselves** — `ParseDictionaryKey` peeks `'}'` (`:1630`), `ParseTokenList` compares `terminator`/`delimiter` (`:978-987`), `ParseParameters` switches on `')'`/`']'`/`','` (`:1013-1018`), `ParseStatementBlock` checks `'}'` (`:1683`), `ParseArray` switches on `']'`/`','` (`:955-960`). **Not one learns "not mine" from a no-progress return.**

A type distinguishing no-progress-null from ordinary null is therefore a value nothing consumes — #1136 §2 Form 1 (data dump) at type level; recurse the consumer chain and it dead-ends immediately. It would touch all nine `Parse(` call sites. And it would not deliver the goal: an author who must unwrap can still `continue`. It converts *"remember to check the index"* into *"remember to handle this case"* — a different thing to remember, not one thing fewer.

### 7.2 REJECTED — "Express the loop contract once as a shared primitive"

The most promising direction on paper; rejected on the project's own arithmetic.

**DRY math (#1136 §1, #1267).** The same before/after check appears at **three** sites — `:992-994`, `:1638-1641`, `:1681-1682` — at **3 lines each**.

| scenario | sites | block | product | vs ~15–20 threshold |
|---|---|---|---|---|
| `master` | 3 | 3 | **9** | below |
| revision 1's proposal (guard `ParseParameters`) | 4 | 3 | **12** | below |
| **after the ruled fold** | **3** | 3 | **9** | **below** |
| if all loops carried it | 6 | 3 | 18 | above |

The six-site row is unreachable: `ParseArray` and `ParseDictionary` are bounded by stricter local constructs C1 protects. **The fold moves the count *down* to 3 — further from the threshold, not closer. The math argues against extraction more strongly after the ruling than before it.**

Three further findings confirm the call:

- **C3 kills the shared diagnostic.** The messages are author-facing and must not collapse, and the differential compares by message. A shared primitive must accept a per-site message; one of them is interpolated over the terminator, so passing it into a loop header either allocates per iteration in the hot path or forces a per-call allocation where today it allocates only on the throw path.
- **It does not answer the question it was proposed to answer.** A helper is opt-in; loop #7's author who does not know the invariant will not reach for it.
- **It is a new mechanism** — precisely what C5 warns against.

**Kept from this direction:** normalising `ParseDictionary`'s `while(true)`, so every construct loop carries a range bound in its header. One line, zero behaviour change. `while(true)` count after: **zero**.

### 7.3 REJECTED — "Enforce at the `Parse` choke point"

**Measured counter-example.** A no-progress return from a *single, non-looping* call is legitimate and load-bearing for input that parses today:

| input | outcome on `master` |
|---|---|
| `{"a":,}` | **parses** — no-progress null at the dictionary-value site, stored as a null value |
| `{"a":,"b":2}` | **parses** |
| `{"a": , }` | **parses** |

Making `Parse` throw turns all three into errors — a C1 regression. Making it merely *record* the fact does not help: the caller that must act is the loop, and `Parse` cannot know whether its caller is looping.

**This reframes the whole problem. The hazard is not the call; it is the loop.** A single no-progress call is a legitimate parse outcome; the same call in a loop is an infinite loop. `Parse` sees the call; only the loop sees the loop.

**Variant also rejected:** a `mustConsume` flag threaded through `Parse`. It cannot carry the per-site message without eager allocation, it threads a parameter through nine sites, and loop #7's author must still remember to pass it — the same inheritance failure, relocated.

### 7.4 ADOPTED (ruled 2026-08-24) — "Delete `ParseParameters`"

Not in #9426's sketch. Revision 1 identified it as the only change that genuinely reduces the parser and filed it as open question F1, blocked on the operator because it tightens the grammar. **Toni ruled: tighten.**

`ParseParameters` and `ParseTokenList` are near-duplicates — both parse a delimited list until a terminator — and `ParseTokenList` is the better of the two: it parameterises terminator and delimiter, and it already carries the progress guard. `ParseParameters` had exactly two call sites, each with an unambiguous terminator:

| call site | context | terminator |
|---|---|---|
| `ParseControlParameters` (`:243`), after consuming `(` | `if(…)`, `while(…)`, … | `)` |
| indexer (`:1430`), after consuming `[` | `$a[…]` | `]` |

Both become `ParseTokenList` calls with `scanforoperations: true` and their own single terminator. `ParseParameters` is deleted outright.

**What this buys, measured (§4.6):** construct loops 6 → 5, parameter-list methods 2 → 1, **net −25 production lines**, the unguarded loop gone rather than guarded, and — per §4.7 — a surviving guard that is load-bearing under ordinary mutation.

**What it costs:** the grammar tightening (57 corpus inputs move from accepted to rejected) and five message families (§10). Both are intended, both are enumerated exhaustively, and neither loosens the grammar or introduces a hang.

**Why this is better on the merits, not merely cheaper.** §4.3 measured the coincidence to be one line from failing. A guard would have *mitigated* that; the fold *removes* it. A loop that does not exist cannot spin, needs no guard, and needs no test to pin a guard.

### 7.5 REJECTED — merging `ParseCore`'s backstop into the loop-level check

`ParseCore`'s `:1523` check compares position before the switch against after it, *within* one iteration. A header-based check compares across iterations — after `SkipWhitespaces` (`:1526`). These differ whenever the switch makes no progress but the whitespace skip does: today the backstop throws at the offending character; a header check would step over the whitespace and re-dispatch, changing which character is reported and possibly the message. Two granularities of one idea; merging risks C1 for a cosmetic gain. **`ParseCore:1523` stays exactly as it is, `!done` gate included.**

### 7.6 REJECTED — the obvious form of the mismatched-closer check (a trap, found by measurement)

The natural way to write §6.4's rule is *"if the character is a closer (`)` or `]`) and is not this list's terminator, throw."* **It is wrong, and the reason is not visible by reading.**

`]` **is** one of `ParseCore`'s no-progress escapes (§4.2); `)` is not. So a closer check covering both intercepts `]` *before* it can reach `ParseTokenList`'s progress guard — and `]` is the guard's only pinned trigger. Measured:

| design | failures with guard present | failures with guard **deleted** | guard load-bearing? |
|---|---|---|---|
| closer check covers `)` **and** `]` | 1 | **1** — no new failure | **NO** |
| closer check covers **`)` only** | 1 | **2** — `Sample_LukasCode` additionally fails | **YES** |

The broad form silently recreates exactly the unpinnable-guard problem the fold was ruled in to dissolve — and it would have shipped looking correct, with a green-except-one suite either way.

**Decision: the pre-dispatch check covers `)` only.** `]` continues to flow through `Parse`, return no-progress, and be caught by the progress guard, whose message now names it. Both mismatch directions are rejected with a specific, author-facing message; the guard stays load-bearing; no #275 sign-off is needed.

**`>` is deliberately excluded** from the closer set — `ParseTokenList` is also called with terminator `>` for generics (`:922`), and `>` is a comparison operator, so treating a stray `>` as a mismatched closer would break `f(a > b)`. **`}` is also excluded**, to keep the change minimal: `f(1}` reports `Unable to parse code` today and continues to.

---

## 8. The KISS accounting — restated against the ruled design

Revision 1 reported **+1 runtime mechanism (7 → 8)** against C5's bar of *fewer moving parts than today*, and declared it as a deviation rather than dressing it up. **The ruling closes that deviation.** The fold removes a loop and a method instead of adding a check:

| | `master` @ `7a51a66` | revision 1 (guard in place) | **ruled design** | Δ vs master |
|---|---|---|---|---|
| Construct loops | 6 | 6 | **5** | **−1** |
| Parameter-list methods | 2 | 2 | **1** | **−1** |
| Loops with **no** bound on progress | **2** | 0 | **0** | **−2** |
| Explicit before/after progress checks | 3 | 4 | **3** | **0** |
| Stricter local check (`ParseArray` null) | 1 | 1 | 1 | — |
| `ParseCore` dispatch backstop | 1 | 1 | 1 | — |
| Global bounds (`MaxParseDepth`, `ParseTimeout`) | 2 | 2 | 2 | — |
| **Liveness mechanisms, total** | **7** | **8** | **7** | **0 — flat, not +1** |
| `while(true)` loops | 1 | 0 | **0** | −1 |
| Loops needing a **cross-method** proof | **1** | 0 | **0** | **−1** |
| Guards that are unpinnable by single-site mutation | 0 | **1** | **0** | **0** |
| Architecture docs stating an unenforced invariant | **1** | 0 | **0** | **−1** |
| Standing suite guards asserting termination | **0** | 1 | **1** | +1 |
| New types / abstractions / files in the production assembly | — | 0 | **0** | — |
| **Net production lines** | — | +3 | **−25** | **−25** |
| New grammar rule (mismatched closer) | 0 | 0 | 1 | +1 (the ruled tightening) |

**The liveness-mechanism count is flat at 7 while covering one fewer loop through one fewer method in 25 fewer lines.** Revision 1's deficit is gone: the design no longer grows the parser to make it safe — it shrinks it.

The one addition is the **mismatched-closer grammar rule**, which is not a liveness mechanism at all. It exists because the operator ruled the grammar should be tighter, and it is the reason §10 exists.

**What also goes down is the amount a human must know.** On `master`, someone adding a `case` to `ParseCore` must know a fact stated in a different method and recorded in a separate architecture document, or they reopen the worst failure mode the library has, with a green suite (§4.3). After this change that obligation is zero, the document is deleted, and the property is asserted by a test that does not need to be told about their new loop.

---

## 9. Contracts & Interfaces (abstract)

### 9.1 The construct-loop contract

> **Any loop in `ScriptParser` that calls `Parse`, `ParseSingle`, or a sibling recursive-descent method must guarantee that every iteration which does not exit the loop strictly advances `index`.**
>
> A loop satisfies this by one of exactly two means, and no other:
> 1. **Explicitly** — capture the position before the recursive call, compare after it, raise a `ScriptParserException` with a site-specific message when it has not moved.
> 2. **By subsumption** — a local check in the same method that rejects every outcome the call can produce without advancing. `ParseArray`'s null-element rejection is the only instance.
>
> *"The call cannot return without advancing, because of a fact about another method"* is **not** an admissible means. That is the form §4.3 measured to be one line from failing, and the form the fold removes.

**Corollary, from §7.6:** a check placed *before* the recursive call that intercepts a character which would otherwise have reached the progress guard **weakens** the guard. Before adding one, confirm which of `ParseCore`'s no-progress escapes still reach the guard.

### 9.2 The `Parse` return contract, unchanged and now written down

| return | meaning | may `index` have advanced? |
|---|---|---|
| non-null token | a construct was parsed | always — a token cannot be produced without consuming |
| null, `index` advanced | input consumed but folded to nothing (a comment with metatokens disabled; a bare `;`) | yes |
| null, `index` unchanged | **"this character is not mine, do not consume it"**, or end of input | no |

The third row is the capability #9426 requires be preserved. It is preserved exactly as it is — §10.

### 9.3 The termination property (Leg 2's contract)

> For every input over the swept alphabet and context set, `ScriptParser.Parse` **terminates** — returns a script or throws — within a bounded time, **with `ScriptLimits.None`**.
>
> The limits clause is the contract. Asserted with default limits, the property is satisfied by the timeout and says nothing about the parser's structure.

---

## 10. Public surface, break table, and the eight `done = true` escapes

### 10.1 The escapes — decision: unchanged

#9426 frames the root cause as *"an overloaded `null`"*. **The framing is half right, and the half that is wrong is the half that would have cost the most to act on.**

The escapes are `ParseCore`-internal loop control: a local flag exiting a local loop, unobservable outside `ParseCore`. What crosses the method boundary is `FoldExpression`'s empty-list result (`:1577`) → null. **Measured answer to whether any caller must distinguish it: no** (§7.1) — every construct loop detects "not mine" by peeking the character itself. The capability #9426 insists must survive is **already not carried by the null**; it is carried by the caller's own peek, and it survives untouched.

The genuinely load-bearing consumer of a no-progress null is the *single-call* site in `ParseDictionary`, where it produces a null dictionary value and makes `{"a":,}` parse (§7.3). C1 protects that.

**All eight escapes, and the null they produce, are unchanged.** The defect was never how the outcome is signalled; it was that two loops did not bound themselves — and the ruled design removes one of those loops entirely rather than signalling differently to it.

### 10.2 API surface

**No API change.** No public type, member, signature, default, or `ScriptLimits` property is added, removed or altered. `ParseParameters` and `ParseTokenList` are both private. Leg 1 is invisible to a consumer who compiles against the assembly.

### 10.3 Break table — grammar, ships in **1.2.0**

The change is breaking **at the script-source level**, not the API level: scripts that parsed under 1.1.0 may be rejected under 1.2.0. Ships alongside `ParseTimeout` being default-on.

| # | Change | 1.1.0 | 1.2.0 | Corpus hits | Migration |
|---|---|---|---|---|---|
| **B1** | **Mismatched closers rejected.** `)` and `]` are no longer interchangeable in indexer and control-statement argument lists. | `$a[1)` parses; `if(1]$x=1` parses | `ScriptParserException` — `Mismatched closing token ')' in parameter list, expected ']'.` / `Unexpected token ']' in parameter list, expected ')'.` | **57** | Close the list with the bracket that opened it. The message names both the found and the expected closer. |
| B2 | EOF in indexer/control args reports the list parser's message | `Parameter list not terminated` | `Expected ']' to end the parameter list.` / `Expected ')' …` | 655 | Message text only — same exception type, same rejection. Update any host that string-matches. |
| B3 | Progress-guard message names the offending character | `Unexpected token in parameter list, expected ')'.` | `Unexpected token ']' in parameter list, expected ')'.` | 182 | Message text only. **`Scripting.Tests` has one test asserting the old text** (`StrayClosingBracketInCallArguments_ThrowsWithinBound`) — update it in the same commit. |
| B4 | A dangling operator before a mismatched `)` is reported as the closer, not the operator | `Right hand side operand expected` | `Mismatched closing token ')' …` | 33 | Message text only. |
| B5 | Control-statement args fail in the list parser rather than downstream | `A parameter was expected` (`:220`) | `Unexpected token ']' in parameter list …` | 32 | Message text only, and more specific. |
| **B6** | **Nine raw-runtime-exception leaks become clean parse errors** | `IndexOutOfRangeException` / `ArgumentOutOfRangeException` | `ScriptParserException` | 9 | **Improvement, not a break.** A host catching only `ScriptParserException` now catches nine inputs it previously could not. |

**Not in the table because measured to be zero:** any input rejected by 1.1.0 that 1.2.0 accepts (0), and any hang on either head (0).

`Malformed dictionary` / `Unterminated dictionary` are **untouched**, per C3.

---

## 11. Does this close #2901? — No, and the reason is the honest test

**Measured on the final design:** `new`, `new `, `$a=new` still throw `NullReferenceException`. #2901 is untouched — though the fold incidentally converts **nine** family members from raw runtime exceptions to clean parse errors (§4.6 B6).

**The reason is the correct one.** This design enforces a **liveness** property — *the parser always terminates*. #2901 is a **safety** property — *the parser terminates in its own exception domain, never with a runtime exception that leaks implementation*. Different invariants over the same code; a mechanism for one has no purchase on the other. `new` at EOF terminates promptly; it just terminates wrongly.

**But Leg 2 is one predicate away, and §4.8 shows how much it would buy.** Widen the assertion from *"terminates"* to *"terminates **with either a parsed script or a `ScriptParserException`**"* — no new mechanism, no new test, one predicate — and the entire class becomes visible. §4.8 measured that class at **2,544 of 15,407 inputs (16.5%) on `master`**, with one-character members (`(`, `.`, `'`), which is far larger than #2901's filed description suggests.

**Operator-confirmed recommendation: widen it in the PR that fixes #2901**, not now behind a suppression list. At that point the widened sweep becomes that fix's own regression pin, for free, and the parser gains a standing safety guarantee alongside its liveness one. **Filed as F2.** §4.8's census should be attached to #2901 as scoping input.

---

## 12. Fate of `parse-core-no-progress-coupling.md` and task #9412

**`docs/architecture/parse-core-no-progress-coupling.md` is DELETED** in the implementing PR.

The document exists for exactly one reason, stated in its own opening: to record an invariant that could not be enforced, so the next person to touch `ParseCore`'s switch would know to check `ParseParameters`. **`ParseParameters` no longer exists.** The document's subject, its invariant and its closing instruction to a future author are all vacuous.

Deletion, not a `> SUPERSEDED by` banner. #1136 §5's banner rule governs *design documents superseded by a newer design*, where a reader may still need the predecessor's reasoning. This is a note about a hazard that ceases to exist; leaving it live would send a reader checking a coupling between a method and one that was deleted. Its content is preserved in §4.3, now backed by a measurement.

**Task #9412** (polish the coupling doc) **closes as superseded**.

---

## 13. Risks & Mitigations

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **Leg 2's coverage is bounded by its alphabet and context list**, which are **provisional** (§6.2) — derived from QA #9397's probes rather than from the parser's true dispatch table. | Medium — the design's principal residual risk | A red-team battery is enumerating the real dispatch alphabet against `master` now. Reconcile before M3 is done; a small follow-up, not a redesign. Leg 3 (`ParseTimeout`) remains the backstop for exactly this, which is the role §0 assigns it. |
| R2 | **The mismatched-closer check shadows the progress guard**, silently making it unpinnable and recreating the #275 problem the fold was ruled in to dissolve. | **High — and it is invisible by reading** | Resolved at design time, not delegated: the check covers **`)` only** (§6.4, §7.6). §4.7 is the measurement — with the narrow form, deleting the guard produces a *new* failure (`Sample_LukasCode`); with the broad form it produces none. **M4 step 3 makes this a standing QA check.** ⚠ **Do not "simplify" the check to cover both closers.** |
| R3 | A future author adds a construct loop **and** a grammar character outside the swept alphabet in one change, evading Leg 2. | Low | The alphabet includes an out-of-grammar character precisely to model novel characters; a new structural character almost always arrives in a new context the same PR would add. Leg 3 backstops. |
| R4 | Normalising `ParseDictionary`'s `while(true)` changes EOF handling. | Low — measured to zero | Isolated differential showed 0 differences. The `terminated` flag is untouched: at EOF the header exits without entering the body, leaving `terminated == false`, which is what `ParseDictionaryKey` returning null already produced. |
| R5 | Leg 2 runs with `ScriptLimits.None`, so a genuine hang **hangs the test run** rather than failing it — the exact trap #9341 identified. | Medium | The existing `BoundedSweep` harness solves this: dedicated background thread, `Join(bound)`, hang reported as a failed assertion, never waited on. Must be reused — no second bounding primitive (#9397 W-3). |
| R6 | A consumer string-matches parser messages and breaks on B2–B5. | Low | Enumerated exhaustively in §10.3 with corpus counts; ships in a minor-version bump with the break table. Exception **type** is unchanged in every row. |

> **Revision 1's R2 is withdrawn, not granted.** It read: *"the `ParseParameters` guard is not load-bearing on today's suite … under the standard QA #9397 applied, a production line no test kills is a rejection"*, and proposed a two-step mutation as a substitute pin. **There is no longer a `ParseParameters` guard** — the fold deleted the method. The surviving guard is reachable and load-bearing (§4.7). **The two-step mutation apparatus is dropped, and no #275 test-impossible sign-off was requested or given.** #275 reserves that exception to Toni personally; the question was removed by the ruling, not waved through by the operator, the reviewer or this design. R2 above is a *different* risk, discovered later, and it is closed by measurement rather than by waiver.

---

## 14. Implementation Guidance

**M1 — Fold `ParseParameters` into `ParseTokenList`.**
1. `ParseControlParameters` (`:243`): call `ParseTokenList` with `scanforoperations: true`, terminator `')'`.
2. Indexer (`:1430`): same, terminator `']'`.
3. Delete `ParseParameters` (`:1002-1027`) entirely, including its commented-out `case '['`.

Do **not** add a null-check for parsed arguments — `ParseTokenList` stores nulls exactly as `ParseParameters` did, and that equivalence is what keeps `$a[/*c*/1]` parsing (§4.5, C1). F3 covers it separately.

**M2 — The mismatched-closer rule** (§6.4), in `ParseTokenList`:
1. Pre-dispatch check for **`)` only** when `)` is not the terminator → `Mismatched closing token ')' in parameter list, expected '{terminator}'.`
2. Progress-guard message gains the offending character → `Unexpected token '{character}' in parameter list, expected '{terminator}'.`

> ⚠ **`)` only.** Covering `]` as well makes the progress guard unpinnable (§7.6, R2). `>` and `}` are excluded for the reasons in §6.4.

**M3 — Normalise the only unbounded loop header.** `ParseDictionary`'s `while(true)` (`:1601`) → a range-bounded header. Body unchanged, including the `terminated` computation and the `key == null` break. `while(true)` count after: zero.

> M1 + M2 + M3 together are **7 insertions, 32 deletions — net −25 lines** in one file. A materially larger diff means something has been over-built.

**M4 — Update the one affected test.** `StrayClosingBracketInCallArguments_ThrowsWithinBound` asserts the pre-fold message text (B3). Update its expectation to the character-naming form. It remains the test that pins the progress guard — do not weaken it to a substring that the closer check would also satisfy.

**M5 — Add the standing termination guard** (Leg 2) per §6.2, in `Scripting.Tests`, on the existing `BoundedSweep` harness, constructed with `ScriptLimits.None`. No second bounding primitive. Keep the alphabet and context list as named constants so extending them is obvious, and **reconcile them against the red-team's dispatch enumeration before calling M5 done** (R1).

**M6 — Verification, and the standing instruction for QA.** Four checks:

1. **Differential:** re-run the #9350 corpus (3,335 prefix + deletion variants) plus the 15,407-input sweep across `master` and the branch head, comparing by exception type **and** message. **Expected: the §4.6 profile — 57 accepted→rejected confined to `$a[` and `if(`, the five message families of §10.3, nine leak→clean improvements, and *zero* loosenings and *zero* hangs.** Any loosening or any hang is a defect, not an acceptable delta.
2. **Suite:** `dotnet test -c Release`. Expected all green after M4.
3. **The mutation that matters (R2).** Delete `ParseTokenList`'s progress guard, change nothing else, run the suite. **A new failure beyond any expected one must appear** (measured: `Sample_LukasCode`). If deleting the guard leaves the suite otherwise unchanged, the mismatched-closer check has been widened and is shadowing the guard — revert to the `)`-only form. This is a standing check, not a one-off.
4. **Break table:** confirm every row of §10.3 against the branch head, and that no row outside it appears.

**M7 — Documentation.** Delete `docs/architecture/parse-core-no-progress-coupling.md` (§12). Commit this design document in the same PR. Ensure the 1.2.0 release notes carry §10.3.

**Follow-up tasks to file (not this PR):**

- **F2 — Widen Leg 2's predicate** from *"terminates"* to *"terminates with a parse result or a `ScriptParserException`"*, **in the PR that fixes #2901** (§11). One predicate. Attach §4.8's census (2,544/15,407 = 16.5%, one-character members `(`, `.`, `'`) to #2901 as scoping input — the defect is materially larger than its title suggests.
- **F3 — Resolve the `ParseArray` / parameter-list comment asymmetry** (§4.5): `[/*c*/1]` errors; `$a[/*c*/1]` parses and stores a null argument. Behavioural, needs an operator decision, unrelated to liveness.
- **F4 — Reconcile Leg 2's alphabet** with the red-team's dispatch enumeration (R1), if not already folded into M5.

*(Revision 1's F1 — the fold — is no longer a follow-up. It was ruled and is now M1/M2.)*

---

## 15. Pre-Design Checklist audit (#1136 §5)

**KISS / DRY / YAGNI**
- ✅ No new type mirroring an existing one — **no new type at all**.
- ✅ No new abstraction with one implementation — the shared primitive was the obvious candidate and is rejected in §7.2 on the project's own arithmetic, which the ruling *strengthened* (4 sites → 3).
- ✅ No element justified by "we might need X later". F2/F3/F4 are follow-ups with named triggers.
- ✅ No deprecation period, feature flag, compatibility shim, or transition window. The grammar break is a clean minor-version bump with a break table, not a dual-mode parser.
- ✅ **DRY math quoted:** 3 lines × 3 sites = 9 on master; **3 sites = 9 after the fold**; both below #1267's ~15–20 threshold. The 6-site row (18) is unreachable because two loops are bounded by stricter checks C1 protects. Extraction declined **by the math**.
- ✅ **The design removes more than it adds:** −1 loop, −1 method, −25 lines, liveness mechanisms flat.

**Existing systems first**
- ✅ The fold is the archetype: instead of adding a guard to a second parameter-list parser, the second parser is **deleted** and its call sites routed to the one that already had the guard.
- ✅ Leg 2 is built on the `BoundedSweep` harness already in `Scripting.Tests`; R5 forbids a second bounding primitive.
- ✅ No new layer proposed. No new persisted data.

**Configurability**
- ✅ **No new config knob.** `ParseTimeout` untouched; no opt-out for the tightening, no parser strictness mode. Leg 2's alphabet and context list are `const` in the fixture per §3's constant rule.

**Less is better**
- ✅ Can-it-be-deleted run on every element — it deleted the shared primitive (§7.2), the outcome type (§7.1), the `Parse`-level enforcement and its flag variant (§7.3), the backstop merge (§7.5), the broad closer check (§7.6), **an entire production method** (§7.4), and an existing architecture document (§12).
- ✅ Trade-offs named explicitly: §8 restates the arithmetic against the ruled design and shows the revision-1 deficit closed; §10.3 enumerates every author-facing break; §11 states that #2901 is not closed.
- ✅ Radical-clean chosen: `ParseParameters` deleted rather than guarded; coupling doc deleted rather than banner-marked.
- ➖ Reader-inventory / carrier-swap tables: not applicable — no field, symbol or DTO changes. The two `ParseParameters` call sites are enumerated exhaustively in §7.4.

**Data deliverables** — ➖ not applicable.

**Document discipline**
- ✅ Cites Code Contracts #114 and Design Contracts #1136 as load-bearing (header); #275 cited where it binds.
- ✅ Scope inventory explicit (§2); loop inventory with file:line (§6.1); call-site inventory (§7.4); break table (§10.3).
- ✅ Out-of-scope items listed explicitly (§2).
- ✅ Predecessor handling: §12 disposes of the coupling doc and #9412 in this PR.
- ✅ Revision history at the head; superseded reasoning retained where it is still evidence (§4.4) and withdrawn explicitly where it is not (R2).

**No open deviations.** Revision 1 declared one — +1 runtime mechanism against C5 — and the ruling closed it (§8). Nothing in the current design knowingly violates a contract rule.

---

## 16. Open Questions

**Resolved since revision 1 — recorded so the trail is legible:**

- **F1 (tighten `)`/`]`?) — DECIDED: tighten.** Ruled by Toni, 2026-08-24. Now M1/M2. Closes the §8 deviation.
- **Revision 1's Q2 (does the QA standard accept a class-level pin for an unpinnable guard?) — DISSOLVED, not granted.** The fold deletes the guard in question; the surviving one is load-bearing (§4.7). No #275 sign-off was requested or given. See the R2 note in §13.
- **Q3 (#2901 / F2 timing) — CONFIRMED.** Widen Leg 2's predicate in the PR that fixes #2901, not now behind a suppression list.

**Still open:**

1. **Leg 2's alphabet is provisional (R1, F4).** Awaiting the red-team battery's enumeration of the parser's true dispatch alphabet. Not blocking — M5 lands with the derived set and is reconciled after. This is the design's principal residual risk and the only thing standing between Leg 2 and a coverage claim rather than a coverage hope.
2. **§4.8 deserves a decision that is not mine to make.** 16.5% of a 15,407-input corpus makes `ScriptParser.Parse` throw a raw `IndexOutOfRangeException` / `ArgumentOutOfRangeException` on `master`, with one-character reproducers (`(`, `.`, `'`). #2901's title describes a much narrower defect. Does #2901 get rescoped to the family, or does the family get its own task with #2901 as one member? Either is fine; leaving #2901 titled as an EOF case in the `new` branch will under-scope the fix.
3. **B2–B5 are message-only breaks with 902 corpus hits between them.** Is any published consumer known to string-match parser messages? If yes, the release notes need more than the break table. If no (the expected answer), §10.3 is sufficient.
