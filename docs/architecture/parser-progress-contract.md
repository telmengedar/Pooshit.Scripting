> **Repo working-tree path:** `docs/architecture/parser-progress-contract.md` (canonical copy; committed on the implementation branch)
> **Task:** #9426 · **Project:** #5 · **Diagnosis:** #9341 · **QA:** #9397 · **Driver bug:** #9350 · **Superseded:** #9412 · **Repo map:** #2676 · **Sibling defect:** #2901
> **Standards:** Design Contracts #1136 (§1 load-bearing, §5 walked), Code Contracts #114 §0, test-impossible exception #275
> **Baseline:** `master` @ `7a51a66`. Every number below was measured, not inferred.
> **Status:** design only — no branch, no PR. Implementer: `john-backend-dev`.
>
> **REVISION 2 — 2026-08-24, after Toni's RULING on #9426. F1 DECIDED: tighten.** `ParseParameters` folds into `ParseTokenList`; `)` and `]` stop being interchangeable closers. This closes revision 1's declared §8 deviation, dissolves revision 1's second open question, and makes the change a **net reduction** of the parser. Revision 1's rejections of directions 1, 2, 3 and 5 stand unchanged.

# The Parser Progress Contract

## Verdict

The fix is **7 insertions / 32 deletions — net −25 production lines** in one file. It **removes** the unguarded loop rather than guarding it: `ParseParameters` is deleted and its two call sites routed to `ParseTokenList`, which already carries the progress guard. Construct loops **6 → 5**, parameter-list methods **2 → 1**, liveness mechanisms **flat at 7**. The answer to *"how does a seventh construct loop inherit the invariant"* is **not a shared primitive** — the project's own DRY arithmetic rejects it, and the fold moves the count *further* from the threshold (3 sites × 3 lines = 9) — but a **standing termination sweep run with `ScriptLimits.None`**, which knows nothing about how many loops exist. `ScriptLimits.ParseTimeout` stays default-on as the last resort.

## What changed in revision 2

| | revision 1 | ruled design |
|---|---|---|
| `ParseParameters` | guarded in place (3 new lines) | **deleted**, folded into `ParseTokenList` |
| Construct loops | 6 | **5** |
| Explicit progress checks | 4 | **3** |
| Liveness mechanisms vs master | **7 → 8 (+1, declared deviation)** | **7 → 7 (flat)** |
| Net production lines | +3 | **−25** |
| Guards unpinnable by single-site mutation | 1 (the #275 problem) | **0** |
| Grammar | unchanged | **tightened — breaking, 1.2.0** |

**Revision 1's declared deviation is closed.** It reported +1 runtime mechanism against the operator's bar of *fewer moving parts than today* and flagged it plainly rather than dressing it up — which is why the question reached Toni in a decidable form. The fold shrinks the parser instead of growing it.

## The measurement that decided it

Add one line to `ParseCore`'s switch — `case '@':` joining the existing `','`/`']'` no-progress escape group — the most ordinary way anyone extends the grammar:

| input | loop | outcome |
|---|---|---|
| `$a[@]` | **ParseParameters** (indexer args) | ***HANG*** — 100% CPU, unkillable |
| `if(@)$x=1` | **ParseParameters** (control-stmt args) | ***HANG*** — 100% CPU, unkillable |
| `$x = foo(@)` | ParseTokenList | `Unexpected token in parameter list, expected ')'.` |
| `[@]` | ParseArray | `Invalid array specification` |
| `{@}` | ParseDictionaryKey | `Malformed dictionary` |
| `{"a":@}` | ParseDictionary (value) | `Malformed dictionary` |
| `$x=1; @` | ParseStatementBlock | `Unable to parse code` |

The coincidence was **one line from failing**, with the suite green. A guard would have mitigated it; the fold removes it. **A loop that does not exist cannot spin, needs no guard, and needs no test to pin the guard.**

