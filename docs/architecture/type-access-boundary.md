# Architectural Document: Type-Access Boundary (Opaque Type Handle)

> **Repo working-tree path:** `C:\dev\claude\Pooshit.Scripting\docs\architecture\type-access-boundary.md`
> **DiVoid:** documentation node linked to bug **#7787** (the confirmed escape), fix task **#7788** (`implements`), project **#5**, reference **#2946** (§11/§12). Source of truth for the language surface: **#2946**.
> **Standards:** Design Contracts **#1136** (KISS/DRY/YAGNI + §5 Pre-Design Checklist, walked in §16 below). Code Contracts **#114 §0** governs John's implementation; the comment contract **#114 §4** applies to his code, not to this design.
> **Status:** design only. Do not branch/PR — the operator packages. Implementer: `john-backend-dev`.

---

## 0. The two operator quotes this design is built to satisfy

> "at design time i was aware of the type gap initially (with an actual type in c# you can basically do anything) and i remember that i introduced a wrapped type somehow and probably blocked gettype or returned something else there. I suspect that somewhere along the road … this concept was forgotten or just some mistake in implementation was made. So we totally have to reintroduce that. We need to intercept gettype calls (if we allow them at all) and only allow controlled type access. like a type for a parameter is okay, but the engine should detect that and only there unwrap - every actual type access has to be isolated to a controlled something."

> "Access to real type objects from script is basically a melting core incident."

The design below reintroduces the wrapped type as an opaque handle (`ScriptType`), intercepts `getType()`/`typeof`, keeps the legitimate type positions (`parameter`, `cast`, `new`, generics, `typeof`-comparison) working, and isolates every unwrap to an enumerated set of engine-internal, trusted sinks. No surviving trace of the original wrapped-type concept exists in the code (`TypeToken` unwraps to raw `Type` at execution); this is a reintroduction, not a restoration.

---

## 1. Problem Statement

