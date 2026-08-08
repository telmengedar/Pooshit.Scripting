# Pooscript — Language & Extensibility Reference

**The authoritative, code-derived reference for the pooscript language** (repo `Pooshit.Scripting`, NuGet `Pooshit.Scripting`; formerly "NC-Script"/NightlyCode). Everything below is verified against the engine source (`Pooshit.Scripts/`) and the executable test suite (`Scripting.Tests/`) — no guesswork. When something is unclear, the test suite is the ground truth.

> **Two execution paths.** The **interpreter** (`script.Execute()`) supports the *entire* surface below. The **compiled** path (`parser.ParseDelegate<Func<…>>(…)` → LINQ delegate) supports a **subset** (§13). Assume the interpreter unless you compile a delegate.

---

## 1. Quick answers — "does pooscript have…?"

| Feature | ? | Feature | ? |
|---|---|---|---|
| `if` / `else` / else-if | ✓ | `switch` / `case` / `default` | ✓ (multi-value & non-constant cases, no fall-through, no `break` needed) |
| `while`, `for`, `foreach` | ✓ | `break(n)` / `continue(n)` (multi-level) | ✓ (⚠ `continue` in `foreach` is currently broken — §14) |
| `return` (value or last-value) | ✓ | `try` / `catch`, `throw` | ✓ |
| ternary `a ? b : c` | ✓ | null-coalescing `a ?? b` | ✓ |
| null-conditional `a?.b` | ✓ | lambdas `x => …` (closures) | ✓ |
| arrays `[…]`, dictionaries `{k:v}` | ✓ | string interpolation `$"…{x}…"` | ✓ |
| method/property/indexer calls | ✓ | generics `m<T>()`, `ref`/`out` params | ✓ |
| `new Type(…)` + object initializer | ✓ | casts / `typeof` | ✓ |
| `using` (dispose), `wait`, `await` (async) | ✓ | custom host functions/types | ✓ (host registers them) |
| `import` external scripts | ✓ (host must wire an `ImportProvider`) | regex match `~~` / `!~` | ✓ (interpreter only) |
| restrict to expressions-only / sandbox | ✓ (§12) | built-in execution timeout, step limit, regex timeout | ✓ (§12, via `parser.Limits`; regex timeout is **on by default** since v1.0, execution timeout and step limit stay opt-in) |

Statement terminators (`;`) are **optional**. Variables are `$name` to declare/assign, `name` (bare) to read (the `$` is a convention, not required).

---

## 2. Execution model

- **Pipeline:** source → parser builds a **tree of tokens** → each token executes to a value (and can be re-rendered to source by the formatters).
- **Run it:** `IScriptParser parser = new ScriptParser(); IScript s = parser.Parse(code); object v = s.Execute(vars);` (see §11 for `vars`). Typed: `s.Execute<int>(vars)`. Async: `s.ExecuteAsync(vars, ct)`. A synchronous, cancellable overload also exists for hosts that cannot await a `Task` (eg. a frame-locked update loop): `s.Execute(vars, ct)` / `s.Execute<int>(vars, ct)` — see §12.
- **Statement separation:** newline or `;`, both optional/mixable. An empty body after a header: `for(…);`.
- **Comments:** `// line` and `/* block */`. Only recognized when `/` starts a statement/expression (mid-expression `/` is division). Discarded unless `parser.MetatokensEnabled = true`.
- **`{}` block vs `{k:v}` dictionary:** `{` is a **dictionary** at the top level, after an operator (e.g. after `=`), or as a value; it's a **statement block** as a control-flow/lambda body. To return a dictionary from a body position, wrap it: `if(c) { { "k":"v" } }`. Empty `{}` = empty dictionary (valid); an empty statement block throws.

---

## 3. Values & literals