## The trap — found by measurement, not by reading (§7.6)

The natural way to write the new mismatched-closer rule is *"if the character is a closer (`)` or `]`) and is not this list's terminator, throw."* **It is wrong.**

`]` **is** one of `ParseCore`'s no-progress escapes; `)` is **not** (its case is commented out, so `)` falls to `default` and is caught inside `ParseCore`). A closer check covering both intercepts `]` *before* it reaches `ParseTokenList`'s progress guard — and `]` is that guard's only pinned trigger:

| design | failures, guard present | failures, guard **deleted** | load-bearing? |
|---|---|---|---|
| check covers `)` **and** `]` | 1 | **1 — no new failure** | **NO** |
| check covers **`)` only** | 1 | **2** (`Sample_LukasCode` additionally) | **YES** |

The broad form silently recreates the unpinnable-guard problem the fold was ruled in to dissolve, and would have shipped looking correct. **Decision: the pre-dispatch check covers `)` only**; `]` keeps flowing to the progress guard, whose message now names the offending character. `>` is excluded (it is a comparison operator and a generics terminator); `}` is excluded to keep the change minimal. **M6 step 3 makes this a standing QA check — do not "simplify" it.**

## Directions — one adopted, five rejected

| direction | verdict | grounds |
|---|---|---|
| **1. Type the no-progress outcome** | REJECTED | **No caller reads no-progress-ness as information.** All six loops detect *"not mine"* by peeking the character themselves. A type carrying that distinction is a #1136 §2 Form-1 dump at type level. And a loop author who must unwrap can still `continue`. |
| **2. Shared loop primitive** | REJECTED | DRY math: 3 lines × 3 sites = 9 on master, **still 3 sites = 9 after the fold** — below #1267's ~15–20 threshold, and the fold moves *away* from it. The 6-site row (18) is unreachable. Also C3: three author-facing messages must not collapse, one is interpolated. And it does not answer the seventh-loop question — a helper is opt-in. |
| **3. Enforce at the `Parse` choke point** | REJECTED | **Measured:** `{"a":,}`, `{"a":,"b":2}`, `{"a": , }` **parse today**, relying on a no-progress null at the dictionary-value site. **The hazard is the loop, not the call.** `Parse` sees the call; only the loop sees the loop. (`mustConsume` flag variant also rejected.) |
| **4. Delete `ParseParameters`** | **ADOPTED (ruled)** | Two call sites, each with an unambiguous terminator: `ParseControlParameters` (`)`) and the indexer (`]`). Both become `ParseTokenList` calls. Net −25 lines. |
| **5. Merge `ParseCore:1523` into the loop check** | REJECTED | Different granularity — one *switch dispatch* vs one *loop iteration*. A header check compares after `SkipWhitespaces`, changing which character is reported. |
| **6. Broad mismatched-closer check** | REJECTED | Shadows the progress guard — see the trap above. |

## Measured end to end

| measurement | result |
|---|---|
| Production diff | **7 insertions, 32 deletions (net −25)**, one file |
| Suite | 780 passed / 1 failed — the failure is one test pinning the old message text, updated as part of the change |
| Differential vs `master`, 15,407 inputs, by exception type **and** message | **967 differences, every one accounted for** |
| **Loosenings (rejected → accepted)** | **0** |
| **Hangs, either head** | **0** |

| count | transition | meaning |
|---|---|---|
| **57** | `OK` → `ScriptParserException` | **the tightening** — exclusively `$a[…)` (39) and `if(…]` (18), i.e. precisely the two former `ParseParameters` call sites |
| 655 | `Parameter list not terminated` → `Expected ']' \| ')' to end the parameter list.` | EOF message now comes from the list parser |
| 182 | `Unexpected token in parameter list…` → `Unexpected token ']' in parameter list…` | guard message names the character |
| 33 | `Right hand side operand expected` → `Mismatched closing token ')' …` | closer rejected before expression folding |
| 32 | `A parameter was expected` → `Unexpected token ']' …` | fails earlier, more specifically |
| **9** | `IndexOutOfRange`/`ArgumentOutOfRange` → `ScriptParserException` | **improvement** — nine raw-exception leaks become clean parse errors |

