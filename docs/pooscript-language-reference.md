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
| restrict to expressions-only / sandbox | ✓ (§12) | built-in execution timeout | ✗ (host must impose one) |

Statement terminators (`;`) are **optional**. Variables are `$name` to declare/assign, `name` (bare) to read (the `$` is a convention, not required).

---

## 2. Execution model

- **Pipeline:** source → parser builds a **tree of tokens** → each token executes to a value (and can be re-rendered to source by the formatters).
- **Run it:** `IScriptParser parser = new ScriptParser(); IScript s = parser.Parse(code); object v = s.Execute(vars);` (see §11 for `vars`). Typed: `s.Execute<int>(vars)`. Async: `s.ExecuteAsync(vars, ct)`.
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
- **Casts:** function-style `int("722")`, `decimal("722")`, `string(722)` (built-in numeric/bool/char/string), and `cast(value, "type"[, default])` (supports `type[]` and `Namespace.Type,Assembly`).
- **Types:** a bare registered type name is a `System.Type` (`int` → `typeof(int)`, `int[]`); `typeof(expr)` gives a value's runtime type. Built-in type names: `list, dictionary, bool, byte, sbyte, char, string, short, int, ushort, uint, ulong, long, float, double, decimal, object`.
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

`IExtensionProvider`: `AddExtensions<T>()` / `AddExtensions(Type)`. Convention: **public static** methods; the **first parameter is the extended type** (the class need **not** be `static`, no `this` keyword). Generic first param → indexed under the generic definition (applies to all `IEnumerable<>`). Extension and instance methods compete on equal footing in resolution (best score wins).

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

Plain objects you bind as globals. `TaskHost`: `Run(lambda)`, `FromResult`, `WaitAll` — used with `await` and lambdas (`task.run([] => {…})`). `TypeHost(parser.Types)`: `Create("TypeName", { …dict… })` builds a registered type from a dictionary (⚠ sandbox-weakening — expose deliberately).

### 11.7 Custom operators — `parser.OperatorTree`

`Add("symbol", Operator)`, `Clear()`. You can **remap/override** operators built from the whitelisted chars `= ! ~ < > / + * - % & | ^`, or clear-and-restrict the set. You **cannot** add an operator using a character outside that set (e.g. `?`, `:`) — that needs library changes.

---

## 12. Restriction / sandboxing

**Default sandbox (allow-list).** A fresh parser with nothing registered can do literals, operators, interpolation, arrays/dictionaries, control flow, and casts among pre-registered primitives — and **nothing else**: no reflection, no `Type.GetType`, no file/network/process, no static members, no arbitrary `new`. To **widen** the sandbox you register a type/extension/variable; to **restrict** it you register nothing. There is no per-method deny-list — restriction is by non-exposure. ⚠ Once you expose an object, the script reaches its whole public instance API and any object graph returned from it — expose narrow facade objects for a tight sandbox.

**Toggle flags (on `ScriptParser`):**

| Property | Default | Effect when off |
|---|---|---|
| `ControlTokensEnabled` | `true` | **expression-only** language — all flow control (`if/for/foreach/while/switch/return/throw/break/continue/using/try/wait/…`) disabled. This is the "restricted parser". |
| `TypeInstanceProvidersEnabled` | `true` | no `new` at all (parse error) |
| `TypeCastsEnabled` | `true` | no `int("7")` / `cast(…)` (parse error) |
| `ImportsEnabled` | `true` | no `import` |
| `AllowSingleQuotesForStrings` | `false` | (on ⇒ `'…'` is a string, not a char) |
| `MetatokensEnabled` | `false` | (on ⇒ keep comments/newlines for tooling/round-trip) |

**Execution safety.** Cancellation via `ExecuteAsync(vars, ct)` → `ScriptContext.CancellationToken`: `foreach` checks it each iteration; **`wait` ignores it** — it calls `Thread.Sleep()` directly and cannot be cancelled mid-wait. **No built-in timeout** — a runaway `while(true)` will block the host thread; impose your own timeout at the host level (note: a tight CPU loop or `wait` will not observe cancellation). Errors surface as `ScriptParserException` (parse) / `ScriptRuntimeException` (execute); script `throw` and downstream .NET exceptions propagate (`await` unwraps inner exceptions).

---

## 13. Compiled path (`ParseDelegate`) — supported subset

Compiling to a LINQ delegate is faster but covers a subset; unsupported tokens throw `NotSupportedException` at compile time.

**Supported:** statement blocks, `+ - * / % << >> & | ^` and compound-assigns, `= == != < <= > >= && ||`, `! ~ - ++ --`, values/variables/indexer/member/method/array, grouping, string interpolation, `return`, `if`, ternary, `??`, `?.`, `switch`, `try`, `while`, `for`, `foreach`, type tokens.

**NOT supported (use `Execute()` instead):** `^^`, `<<<`/`>>>` rotate, regex `~~`/`!~`, `new`, lambdas, `break`/`continue`, `throw`, `using`, `wait`, `await`, `import`, `cast`/`typeof`/`parameter`.

---

## 14. Known limitations & gotchas (user-facing)

- **`foreach` + `continue` is broken:** `continue`/`continue(n)` inside a `foreach` body is silently ignored — the loop continues as if no `continue` was issued. Root cause: the body result is checked for `Break` but the `Continue` check inspects the iteration value instead of the body result. `for`/`while` handle `continue` correctly. Executable proof: `Scripting.Tests/` suite.
- **Shifts/rotates are bitwise, not masked/arithmetic** (see README operator table): `8<<32` → `0`; `-1>>1` → `int.MaxValue` (logical, fills zero); rotates wrap.
- **`d` suffix = decimal, not double** — `3.5d` is a `decimal`; changes overload resolution and arithmetic result types.
- **char/small-int promote to int** for overload selection; numeric widening drives resolution and can surprise.
- **`{…}` block-vs-dictionary ambiguity** — wrap a dictionary in an extra `{ }` when it sits in a body position.
- **No execution timeout** — host must add one. `wait` is non-cancellable (calls `Thread.Sleep()`).
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