**Numbers** (suffix decides the .NET type — note `d` = **decimal**, unlike C#):

| Literal | Type | | Literal | Type |
|---|---|---|---|---|
| `7` | int | | `3.5` | double |
| `7u` / `7l` / `7ul` | uint / long / ulong | | `3.5f` | float |
| `7s` / `7us` | short / ushort | | `3.5d` | **decimal** |
| `7b` / `7sb` | byte / sbyte | | `0xFF` / `0o17` / `0b1011` | hex / octal / binary |

The `d` suffix (decimal) applies to floating-point literals only (i.e. the literal must contain a `.`). Integer literals do not support a `d` suffix.

**Other literals:** `true`/`false`; `null`; char `'a'` (escapes `'\t' '\n' '\r'`; promotes to int in numeric context); strings `"…"` — **literal embedded newlines/tabs allowed**, escapes `\t \n \r`, `\X`→`X` (so `\"`, `\\`). Optional `parser.AllowSingleQuotesForStrings = true` makes `'…'` a string. Arrays `[a, b, c]` (empty `[]`, nesting, expression elements). Dictionaries `{ "k": v, "k2": v2 }` (expression keys/values, comma- or newline-separated).

---

## 4. Variables

`$name = expr` declares/assigns; `name` (bare) and `$name` reference the **same** slot. Assignment returns the assigned value. Scope spans the whole script including `catch` bodies and lambda closures. Host-supplied variables are read the same way (§11).

---

## 5. Operators & precedence

Lower rank binds **tighter**. Full core set (README table has the arithmetic/bitwise/logical core):

| Rank | Symbols | |
|---|---|---|
| 0 | `++` `--` (pre/post) | tightest |
| 1 | unary `!` `~` `-` | |
| 2–5 | `/` `*` · `%` · `-` · `+` | arithmetic |
| 6–8 | `&` · `\|` · `^` | bitwise and/or/xor |
| 9 | `<<` `>>` `<<<` `>>>` | shift / rotate (all equal rank; **bitwise, not arithmetic — §14**) |
| 10 | `< <= > >= == != <>` · `~~` `!~` | comparison · regex match/not-match (all equal rank) |
| 11–13 | `&&` · `\|\|` · `^^` | logical and/or/xor (`&&`,`\|\|` short-circuit) |
| 14 | `??` | null-coalescing (short-circuit) |
| 15 | `=>` | lambda |
| 16 | `=` `+=` `-=` `*=` `/=` `%=` `<<=` `>>=` `&=` `\|=` `^=` | assignment / compound-assign (loosest) |

**Ternary / null-coalescing / null-conditional** (parsed specially, not in the fold table):
- `cond ? a : b` — right-associative, short-circuits (only the taken branch runs); grafts under assignment (`$x = c ? a : b`).
- `a ?? b` — returns `a` if non-null else `b`; short-circuits `b`; chains left-to-right.
- `a?.b` — **C#-faithful**: if the receiver is null the whole continuation is null; the receiver is evaluated once. Only the immediate `?.` guards — a following plain `.` still dereferences (`$a?.Inner.Name` is null when `$a` is null but **throws** when `$a` is non-null and `Inner` is null; use `$a?.Inner?.Name` to guard both). Not assignable; can't lead an expression.

Operators are host-remappable via `parser.OperatorTree` (§11.7). Grouping: `( … )`.

---

## 6. String interpolation

`$"…{expr}…"` embeds any expression. Escape a brace by doubling (`{{` → `{`). Format spec inside the hole: `$"{$value:F2}"` (InvariantCulture). Bonus: `expr:FORMAT` is a **general formatting operator** anywhere — it rewrites to `expr.ToString(FORMAT, InvariantCulture)`.

---

## 7. Member / method / indexer access

- `host.member` — property/field, **or a dictionary entry as a property** (`$d.number` ↔ key `"number"`), resolved **case-insensitively**.
- `host.method(args)` — resolved by `MethodResolver`; **method names are case-insensitive / lower-cased** (`x.someMethod()` binds to `SomeMethod`). Overloads chosen by argument type; **optional/default params**, **`params` arrays**, **generics** `m<T>(…)`, **`ref`/`out`** via `ref($var)`, and **automatic argument conversion** (string→Guid/enum/number, dictionary→typed object, implicit operators) are all supported.
- `host[index]` — arrays, lists, dictionaries.
- `a?.b` — null-conditional (§5).
- **Bare `name(args)`** invokes a variable/imported delegate (implicit `.invoke`).

---

## 8. Instantiation, casts, types

- `new Type(args)` — the type must be registered (`parser.Types.AddType<T>()`) or a built-in; name is **lower-cased** (`new datetime(2012,9,4)`). **Object-initializer:** `new ComplexType { "Prop": v, "Nested": {…} }`.
- **Casts:** function-style `int("722")`, `decimal("722")`, `string(722)` (built-in numeric/bool/char/string), and `cast(value, "type"[, default])` (supports `type[]`; resolves against registered types and primitives only — a host needing a specific type registers it with `AddType<T>()`).
- **Types:** a bare registered type name or `typeof(expr)` yields a `ScriptType` — an opaque, inert handle (`Name`, `FullName`, equality, `ToString()`; no reflective surface) — never a live `System.Type` (`int` → the handle for `int`, `int[]`). Built-in type names: `list, dictionary, bool, byte, sbyte, char, string, short, int, ushort, uint, ulong, long, float, double, decimal, object`.
- `parameter($var, "type"[, default])` declares a typed host-supplied parameter.

---

## 9. Lambdas

`params => body`: single `$x => $x.Value`, bracketed `[$a,$b] => {…}`, zero-arg `[] => expr`. Body = expression or `{ block }`. Usable anywhere a delegate is expected (LINQ-style extensions `$c.where($s => $s>4)`, `task.run([] => {…})`, or `.invoke()`). Lambdas capture and can mutate outer variables (closures).

---

## 10. Control flow — every construct

Gated by `parser.ControlTokensEnabled` (default true). Headers use **comma-separated** args; bodies are the next statement or a `{ block }`.

| Construct | Syntax |
|---|---|
| if / else | `if(cond) body` · `else body` · `else if(…)` |
| while | `while(cond) body` |
| for | `for(init, cond, step) body` — **commas, not semicolons** |
| foreach | `foreach($item, collection) body` |
| switch | `switch(expr)` then `case(v1,v2,…) body` … `default body` — no braces, cases multi-valued & non-constant, no fall-through |
| break / continue | `break` / `break(n)` · `continue` / `continue(n)` |
| return | `return(v)` · bare `return` returns the **last evaluated value** (or null) |
| throw | `throw("message")` · `throw("message","data")` |
| try / catch | `try body catch body` — exception exposed as `$exception` (`$exception.message`) |
| using | `using($disposable) body` · `using($a,$b) { … }` — disposes after body |
| wait | `wait(ms)` (number) · `wait("h:m:s.fff")` (TimeSpan string) |
| await | `await(task)` — unwraps `Task`/`Task<T>`, rethrows inner exceptions |
| import | `$m = import("resource.path"); $m.invoke(args)` or `$m(args)` — needs `ImportsEnabled` + a set `ImportProvider` (§11.5) |
| ref | `ref($assignable)` for ref/out arguments (§7) |

---

## 11. Extensibility — the host-side API

**Mental model:** a host builds a `ScriptParser`, configures **parse-time capability** on it (types, extensions, imports, option flags, operators), then supplies **run-time state** (global variables) *per execution* to `Execute(…)`. The engine is an **allow-list sandbox**: a script can only touch what the host explicitly hands it.

### 11.1 Global variables — supplied at Execute time (NOT the constructor)

`ScriptParser` has only a **parameterless** constructor. Inject host objects when you execute:

```csharp
IScriptParser parser = new ScriptParser();
IScript script = parser.Parse("http.get(\"http://example.com/\")");

// Option A — VariableProvider wrapping named Variable objects
script.Execute(new VariableProvider(new Variable("http", new HttpProvider())));

// Option B — dictionary
script.Execute(new Dictionary<string, object> { ["http"] = new HttpProvider() });
```

`Variable(string name, object value)`. `VariableProvider(params Variable[])` / `(IDictionary<string,object>)` / `(parent, …)` for scope chaining. The bound object is a **capability handle** — its public instance methods/properties are callable/settable; the script cannot replace the object itself.

### 11.2 Register types (`new`) — `parser.Types`

`ITypeProvider`: `AddType<T>(name=null)`, `AddType(Type, name)`, `AddType(name, ITypeInstanceProvider)`, `RemoveType`, `HasType`. Name defaults to `Type.Name.ToLower()`. Enables `new`, constructor-overload resolution, and **instance** member dispatch. **Static members are never exposed** — expose static-like behavior as a bound host object instead. Primitives (`int`, `string`, `list`, `dictionary`, …) are pre-registered.

```csharp
IScriptParser parser = new ScriptParser();
parser.Types.AddType<DateTime>();
IScript script = parser.Parse("new datetime(2012,9,4)");
script.Execute();
```

### 11.3 Extension methods — `parser.Extensions`

`IExtensionProvider`: `AddExtensions<T>()` / `AddExtensions(Type)`. Convention: **public static** methods; the **first parameter is the extended type** (the class need **not** be `static`, no `this` keyword). Generic first param → indexed under the generic definition (applies to all `IEnumerable<>`). Extension and instance methods compete on equal footing in resolution (best score wins). A method (extension or instance) may declare a trailing `ScriptContext` parameter — never first, since the first parameter of an extension method determines the extended type — and the engine supplies it automatically; it consumes no script argument and is invisible to script call sites. Use it to observe cancellation/step-limit guards (§12) inside a host method.

```csharp
IScriptParser parser = new ScriptParser();
parser.Extensions.AddExtensions<StringExtensions>();
IScript script = parser.Parse("\"test\".beautify()");
script.Execute();
```

### 11.4 Method resolution — what callers can rely on

Case-insensitive names; overloads by argument type; optional/default params; `params` arrays; generic methods; `ref`/`out`; automatic argument conversion (numbers, string→Guid/enum, dictionary→typed object, implicit operators). Results cached (caching is on by default).

### 11.5 Import external scripts — `IImportProvider` + `import`

`parser.ImportProvider = …` (and `ImportsEnabled`, default true). Built-ins: `FileMethodProvider` (import a `.ns` file), `ResourceScriptMethodProvider(assembly, parser)` (embedded resource). `import(...)` returns a callable; `$m.invoke(args)` or `$m(args)` runs it (`arguments` bound inside). Without a provider set, `import` throws a parse-time error.

### 11.6 Hosts — `TaskHost`, `TypeHost`

Plain objects you bind as globals. `TaskHost`: `Run(lambda)`, `FromResult`, `WaitAll` — used with `await` and lambdas (`task.run([] => {…})`). `WaitAll` (like several `EnumerableExtensions` methods, §12) declares a trailing `ScriptContext` parameter that the engine injects automatically to observe cancellation — it is invisible to script code (`task.waitall($tasks)` still takes one script-visible argument) and only matters if you call these methods directly from C#. `TypeHost(parser.Types)`: `Create("TypeName", { …dict… })` builds a registered type from a dictionary (⚠ sandbox-weakening — expose deliberately).

### 11.7 Custom operators — `parser.OperatorTree`

`Add("symbol", Operator)`, `Clear()`. You can **remap/override** operators built from the whitelisted chars `= ! ~ < > / + * - % & | ^`, or clear-and-restrict the set. You **cannot** add an operator using a character outside that set (e.g. `?`, `:`) — that needs library changes.

---

## 12. Restriction / sandboxing

**Default sandbox (allow-list + structural refusal).** A fresh parser with nothing registered can do literals, operators, interpolation, arrays/dictionaries, control flow, and casts among pre-registered primitives — and **nothing else**: no file/network/process, no static members, no arbitrary `new`. The sandbox restricts by non-exposure **and** by a structural refusal of the reflection type-family: script can never obtain a live `System.Type`/`System.Reflection.*` object as a callable surface — `typeof`/`getType()` yield an opaque, inert `ScriptType` handle; method/member/indexer resolution refuses reflective receivers; `cast`/`parameter` resolve type names against registered types and primitives only, never `Type.GetType`/`Assembly.Load`. **The security goal is a boundary a correctly-configured script cannot escape** — a script may *crash* its own execution, but must never break out to the filesystem, network, process, or any capability the host did not hand it. The known reflection/RCE escape is closed and the boundary is actively red-teamed toward that goal; an escape from a tightly-configured host is a **defect to fix**, not an accepted limitation. The one thing outside the engine's control — as with every sandbox — is a host that **willingly** exposes a hole (see the delegate caveat below); that is host misconfiguration, not an engine gap. (Process/container isolation is a prudent *additional* layer for the highest-risk deployments, not a concession that the in-process engine cannot be a boundary.) To **widen** the sandbox you register a type/extension/variable; to **restrict** it you register nothing. There is no per-method deny-list — restriction is by non-exposure. ⚠ Once you expose an object, the script reaches its whole public instance API and any object graph returned from it — expose narrow facade objects for a tight sandbox. ⚠ A delegate is a callable value, not a reflective one, so a host member that returns or exposes a `Func`/`Action`/delegate bound to a dangerous sink (e.g. a `Func<string,string>` wrapping `File.ReadAllText`) is directly invokable from script — this is host-side exposure, not a sandbox gap; do not expose delegate members bound to file/process/network operations.

**Toggle flags (on `ScriptParser`):**

| Property | Default | Effect when off |
|---|---|---|
| `ControlTokensEnabled` | `true` | **expression-only** language — all flow control (`if/for/foreach/while/switch/return/throw/break/continue/using/try/wait/…`) disabled. This is the "restricted parser". |
| `TypeInstanceProvidersEnabled` | `true` | no `new` at all (parse error) |
| `TypeCastsEnabled` | `true` | no `int("7")` / `cast(…)` (parse error) |
| `ImportsEnabled` | `true` | no `import` |
| `AllowSingleQuotesForStrings` | `false` | (on ⇒ `'…'` is a string, not a char) |
| `MetatokensEnabled` | `false` | (on ⇒ keep comments/newlines for tooling/round-trip) |

**Execution safety.** The engine guarantees an interruption checkpoint at every point where control returns to the engine (every statement, every loop iteration, every lambda invocation, every enumerated element) — it guarantees nothing *inside* a single host call. Concretely:

- **Cancellation is complete.** `ExecuteAsync(vars, ct)` / the sync `Execute(vars, ct)` overload propagate `ct` (or, on the parameterless overloads, `CancellationToken.None`) into every checkpoint: `while`/`for`/`foreach` per iteration, every statement in a block, every lambda invocation (`task.run` bodies, `.where()`/`.indexof(predicate)` callbacks and any host extension that invokes a script lambda), the pure-iteration `EnumerableExtensions` methods (`count`, `order`, `toarray`, …), `wait` (now an **interruptible** sleep — cancelling mid-wait returns immediately instead of blocking for the full duration), `await`/`task.waitall` (the awaited/joined tasks themselves are the host's and are **not** cancelled — only the script unwinds), and an **imported script** (`import(...)`) — it inherits the caller's token, so it can no longer run in a completely uncancellable region.
- **`try`/`catch` cannot swallow an engine cancellation.** This is an intentional, breaking behavior change: previously a script could wrap cancellable work in `try { … } catch($e) { }` and the host would see `ExecuteAsync` complete *successfully* despite the cancel. Now, when the context's own token is the one that was cancelled, the `OperationCanceledException` (and a step-limit abort) rethrows through any surrounding `try`/`catch`. A task cancelled by a host's own *unrelated* token is unaffected and remains ordinary, catchable script control flow.
- **Six guards on `parser.Limits`** (`ScriptLimits`). **Since v1.0 the default is `ScriptLimits.Default` — secure by default:** `MaxDepth = 10`, `MaxParseDepth = 100`, `MaxVariableBytes = 128 MiB` and `RegexTimeout = 1s` are set out of the box (the four *uncatchable-or-uninterruptible* failures — parse-time `StackOverflow`, runtime `StackOverflow`, OOM, and unbounded regex backtracking; `MaxSteps`/`Timeout` stay `null`, since an ordinary script hang is recoverable via the token). A host wanting the pre-1.0 unbounded behaviour sets `parser.Limits = ScriptLimits.None` (a deliberate opt-out); a host wanting different bounds builds `new ScriptLimits { … }`, referencing `ScriptLimits.DefaultMaxDepth`/`DefaultMaxParseDepth`/`DefaultMaxVariableBytes`/`DefaultRegexTimeout` for knobs it keeps. The knobs:
  - `Timeout` (`TimeSpan?`) — a wall-clock deadline, realised as a linked cancellation token so every checkpoint above already honors it for free, on **both** the sync and async paths. On expiry (with no caller cancellation) the host sees `ScriptTimeoutException`; a genuine caller cancel still surfaces as `OperationCanceledException`/`TaskCanceledException` — the two are always distinguishable.
  - `MaxSteps` (`long?`) — bounds *script-driven looping* by counting engine checkpoints (not instructions); overrun throws `ScriptStepLimitExceededException`. Does not bound a single expensive host call, a huge allocation, or a runaway regex.
  - `RegexTimeout` (`TimeSpan?`, defaults to **1 second**) — bounds a single `~~`/`!~` match; on timeout the host sees a `ScriptRuntimeException` wrapping a `RegexMatchTimeoutException`, carrying the offending token's source position. This is the **only** in-process mechanism against catastrophic regex backtracking — the backtrack runs inside one native `Regex` call, so no checkpoint runs between engine steps and **no caller `CancellationToken` can interrupt it either**; `MaxSteps`/`Timeout` are no substitute. A host that needs unbounded matching sets this back to `Regex.InfiniteMatchTimeout` explicitly.
  - `MaxDepth` (`int?`) — bounds call depth through lambda invocation and imported-script invocation; overrun throws `ScriptDepthLimitExceededException`. This is the one guard that protects the host process itself rather than just the script: unbounded recursion otherwise produces an uncatchable `StackOverflowException`. **There is no engine-level backstop beneath it and none is planned** — a `RuntimeHelpers.EnsureSufficientExecutionStack`-based probe was built and measured, and dropped: on the measured build, recursion that goes through reflection (a lambda reached through a reflected host method, or an ordinary host-method call chain) went directly from completing normally to a hard process crash, with **no** intervening window in which the probe's exception ever fired. `MaxDepth` is therefore not a ceiling with a safety margin behind it; it **is** the only mechanism watching.
    - **Calibration procedure — measure the abort, not the descent.** Do **not** find "the deepest recursion that completes normally" and subtract a margin: on a measured build, unconfigured recursion completed **664** levels deep before the process crashed, while the safe *aborting* ceiling was only **21** — a ~32× gap. The two numbers are not close, and the descent figure is not a usable proxy for the abort figure, because past the safe ceiling it is **the abort's own unwind that overflows, not the recursion's descent**: throwing `ScriptDepthLimitExceededException` at too great a depth still crashes the process, just one step later than an unguarded crash would have. Instead, **binary-search the largest `MaxDepth` at which a script recursing well past it still aborts cleanly** (with `ScriptDepthLimitExceededException`, not a process crash) — that is the quantity that matters, and it must be measured directly, not inferred from how deep unguarded recursion can go.
    - **One knob, two ceilings — calibrate against the most expensive shape your scripts can reach.** The safe ceiling is not a single number even on one host: on the same measured build, direct `$lambda.invoke(...)` recursion (no reflection since DiVoid #7749) safely aborted up to depth 21, while recursion reached through a *reflected* host callback (a lambda passed to a host extension method, invoked via `MethodInfo.Invoke`) safely aborted only up to depth 16 — reflected dispatch costs more physical stack per level. Calibrating `MaxDepth` against the cheaper, non-reflected shape and then letting scripts reach the more expensive, reflected one (eg. any host extension that accepts and invokes a lambda) can still kill the process even with the guard configured. Measure against whichever call shape in your own host's registered extensions is most expensive, not whichever is easiest to construct a test for.
  - `MaxParseDepth` (`int?`, defaults to **100**) — bounds recursion depth in `ScriptParser.Parse` itself, distinct from `MaxDepth` (which bounds *runtime* call depth and is far too tight for expression nesting — `((1+2)*3)` is already runtime-depth-irrelevant but 3 levels of parse nesting). Deeply nested `(`, `{`/dictionary, or `$f($f($f(...)))` input overflows the native stack **during parsing, before any other limit is consulted** — a `StackOverflowException` here cannot be caught by any host `try`/`catch`, so a host that only *validates* untrusted script (parses without executing) was previously killable by input alone. Overrun throws `ScriptParserException`. Measured on this repo's build: parsing overflows the stack between depth ~600 (Debug, nested calls) and ~1300 (Release, parens/braces) frames; the default of 100 keeps ~6-13× headroom under the tightest measured configuration while comfortably permitting realistic expression nesting. `[`/`]` array nesting is not covered by this guard (it recurses through a different, cheaper-per-frame path and was measured safe to depth 3000+); it remains a residual, lower-severity gap.
  - `MaxVariables` (`long?`) / `MaxVariableBytes` (`long?`) — bound the script's **own** variable footprint (entry count / approximated bytes); overrun throws `ScriptVariableLimitExceededException`. Growth is caught three ways: at value production/assignment, a periodic sampled walk (catching in-place mutation such as `$list.add($existing)`), and a **pre-allocation charge** that charges a collection's requested capacity *before* the backing store is allocated — so `new list(2000000000)`, `list.ensurecapacity(2e9)`, `$list.capacity = n`, and `dict.ensurecapacity(n)` throw a clean exception with **nothing allocated** instead of OOMing the process. A host object graph held by a variable is charged its handle size, not walked into.
    - Full design: `docs/architecture/execution-guards-depth-memory.md`.
- **What the engine still cannot guarantee, even with every guard configured:** a host-supplied method that itself blocks (sync IO, a lock, a raw `Thread.Sleep` in host code); a host-supplied sequence whose single `MoveNext` blocks; a detached `task.run(...)` the script never awaits (its body still observes cancellation and unwinds on its own, but the engine cannot join it); stack overflow from recursion at or beyond `MaxDepth`'s own unwind cost, or from any recursion at all when `MaxDepth` is left unconfigured — see the `MaxDepth` entry above, there is no mechanism watching this beneath the policy ceiling itself. `task.run` bodies get an independent depth budget per task. On **every engine-dispatched invocation** — `$f.invoke(…)` in script, a `task.run` body, and any host extension method that declares a `ScriptContext` parameter and invokes through it — the budget is resolved from whichever context is actually invoking the lambda at the moment of the call rather than from wherever that lambda happened to be defined, so a shared helper lambda captured outside a `task.run` body and invoked from inside it is bounded correctly too, the same as one defined inline. ⚠ **This does not extend to `LambdaMethod.Invoke(params object[])`, the overload host C# code calls directly** — that one resolves the budget from the context the lambda was *defined* in. **A host extension that accepts a `LambdaMethod` must declare a trailing `ScriptContext` parameter** — the engine injects it, it consumes no script argument, and the script-level call is unchanged (§11.3) — **and invoke through `InvokeFrom(context, …)`.** An extension that calls `Invoke(…)` instead charges every concurrent callback to the defining script's single counter, so *N* simultaneous non-recursive callbacks abort a `MaxDepth` of *N-1* with no recursion involved. `Invoke(…)` is correct only where there is no invoking context at all — host code or a test driving a lambda returned from `Execute`; such a host driving one lambda on several of its own threads hits the same shared-counter shape and must size `MaxDepth` above its own concurrency. The memory guard bounds retained growth and collection pre-sizing (above). The one memory shape it does **not** bound is a **single reflected BCL call on a primitive that allocates from a size or format argument** — `"a".padright(2000000000)` builds ~4 GB inside `String.PadRight`, `(1.0).tostring("F100000000")` ~700 MB inside a numeric `ToString`, both entirely inside one call before any checkpoint. The retained result is charged *after* the fact, but the transient peak inside the call is unbounded, so a script can still OOM-crash the host this way. This is a **crash (DoS), never an escape**, and it is not closable by the pre-allocation charge — the size can be a runtime-computed format string, not a capacity argument; full closure needs a reflected-method allow-list or a memory-capped worker process (a post-1.0 task). A script may still crash its execution — the accepted failure mode: crashing is not escaping. This robustness ceiling is distinct from the escape boundary (above), which is the actual security guarantee; for the highest-risk deployments, running the executor in a killable child process bounds even the crash.
- **The compiled path (`ParseDelegate`, §13) has none of this.** It has no `ScriptContext` plumbing at all, so it is **not cancellable** by any mechanism above and must not be used to run untrusted code; use the interpreter (`Execute`/`ExecuteAsync`) instead.

Errors surface as `ScriptParserException` (parse) / `ScriptRuntimeException` (execute) / `ScriptTimeoutException` / `ScriptStepLimitExceededException` / `ScriptDepthLimitExceededException` (guards); script `throw` and downstream .NET exceptions propagate (`await` unwraps inner exceptions). Full design and test plan: `docs/architecture/cancellation-support.md`, `docs/architecture/execution-guards-depth-memory.md`.

---

## 13. Compiled path (`ParseDelegate`) — supported subset

Compiling to a LINQ delegate is faster but covers a subset; unsupported tokens throw `NotSupportedException` at compile time.

**Supported:** statement blocks, `+ - * / % << >> & | ^` and compound-assigns, `= == != < <= > >= && ||`, `! ~ - ++ --`, values/variables/indexer/member/method/array, grouping, string interpolation, `return`, `if`, ternary, `??`, `?.`, `switch`, `try`, `while`, `for`, `foreach`, type tokens.

**NOT supported (use `Execute()` instead):** `^^`, `<<<`/`>>>` rotate, regex `~~`/`!~`, `new`, lambdas, `break`/`continue`, `throw`, `using`, `wait`, `await`, `import`, `cast`/`typeof`/`parameter`.

⚠ **Not cancellable.** The compiled path has no `ScriptContext` plumbing at all — none of §12's cancellation coverage or execution guards apply to it, at any level. Do not use `ParseDelegate` to run untrusted code; use the interpreter (`Execute`/`ExecuteAsync`) instead.

---

## 14. Known limitations & gotchas (user-facing)

- **`foreach` + `continue` is broken:** `continue`/`continue(n)` inside a `foreach` body is silently ignored — the loop continues as if no `continue` was issued. Root cause: the body result is checked for `Break` but the `Continue` check inspects the iteration value instead of the body result. `for`/`while` handle `continue` correctly. Executable proof: `Scripting.Tests/` suite.
- **Shifts/rotates are bitwise, not masked/arithmetic** (see README operator table): `8<<32` → `0`; `-1>>1` → `int.MaxValue` (logical, fills zero); rotates wrap.
- **`d` suffix = decimal, not double** — `3.5d` is a `decimal`; changes overload resolution and arithmetic result types.
- **char/small-int promote to int** for overload selection; numeric widening drives resolution and can surprise.
- **`{…}` block-vs-dictionary ambiguity** — wrap a dictionary in an extra `{ }` when it sits in a body position.
- **`try`/`catch` no longer swallows an engine cancellation** (§12) — an intentional, breaking change. A script that wraps cancellable work in `try { … } catch($e) { }` previously masked a host cancel; now the cancellation rethrows through the `catch`. Unrelated task cancellations (a host task cancelled by its *own* token) are unaffected and remain catchable.
- **`Converter.RegisterConverter` is process-global and not thread-safe** — register converters once at startup, never from concurrent code.

---

## 15. Common misconceptions corrected

1. **`new ScriptParser(new Variable(...))` does not compile** — there is only `new ScriptParser()`; bind globals at `Execute()` time (§11.1).
2. **`$` sigil is optional for reads** — `name` and `$name` resolve to the same slot; `$` is a convention, not a requirement.
3. **Method and type names are case-insensitive** — the engine lower-cases all names internally; `x.SomeMethod()` and `x.somemethod()` are equivalent.
4. **`AddType` does NOT expose static members** — only instance members are reachable after registration. Wrap static APIs in an instance facade object if you need them.
5. **`import` is fully supported at the library level** — the `import` keyword and the full import machinery (`FileMethodProvider`, `ResourceScriptMethodProvider`) are built in. A host that doesn't wire an `ImportProvider` will get a parse error on `import`, but that's a host policy choice, not a language limitation.
6. **`ParserOptions` is largely vestigial** — the real on/off switches are the `ScriptParser` properties listed in §12.

---

## Where to go next

- **Executable proof of every feature:** `Scripting.Tests/` — each construct has dedicated test coverage.
- **Extend the language at the library level:** `Pooshit.Scripts/Control/` for new control-flow tokens, `Pooshit.Scripts/Tokens/` for new value tokens, `Pooshit.Scripts/Expressions/ExpressionBuilder.cs` for compiled-path support.
- **NuGet:** [Pooshit.Scripting](https://www.nuget.org/packages/Pooshit.Scripting)