Break table (B1–B6) is §10.3 of the repo doc. **API surface: no change** — both methods are private. The break is at the **script-source** level; ships in **1.2.0** alongside `ParseTimeout` default-on.

## Unplanned finding — the #2901 family is far larger than its title (§4.8)

The differential recorded exception *type*, so it censused the corpus:

| head | `ScriptParserException` | `IndexOutOfRangeException` | `ArgumentOutOfRangeException` | parses |
|---|---|---|---|---|
| `master` | 12,271 | **1,359** | **1,185** | 592 |

**2,544 of 15,407 inputs — 16.5% — make `ScriptParser.Parse` throw a raw runtime exception on `master`.** The three shortest reproducers are the single characters **`(`**, **`.`** and **`'`**. Spread across at least the `$a[`, `{"a`, `$a=`, `f(1`, `$a.`, `if(` and `$x=` contexts.

#2901 is filed as *"parse-time IndexOutOfRange/NRE at EOF in the `new` branch"*. That understates it by a wide margin. Attach this census to #2901 as scoping input.

## #2901 not closed — and that is the honest test

Measured on the final design: `new`, `new `, `$a=new` still throw `NullReferenceException`. This design enforces **liveness** (*always terminates*); #2901 is **safety** (*terminates inside its own exception domain*). Different invariants; a mechanism for one has no purchase on the other. The fold does incidentally close **nine** family members.

**Operator-confirmed (Q3): widen Leg 2's predicate** from *"terminates"* to *"terminates with a parse result or a `ScriptParserException`"* **in the PR that fixes #2901**, so it becomes that fix's own regression pin — not now behind a suppression list. Filed as F2.

## Unchanged from revision 1

- **Standing termination sweep (Leg 2)** run under `ScriptLimits.None` remains the seventh-loop answer. ⚠ **Its alphabet and context list are PROVISIONAL** — derived from #9397's probes, not from the parser's true dispatch table. A red-team battery is enumerating that now; reconcile before M5 is done (R1/F4).
- **The eight `done = true` escapes and their `null` are untouched.** (#9426 says nine; `:1054` belongs to `ParseOperator`, not `ParseCore`.) Only two of the eight can return no-progress from entry. The *"not mine"* capability is carried by each caller's own peek, not by the null.
- `ScriptLimits.ParseTimeout` stays, default-on.
- `docs/architecture/parse-core-no-progress-coupling.md` is **deleted** — its subject method no longer exists. **#9412 closes as superseded.**
- No new type, no new abstraction, no new config knob.

## Revision 1's R2 is WITHDRAWN, not granted

Revision 1 raised the `ParseParameters` guard being unreachable-and-unpinnable as a genuine #275 conflict and proposed a two-step mutation as a substitute pin. **The fold deletes that guard entirely.** The surviving `ParseTokenList` guard is reachable and load-bearing under ordinary single-site mutation (measured above). **The two-step mutation apparatus is dropped, and no #275 test-impossible sign-off was requested or given** — #275 reserves that exception to Toni personally, and the question was removed by the ruling rather than waved through. Recorded explicitly so a later reader does not infer a waiver.

## Still open

1. **Leg 2's alphabet is provisional (R1/F4)** — awaiting the red-team's dispatch enumeration. Not blocking. The design's principal residual risk.
2. **Does #2901 get rescoped to the family, or does the family get its own task** with #2901 as one member? 16.5% with one-character reproducers is not what the title describes.
3. **B2–B5 are message-only breaks with 902 corpus hits.** Is any published consumer known to string-match parser messages? If no (expected), the break table suffices.