A pooscript script can obtain a live `System.Type` and reflect from it to arbitrary code. The confirmed chain (bug #7787), reproduced on the **maximally-restricted, fully-guarded default parser** (all sandbox flags off, all four `Limits` guards set):

```
(1).getType().getType().invokeMember("GetType",344,null,null,["System.Environment"]).invokeMember("get_UserName",344,null,null,[])
```

returns the host username, and the same technique reaches `File.ReadAllText`/`WriteAllText`, `Process.Start`, and `Assembly.Load`. This is a catastrophic sandbox-escape / RCE.

**Goal:** make a live `System.Type` (and the `System.Reflection.*` family reachable from it) **unreachable as a callable/reflective surface from script**, while keeping the legitimate positions where script needs to *name*, *compare*, or *pass* a type fully working. Success = every PoC in #7787 throws a contained `ScriptRuntimeException` on a default parser, and the legitimate type tests in `Scripting.Tests` stay green (with the two documented exceptions in §12).

---

## 2. Scope & Non-Scope

**In scope** — the type-access model end to end:
- The opaque type handle representation (`ScriptType`), its script-visible surface, and its engine-internal unwrap.
- Every **source** that today hands script a raw `Type`, converted to hand a handle (or nothing).
- Every **sink** that legitimately needs a real `Type`, and how it unwraps there and only there.
- The **structural boundary**: method / member / indexer resolution refusing to reflect against a raw `Type` / reflection-family receiver, in **both** execution engines (interpreter and compiled `ExpressionBuilder`).
- The **`cast` / `DetermineType` closure** (script-supplied type names resolve against registered types + primitives only).
- `getType()` and `typeof` behaviour.
- The correction to reference #2946 §12 (see §14).

**Explicitly out of scope** (do not entangle):
- The memory guard (#7715 PR 2), the async rewrite (#7713), the `MaxDepth` ceiling calibration (#7748).
- Process/container isolation (see the plain statement in §3).
- Any change to the guard/limits model (`ScriptLimits`), imports, or operators beyond what the boundary requires.
- One design per task — this covers type access only.

---

## 3. Assumptions & Constraints (and the plain truth about isolation)

- **Two execution engines.** The **interpreter** (`IScript.Execute`, the token tree via `ScriptToken.ExecuteToken`) supports the entire surface and is where every #7787 PoC runs. The **compiled path** (`ExpressionExtensions.ParseDelegate` → `ExpressionBuilder` → LINQ delegate) supports a subset (#2946 §13) and is documented "not recommended for untrusted scripts." Both must be closed because both can materialise a raw `Type` (§7).
- **The default parser is the threat surface.** The escape needs nothing registered and no sandbox flags on. The fix must hold on a bare `new ScriptParser()`.
- **The allow-list sandbox model stays.** Restriction is by non-exposure of host objects. This design adds a *structural* refusal for one specific family (reflective types) that the engine was silently exposing via `GetType()`.
- **Plainly stated, and it stays true after this fix:** *the in-process sandbox is NOT, and after this fix is NOT, a substitute for process/container isolation against a determined hostile script author.* This change raises the bar enormously — it closes the confirmed RCE — but defence-in-depth is the goal, not a claim of perfect containment. Any host executing genuinely untrusted community scripts (the "Uberkarl" driver) MUST additionally run them under process/container isolation. #2946 §12's current "allow-list is a boundary … nothing else: no reflection, no `Type.GetType`" text is **false today** and must be corrected (§14).

---

## 4. Architectural Overview

```
                          SCRIPT-VISIBLE WORLD                    │        ENGINE-INTERNAL (trusted)
                                                                  │
   typeof(x) ─┐                                                   │
   getType()  ├─►  ScriptType  (opaque handle)                    │
   int, date  ┘    • Name, FullName (strings)                     │
                   • == / != / Equals (identity)                  │   internal Type Unwrap()  ◄─── only engine code
                   • ToString()                                   │            │
                   • NO reflective surface                        │            ▼
                                                                  │   ┌─── controlled UNWRAP sinks ───┐
   x.someMethod()  ─►  dispatch  ──┐                              │   │  parameter type slot          │
   x.member        ─►  dispatch  ──┤                              │   │  generic arg  m<int>()        │
   x[i]            ─►  dispatch  ──┤──►  STRUCTURAL BOUNDARY       │   │  new / TypeInstanceProvider   │
                                   │     "is receiver a raw Type  │   │  cast target (via names only) │
                                   │      / reflection family?"   │   │  host-return marshalling      │
                                   │        │YES → refuse         │   │  ScriptType → Type converter  │
                                   │        │NO  → resolve        │   └───────────────────────────────┘
                                   │     (+ name-backstop, 2ndary)│
                                   └──────────────────────────────┘
```

Three moving parts:

1. **`ScriptType`** — a small, sealed, opaque handle. It carries the real `Type` in a field that only engine code can read (`internal`). To script it exposes strings (`Name`, `FullName`), value equality, and `ToString()` — and nothing that returns a `Type`, `MemberInfo`, `Assembly`, or invokes anything.
2. **Sources** produce `ScriptType` (or, for parse-time-known positions, never produce a runtime type value at all).
3. **The structural boundary** at every point where the engine reflects over a host object's runtime type refuses reflection-family receivers, so even a leaked raw `Type` is inert as a dispatch target. Beneath it sits a conservative method-name backstop (defence-in-depth).

---

## 5. Components & Responsibilities

| Component | New file / touched file | Owns | Does NOT own |
|---|---|---|---|
| **`ScriptType`** (new) | `Pooshit.Scripts/Data/ScriptType.cs` (new) | Being the sole script-visible representation of a type; identity/equality; string identity (`Name`/`FullName`); the `internal` unwrap accessor | Any reflective capability; construction of instances; resolution of names |
| **`TypeGuard`** (new helper) | `Pooshit.Scripts/Parser/Resolvers/TypeGuard.cs` (new, name at John's discretion) | The single predicate `IsForbiddenReflectiveReceiver(Type)` and the method-name backstop set; the one place the deny-family is defined | Deciding *where* it is called (callers own that) |
| **Sources** | `TypeToken`, `TypeOfToken`, `getType` interception in `ScriptMethod` + `ExpressionBuilder` | Producing a `ScriptType` instead of a raw `Type` | Unwrapping |
| **Sinks** | `ScriptParameter`, `ScriptMethod.CreateGenericParameters`, `TypeInstanceProvider`/`NewInstance`, `ExpliciteTypeCast`, `Converter`, `Script.ConvertResult` | Unwrapping a `ScriptType`/`TypeToken` to a real `Type` at exactly one trusted point each | Exposing the `Type` to script |
| **Boundary enforcers** | `MethodResolver`, `ScriptMember`, `ScriptIndexer`, `ExpressionBuilder.BuildMethod`/member/indexer | Calling `TypeGuard` before reflecting over a host's runtime type | The predicate definition (owned by `TypeGuard`) |
| **`DetermineType`** | `Extensions/TypeExtensions.cs` | Resolving script-supplied type *names* against registered types + primitives only | `Type.GetType` / `AppDomain` scan / `Assembly.Load` (all removed) |

---

## 6. `ScriptType` — what it IS and what it exposes (KISS: the minimum the legitimate cases need)

`ScriptType` is a `sealed class` holding one private/`internal` `Type` field. It is the **only** thing `typeof`, `getType()`, and a bare registered type name (`int`, `datetime`) evaluate to as a script value.

**Script-visible surface — deliberately minimal, each member justified:**

| Member | Kind | Why it earns its place |
|---|---|---|
| `Name` | `string` property | The task requires "reading a type's name"; identity display. Capability-free. |
| `FullName` | `string` property | The pre-fix raw `Type` exposed `.FullName`; community scripts plausibly read it for logging/branching. A string, zero capability. |
| `Equals(object)` / `GetHashCode()` | overrides | Value equality by the wrapped `Type`. Required for `typeof($x) == int` (§8). |
| `operator ==` / `operator !=` | operators | The equality operator path evaluates `(dynamic)lhs == (dynamic)rhs` (`Operations/Comparision/Equal.cs:15`); handle-to-handle comparison must be value equality. |
| `ToString()` | override | Returns `FullName` (falling back to `Name`). Used by string interpolation — `ExtractTokenTests.cs:70` interpolates `{$Parcel.gettype()}`. |

**Explicitly NOT exposed** (this is the whole point): no `Type`, no `Assembly`/`Module`, no `GetMethods`/`GetMembers`/`GetFields`/`GetConstructors`, no `InvokeMember`/`Invoke`, no `MakeGenericType`/`MakeArrayType`, no `BaseType`/`GetInterfaces`, no `IsClass`/`IsArray`/…, no `GetType()`-returning-a-Type. `getType()` on a `ScriptType` returns another `ScriptType` (§9), never a raw `Type`.

**Engine-internal unwrap:** an `internal Type Unwrap()` (or `internal Type WrappedType { get; }`). Because it is `internal`, it is invisible to script reflection — `MethodResolver` uses `GetMethods()` (default `Public|Static|Instance`) and `ScriptMember` uses `GetProperties()` (default `Public`), neither of which return `internal` members. So `ScriptType` is inert-by-construction: script can only reach the five public members above; only engine code (same assembly) can unwrap.

**KISS / YAGNI decisions (math per #1136 §1):**
- **No general capability system.** A `sealed` handle + a fixed unwrap-sink list is the whole model. A per-parser "allowed type-operations" policy object was considered and **deleted**: it adds a config surface (§3 of #1136 — "configurability is not free") with no named operator who would tune it and no environment that differs. The default-deny opaque handle covers the requirement. Re-add only if a concrete host need surfaces.
- **`Namespace` dropped.** `Name` + `FullName` cover identity and the documented legitimate reads; `Namespace` has no named need and is `FullName`-derivable. YAGNI — re-add on a concrete script that needs it.
- **No `IsAssignableFrom` / `is` / `as` on the handle.** pooscript has no `is`/`as` operator (#2946 §5 operator table); the identity check is `==`. `cast(x, "type")` already covers instance-of coercion (§8) and does not need the handle. Adding assignability methods would be speculative surface. YAGNI.

---

## 7. The three confirmed doors — explicit design answer for each

### Door 1 — `typeof(x)` leaks a raw `Type` (`Tokens/TypeOfToken.cs:25`)
`ExecuteToken` returns `result?.GetType()` (a live `RuntimeType`).
**Answer:** return `ScriptType.Of(result?.GetType())` (null-safe: a null result yields a null handle, preserving today's null-on-null behaviour). `typeof` stays available; it now yields an inert handle. This is the *only* change to `TypeOfToken`.

### Door 2 — `Object.GetType()` is dispatched as an ordinary method (no `typeof` needed at all)
`(1).getType()` reaches `MethodResolver.Resolve` (`Parser/Resolvers/MethodResolver.cs:52`) and binds `object.GetType()`, returning a live `RuntimeType`. **This is why intercepting `typeof` alone is insufficient.**
**Answer — intercept `getType` at dispatch, in both engines:**
- Interpreter: in `ScriptMethod.ExecuteToken` (`Tokens/ScriptMethod.cs:83`), add an early branch — when `MethodName == "gettype"` and there are zero parameters and the host is not an `IDictionary`/`IExternalMethod`, return `ScriptType.Of(host.GetType())` **before** the resolver is consulted. `getType()` is thereby *allowed* but always yields a `ScriptType`, never a raw `Type`.
- Compiled: in `ExpressionBuilder.BuildMethod` (`Expressions/ExpressionBuilder.cs:531`), same interception — when the method name is `gettype` and no parameters, emit a call/constant producing a `ScriptType` rather than binding `object.GetType()`.
- **Belt:** the structural boundary (§10) independently refuses to bind any method on a raw `Type` receiver, so even if an interception path is missed, the chain dies at the first reflective hop.

Decision on the user's open question ("if a script calls `GetType()` on a wrapped type or on any object, what happens?"): **`getType()` is allowed and always returns a `ScriptType`.** On any object → `ScriptType` of its runtime type. On a `ScriptType` → `ScriptType` of `ScriptType` (inert). This is more backward-compatible than denial (scripts using `$x.getType().name` keep working) and equally safe, because a `ScriptType` has no reflective surface.

### Door 3 — a registered type name executes to a raw `Type` (`Tokens/TypeToken.cs:29`)
A bare `int`/`datetime` is parsed to a `TypeToken` whose `ExecuteToken` returns the raw `Type`. In the compiled path, `ExpressionBuilder.cs:349` emits `Expression.Constant(type.Type)` — a raw `Type` constant.
**Answer:**
- Interpreter: `TypeToken.ExecuteToken` returns `ScriptType.Of(type)`.
- Compiled: line 349 emits `Expression.Constant(ScriptType.Of(type.Type))`.
- The `TypeToken` AST node keeps its `public Type Type` property (parse-time metadata) — sinks that know the type at parse time read it directly and never touch a runtime type value (see §8 K1/K2). This is what makes `parameter`/generics safe without a runtime unwrap.

---

## 8. Interactions & Data Flow — the sinks where the engine unwraps (enumerated from code)

**Enumeration method (for the audit — this list is derived, not remembered):** I grepped every consumer of a `System.Type` that originates from a script type-expression — the symbols `TypeToken`, `TypeOfToken`, `DetermineType`, `ProvidedType`, `GenericParameters`/`MakeGenericMethod`, and every `host.GetType()`-reflection site — across the whole `Pooshit.Scripts` tree; read each token's `ExecuteToken` (and the compiled `Build*` equivalent); and checked `Converter` for `string↔Type` conversions. Both engines were walked. The result is the closed set below. I assess it **exhaustive** because a `Type` can only enter a live reflective operation through (a) a token that evaluates a type expression, (b) a reflect-over-host dispatch site, or (c) a `Converter` conversion — and all three categories are enumerated. The one residual risk is a *new* code path added after this design; §11 names the invariant that guards against that.

### Sources (produce `ScriptType`)
| # | Site | File:line | Change |
|---|---|---|---|
| S1 | `typeof(x)` | `Tokens/TypeOfToken.cs:25` | return `ScriptType.Of(...)` |
| S2 | `getType()` dispatch | `Tokens/ScriptMethod.cs:83` (interp) · `Expressions/ExpressionBuilder.cs:531` (compiled) | intercept → `ScriptType` |
| S3 | bare registered type name (`int`) | `Tokens/TypeToken.cs:29` (interp) · `Expressions/ExpressionBuilder.cs:349` (compiled) | return / emit `ScriptType.Of(...)` |

### Sinks (unwrap to a real `Type` — trusted, engine-internal)
| # | Legitimate position | File:line | How it unwraps *there and only there* |
|---|---|---|---|
| **K1** | `parameter($v, int)` / `parameter($v, "int")` — parameter type slot | `Tokens/ScriptParameter.cs:56` | **Read `Type.Type` directly** (the `TypeToken` AST node's parse-time property) instead of `Type.Execute(context)`. The parameter's type is known at parse; it never needs to become a runtime type value. Replaces the `is not Type` check that would now (wrongly) reject a `ScriptType`. |
| **K2** | `m<int>()` — generic argument | `Tokens/ScriptMethod.cs:62-69` (interp) · `Expressions/ExpressionBuilder.cs:535` (compiled) | Interp `CreateGenericParameters`: accept a `ScriptType` and `Unwrap()` it; keep the existing string→registered-type fallback (`context.TypeProvider.GetType(name)?.ProvidedType`); **remove** the raw `value is Type` acceptance. Compiled already reads `TypeToken.Type` at compile time — unchanged. |
| **K3** | `new datetime(...)` — constructor target | `Tokens/NewInstance.cs:52-54` → `Providers/TypeInstanceProvider.cs:32-35` | **No change.** The `Type` is bound at parse from the registered `Types` set and is never a script value. Already safe. |
| **K4** | `int("722")` — built-in primitive cast | `Tokens/ImpliciteTypeCast.cs:45` | **No change.** Target `Type` bound at parse from the `supportedcasts` primitive whitelist (`Parser/ScriptParser.cs:64-77`); never a script value. Already safe. |
| **K5** | `cast(x, "type")` — dynamic cast target | `Tokens/ExpliciteTypeCast.cs:56` | Target comes from a **string** via `TypeProvider.DetermineType(name)`; the resolved `Type` is used only for `IsInstanceOfType`/`Convert` and is returned to script *never* (the cast returns the value). **No unwrap needed; the door is `DetermineType` itself** — hardened in §B4 below. |
| **K6** | host-return marshalling — `Execute<Type>()`, bare-type script result | `Script.cs:34-47` (`ConvertResult<T>`) → `Extern/Converter.cs` | Add a `ScriptType → Type` unwrap so a script whose result is a type handle still returns the real `Type` to the trusted host (keeps `TypeTokenTests` green — §12). |
| **K7** | passing a type to a host method parameter of type `System.Type` | `Operations/MethodOperations.CreateParameters` via `Extern/Converter.Convert` | Add the same `ScriptType → Type` converter (K6). This is the **controlled replacement** for the removed `string→Type` door (§B4): a host method that wants a `Type` receives it by the script passing `int`/`typeof($x)` (a `ScriptType`, unwrapped), not an arbitrary string. |

**Confidence the sink list is exhaustive:** high. K1–K7 are every place a real `Type` is derived from a script-controlled type expression. K3/K4 need no change (type is parse-time-internal); K1/K2 unwrap at the one point they consume the type; K5 is closed at the resolver; K6/K7 are the two trusted egress points (host return, host-method `Type` parameter). The reflect-over-host dispatch sites are handled separately by the structural boundary (§10), which is not a "type sink" but a receiver refusal.

---

## 9. Contracts & Interfaces (abstract)

**`ScriptType` contract**
- **Input:** a non-null `System.Type` (via an internal factory `ScriptType.Of(Type)`; `Of(null)` returns `null`, preserving null-propagation).
- **Output to script:** `Name`, `FullName` (strings); `==`/`!=`/`Equals`/`GetHashCode` (value identity by wrapped `Type`); `ToString()` (full name).
- **Output to engine:** `internal Type Unwrap()`.
- **Invariant:** two `ScriptType` values are equal iff their wrapped `Type` is reference-equal (CLR interns `Type`, so this is exact identity). No public member returns a `Type`, `MemberInfo`, `Assembly`, `Delegate`, or invokes anything.

**`TypeGuard` contract**
- `bool IsForbiddenReflectiveReceiver(Type receiverType)` → true when `receiverType` is in the deny-family (§10). Pure, allocation-free, O(1) after a static set build.
- `bool IsForbiddenReflectiveMethodName(string loweredName)` → true for the reflection-specific method-name backstop set (§10). Secondary gate.

**Dispatch contract (unchanged externally, hardened internally):** method/member/indexer resolution binds only against a receiver that is *not* a forbidden reflective type, using **`Public | Instance`** binding flags (no `Static`) — which additionally aligns the engine with its own documented contract "static members are never exposed" (#2946 §11.2), currently violated by `MethodResolver`'s default `GetMethods()`.

---

## 10. The structural boundary vs. the defence-in-depth backstop — layering stated explicitly

There are **two layers**, and their relationship is deliberate:

### Layer 1 (primary, structural, complete for the known attack): refuse reflective receivers *by construction*
At every site where the engine reflects over a host object's **runtime** type to dispatch, call `TypeGuard.IsForbiddenReflectiveReceiver(host.GetType())` and throw a contained `ScriptRuntimeException` when true — **before** any binding. Sites (both engines):

| Site | File:line |
|---|---|
| method resolve (interp) | `Parser/Resolvers/MethodResolver.cs:52` |
| member read/write (interp) | `Tokens/ScriptMember.cs:60,71,135,139` |
| indexer read/write (interp) | `Tokens/ScriptIndexer.cs:50,92` |
| method build (compiled) | `Expressions/ExpressionBuilder.cs:536` |
| member build (compiled) | `Expressions/ExpressionBuilder.cs:265-271` |
| indexer build (compiled) | `Expressions/ExpressionBuilder.cs:258-262` |

**Deny-family** (structural predicate — assignability, not name matching):
- `System.Type` (and thus `RuntimeType`),
- `System.Reflection.*` — `MemberInfo`, `MethodBase`, `MethodInfo`, `ConstructorInfo`, `FieldInfo`, `PropertyInfo`, `EventInfo`, `ParameterInfo`, `Assembly`, `Module`, `MemberInfo`-derivatives,
- `System.Activator`, `System.AppDomain`, `System.Runtime.*` (e.g. `RuntimeTypeHandle`, `RuntimeMethodHandle`).

Because `Type.InvokeMember`, `Type.GetMethod`, `Type.Assembly`, `Activator.CreateInstance`, etc. **require** a receiver in this family, refusing the receiver kills the entire #7787 chain at its first reflective hop — *this layer alone is sufficient for the confirmed attack.* `System.Delegate` is **not** in the deny-family (that would break `.invoke()` on script lambdas/imports); a delegate's dangerous members (`Method` returns a `MethodInfo`, `DynamicInvoke`) are caught at the *next* hop — the returned `MethodInfo` is a deny-family receiver, so any dispatch on it is refused.

Additionally, dropping `Static` from resolution binding flags (`MethodResolver.cs:52` → `Public | Instance`, matching the compiled path at line 536) means even a deny-family *classification miss* cannot reach a static reflection entry point (`Type.GetType`, `Assembly.Load`, `Activator.CreateInstance` are static).

### Layer 2 (secondary, defence-in-depth, conservative): reflection-specific method-name backstop
After Layer 1, at the method-resolve sites, additionally refuse a small set of **reflection-specific** method names regardless of receiver: `invokemember`, `getmethod`, `getmethods`, `getconstructor`, `getconstructors`, `getmember`, `getmembers`, `getfield`, `getfields`, `getproperty`, `getproperties`, `makegenerictype`, `makearraytype`, `dynamicinvoke`, `createinstance`, `getinterface`, `getinterfaces`, `getnestedtype`, `getruntimemethod`.

- **`invoke` is deliberately NOT in the list** — it is legitimate (`IExternalMethod.Invoke` at `ScriptMethod.cs:88`, `$m.invoke(args)` on imports/delegates).
- **`gettype` is NOT in the list** — it is intercepted to a `ScriptType` (§7 Door 2), not denied.

**Why Layer 2 earns its keep (not YAGNI):** Layer 1 is complete for the *known* attack, but it depends on the deny-family predicate correctly classifying every reflective type. Layer 2 is a cheap (one lowered-string set lookup) catch for a **classification miss** — a reflective type the predicate fails to recognise (a future runtime type, a platform-specific `MemberInfo` subtype). It is mandated by the security remediation (#7788 option 3) and the red-team (#7789). This is a *named* need (predicate-miss insurance), not speculative flexibility. It is explicitly framed as *beneath* the structural boundary: if Layer 1 and Layer 2 ever disagree, Layer 1's refusal is the real guarantee.

### B4 — the `cast` / `DetermineType` closure (`Extensions/TypeExtensions.cs:21-63`)
`DetermineType` today, on a miss against registered types, falls through to `Type.GetType(name)` + a scan of `AppDomain.CurrentDomain.GetAssemblies()` + `Assembly.Load` of every referenced assembly — turning any script string into an arbitrary `System.Type` and running `Assembly.Load` as a side effect.
**Answer:** resolve script-supplied type names against **only** (a) the registered `Types` set (`provider.GetType(name)?.ProvidedType`) and (b) the built-in primitive whitelist (`Parser/ScriptParser.cs:64-77` / the `StandardTypes` in `ExpressionBuilder.cs:26-40`). Keep the `[]`/array-suffix handling. On a miss, throw `ScriptParserException`/`ScriptRuntimeException`. **Remove entirely** the `Type.GetType` + `AppDomain` scan + `Assembly.Load` fallthrough (`TypeExtensions.cs:38-53`). This also resolves robustness bug #2910.

Also **remove** the latent `string → Type` converter at `Extern/Converter.cs:29` (`o => Type.GetType((string)o)`) — a host method taking a `System.Type` parameter would otherwise let a script pass any string and get an arbitrary `Type`. Its legitimate replacement is the controlled `ScriptType → Type` converter (K7): pass a type *handle*, not a string.

---

## 11. Cross-Cutting Concerns

- **Error handling.** Every refusal throws a contained `ScriptRuntimeException` carrying the offending token's source position (the existing `ScriptMethod`/`ScriptMember`/`ScriptIndexer` catch-wrap patterns already do this). No refusal escapes as a raw .NET exception; none crashes the host.
- **Security invariant (the load-bearing one).** *No script-reachable operation returns a live `System.Type` or `System.Reflection.*` object as a callable/reflective value.* Every future code path that evaluates a type expression or reflects over a host type must either produce a `ScriptType` or pass through `TypeGuard`. State this as a comment on `ScriptType` and `TypeGuard` (per #114 §4) so the next contributor cannot silently reopen the door. A regression test battery (§15) is the executable form of the invariant.
- **Caching.** `MethodResolver` caches by `MethodCacheKey(host.GetType(), …)`. The `TypeGuard` receiver check must run **before** the cache lookup (a denied receiver must never be cached as resolvable). The `getType` interception (§7) runs before resolution entirely, so it is never cached.
- **Performance.** One O(1) predicate call per dispatch (a `HashSet`/assignability check) and one allocation per `typeof`/`getType`/bare-type evaluation (`ScriptType` wrapper). Negligible; type evaluation is rare on hot paths.
- **Concurrency / idempotency / consistency.** Unchanged — the design touches only synchronous token evaluation and resolution; no shared mutable state is introduced (`ScriptType` is immutable; `TypeGuard`'s sets are static readonly).

---

## 12. Backward Compatibility — what breaks for a legitimate host, and which tests change

This is a behavioural change to a public surface. Enumerated:

**Stays green (verified against the suite):**
- `TypeTokenTests.ParseType/ParseTypeArray/…` — `parser.Parse("int").Execute<Type>()` still returns `typeof(int)` via the K6 host-return unwrap.
- `TypeCastTests.Integer/Decimal/String` — `int("722")` etc. unchanged (K4).
- `ParameterTests` bare-type (`parameter($input, int)`, line 28) and registered-string (`parameter($input, "int")`, line 34; `"ont"` still throws, line 40; `"int[]"`, lines 109/119) — all resolve against registered/primitive types (K1, B4).
- `m<int>()` generics, `new datetime(...)`, `typeof($x) == int` — all preserved (K2, K3, §8/§6 equality).
- `ExtractTokenTests` `$Parcel.gettype()` (line 70) — parse/extract only; `getType` still parses to a `ScriptMethod`. Green.

**Breaks — intended, must be called out:**
1. **`ParameterTests.FreeTypeParameter` (line 70-72)** — `parameter($collection, "Pooshit.Scripting.Data.Variable,Pooshit.Scripting[]")` relies on the removed assembly-qualified `DetermineType` door. **This test must change:** either register the type on the parser under test (`parser.Types.AddType<Variable>("variable")`) and reference it by registered name, or assert that the assembly-qualified string now throws. The assembly-qualified `cast`/`parameter` capability documented in #2946 §8 ("supports … `Namespace.Type,Assembly`") is **removed** — this is the intended closure of door B4. **John: run the full `Scripting.Tests` suite; this is the one legitimate-type test expected to fail, and its change is the deliberate security trade-off.**
2. **Reading non-identity members of a `typeof(...)` result** — any script doing `typeof($x).IsArray`, `.BaseType`, `.GetMethods()`, `.Assembly`, etc. now fails (those members do not exist on `ScriptType`). This is the security change itself. Scripts needing such data must be given a narrow host facade. Likely rare; no test in the suite exercises it.
3. **A host method taking `System.Type` and relying on a script passing a *string*** — now fails (the `string→Type` converter is removed). The script must pass a type handle (`int`, `typeof($x)`), which unwraps via K7. Equivalent capability, different call shape. No test exercises the string form.
4. **Compiled `ParseDelegate<Func<…, Type>>` returning a bare type** — the compiled type-token constant is now a `ScriptType`; the K6/K7 `ScriptType→Type` converter covers the return marshalling when the delegate's declared type is `System.Type`. **Open question O-3** flags the residual edge for the operator; the compiled path is documented "not recommended for untrusted scripts" (#2946 §13).

No other existing test is expected to change. `MethodCallTests`, `ConstructorTests`, `DictionaryTests`, `ImportTests`, etc. exercise normal host-object dispatch, which the boundary leaves untouched (host objects are not in the deny-family).

---

## 13. Quality Attributes & Trade-offs

- **Security (primary).** The confirmed RCE is closed at two independent layers (structural receiver refusal + name backstop) plus source-level handle wrapping plus the `Static` binding-flag removal plus the `DetermineType`/converter closure. Any single layer surviving still denies the chain. Trade-off: the assembly-qualified `cast`/`parameter` feature is removed (§12.1) — accepted, because it is the door and the operator mandated its closure.
- **Simplicity / maintainability.** One new immutable class, one new stateless helper, edits at an enumerated set of ~15 call sites. No new abstraction layer, no policy engine, no config surface (§6 KISS math). The deny-family and name-backstop live in exactly one place (DRY).
- **Rejected alternative — name-blocklist only (no structural boundary).** Rejected: a pure method-name blocklist (`InvokeMember`, `GetMethods`, …) is bypassable (reflection has many entry points; casing/aliasing; `MemberInfo` traversal) and is exactly the kind of enumerate-the-bad approach that missed this bug for years. The structural receiver refusal is *by construction* and does not depend on knowing every dangerous name. The blocklist is kept only as Layer 2 defence-in-depth (§10).
- **Rejected alternative — deny `getType`/`typeof` entirely.** Rejected: needlessly breaks legitimate `$x.getType().name` / `typeof($x) == int` usage. Returning an inert `ScriptType` is equally safe and backward-compatible.
- **Rejected alternative — per-parser allowed-reflection policy.** Rejected on KISS/YAGNI (§6): no named host need, adds a config surface. The opaque handle + fixed sink list is sufficient.

---

## 14. Correction to reference #2946 §12 (must ship as a doc update)

The current §12 text — *"a fresh parser … can do literals/operators/casts … and **nothing else**: no reflection, no `Type.GetType`, no file/network/process, no static members"* — is **false today** (every one is reachable per #7787) and becomes **true only after this fix, and only as defence-in-depth**. The correction (John or the operator applies it to node #2946 and to `docs/pooscript-language-reference.md` §12):

> The default sandbox restricts by non-exposure **and** by a structural refusal of the reflection type-family: script can never obtain a live `System.Type`/`System.Reflection.*` as a callable surface (`typeof`/`getType()` yield an opaque, inert `ScriptType`; method/member/indexer resolution refuses reflective receivers; `cast`/`parameter` resolve names against registered types + primitives only, never `Type.GetType`/`Assembly.Load`). **This is defence-in-depth, not a security boundary.** For genuinely untrusted (hostile-author) scripts, process/container isolation remains mandatory — the in-process engine is not a substitute for it.

---

## 15. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| A reflective type not caught by the deny-family predicate (classification miss) | Layer 2 name backstop + the `Static` binding-flag removal both independently block the static reflection entry points; regression tests assert the #7787 chains throw. |
| A future new token/engine path materialises a raw `Type` | The security invariant (§11) is documented on `ScriptType`/`TypeGuard`; the regression battery (below) fails if the chain reopens. |
| `TypeInformation.ConvertOperands` mangles a `ScriptType` operand before `==` | Two same-typed `ScriptType` operands need no conversion; John verifies `ConvertOperands` leaves them untouched and adds a `typeof($x)==int` test. |
| The `getType` interception missed on one engine | The structural boundary is the belt: even an un-intercepted raw `Type` is inert as a dispatch/member/indexer receiver. |
| Compiled-path `ParseDelegate` returning `Type` | Covered by K6/K7 for `System.Type` targets; flagged as O-3; compiled path is not for untrusted scripts. |

**Regression battery (acceptance, per #7788):** port the #7789 red-team chains into `Scripting.Tests` — each of `(1).getType()...InvokeMember(...)`, `typeof(typeof(1))...`, `File.ReadAllText`/`WriteAllText`, `Environment.*`, `Process.Start`, and the maximally-restricted single-expression variant must throw a contained `ScriptRuntimeException` on a default parser and stay contained on a widened realistic host. Plus positive tests: `typeof($x)==int`, `typeof($x).name`, `m<int>()`, `new datetime(...)`, `int("722")`, `parameter($v,int)`, `parameter($v,"int")`, `Execute<Type>()` of `"int"`.

---

## 16. Pre-Design Checklist (#1136 §5) — answered in order

**KISS / DRY / YAGNI**
- *No new type mirroring an existing one:* `ScriptType` has no existing equivalent (the original wrapped type is gone from the code). Not a mirror.
- *No abstraction with one implementation and no second planned:* `TypeGuard` is a concrete helper, not an interface. `ScriptType` is `sealed`. No speculative interface.
- *No element justified by "might need later":* the per-parser policy object and `Namespace`/`is`/`as` surface were **cut** for exactly this reason (§6).
- *No deprecation/flag/shim/transition window:* none — the change is behavioural and atomic; the two intended breaks (§12) change tests directly.
- *`block_size × site_count` for inline-vs-extract:* the deny-family predicate and name-backstop are used at ~15 sites → extracted into `TypeGuard` (a nameable 1-3-word helper), not inlined. Math: a ~5-line predicate × 8 dispatch sites = ~40 duplicated lines if inlined → extraction wins per #1267.

**Existing systems first**
- *Existing surface audited:* the sinks (`TypeInstanceProvider`, `ImpliciteTypeCast`, `ScriptParameter`) already own type consumption; the design reuses them and adds no new layer. `ScriptType` is the one genuinely new type — justified by a concrete security boundary (a different *security* boundary, per #1136 §2's "genuinely different security boundary" bar), not cleanliness.
- *New persisted data:* none.
- *Reader-chain recursion:* N/A (no data model change).

**Configurability**
- *Every knob has a named operator/environment:* **no new knob is added.** The deny-family and name-backstop are `const`/`static readonly` sets in code, named clearly (magic stays magic, #1136 §3).

**Less is better**
- *Delete/merge/inline check:* the policy object was deleted; `Namespace` merged away into `FullName`; the predicate inlined-vs-extracted decision resolved to extract (above).
- *Trade-offs named explicitly:* §12 (assembly-qualified feature removed), §13 (rejected alternatives).
- *Radical-clean vs compromise:* `DetermineType` is closed to registered+primitives outright (radical-clean), not a half-open assembly allow-list (a compromise shape with no named consumer) — per #1136 §4.
- *Reader-inventory covers AST + string-literal refs:* §8 enumeration walked both token AST sites and string-name resolution (`DetermineType`, `supportedcasts`, `StandardTypes`).

**Data deliverables:** N/A (no SQL/migration).

**Document discipline**
- Cites #114 and #1136 as load-bearing (header). Scope in/out explicit (§2). Both quotes present (§0). No superseded predecessor doc (this is the first type-access-boundary design). No multi-paragraph "why keep X" filler.

---

## 17. Implementation Guidance for the Next Agent (john-backend-dev) — ordered build phases

Still no code — architectural units in dependency order:

1. **`ScriptType` (source of truth for the handle).** New immutable `sealed` class in `Data/`. Internal `Of(Type)` factory (null→null), `internal Unwrap()`, public `Name`/`FullName`/`ToString()`/`Equals`/`GetHashCode`/`==`/`!=`. Document the security invariant (§11) on it. Unit-test equality and inertness.
2. **`TypeGuard` (the boundary predicate).** New static helper: `IsForbiddenReflectiveReceiver(Type)` (deny-family, §10) and `IsForbiddenReflectiveMethodName(string)` (name backstop, §10). Static readonly sets. Unit-test both.
3. **Sources → handle.** `TypeOfToken.cs:25` (S1); `TypeToken.cs:29` + `ExpressionBuilder.cs:349` (S3); `getType` interception in `ScriptMethod.cs:83` and `ExpressionBuilder.cs:531` (S2/Door 2).
4. **Structural boundary (Layer 1).** Insert the `TypeGuard.IsForbiddenReflectiveReceiver` check — *before* cache and binding — at all six sites in §10; change `MethodResolver.cs:52` `GetMethods()` → `GetMethods(Public | Instance)`.
5. **Name backstop (Layer 2).** Apply `IsForbiddenReflectiveMethodName` at the method-resolve sites (interp + compiled), after Layer 1.
6. **Sinks unwrap.** `ScriptParameter.cs:56` read `Type.Type` (K1); `ScriptMethod.CreateGenericParameters` unwrap `ScriptType` (K2); `Converter` add `ScriptType→Type`, remove `string→Type` (K6/K7 + B4-converter); `Script.ConvertResult` relies on the converter (K6).
7. **`DetermineType` closure (B4).** Rewrite `TypeExtensions.cs:21-63` to registered-types + primitive whitelist only; remove the `Type.GetType`/`AppDomain`/`Assembly.Load` fallthrough.
8. **Regression + positive tests (§15).** Port the #7789 red-team battery; add the positive-path assertions; update `ParameterTests.FreeTypeParameter` (§12.1). Run the full suite; the only expected pre-existing failure is `FreeTypeParameter` (fix it as described).
9. **Doc correction (§14).** Update `docs/pooscript-language-reference.md` §12 and note the #2946 §12 correction for the operator.

Suggested PR decomposition (one feature per PR, per the PR-scope discipline): this is **one** feature — the type-access boundary — and ships as **one PR**; steps 1-9 are internal ordering, not separable shippable units (the boundary is not safe until all sources, sinks, and both engines are closed together).

---

## 18. Open Questions (batched for the operator/user)

- **O-1.** `getType()`/`typeof` decision is made in this design as **allow, return `ScriptType`** (not deny). Confirm you are happy keeping them available (backward-compatible for `$x.getType().name`), versus denying them outright for an even tighter surface.
- **O-2.** `ScriptType` exposes `Name` + `FullName` + equality. Confirm this is the right minimum, or name any other capability-free member community scripts read on a type (e.g. `Namespace`), so it is added deliberately rather than discovered as a break.
- **O-3.** Compiled path (`ParseDelegate`): a delegate declared to return `System.Type` will now return via a `ScriptType→Type` unwrap. Confirm any real host uses `ParseDelegate` with a `Type` return/parameter; if none, the residual edge is moot (compiled path is "not for untrusted scripts", #2946 §13).
- **O-4.** The assembly-qualified `cast`/`parameter` capability (`"Namespace.Type,Assembly"`, #2946 §8) is **removed** (§12.1). Confirm no production host relies on it; if one does, the mitigation is `parser.Types.AddType<T>()` for the specific types it needs — an explicit, auditable allow-list rather than an open assembly scan.
