# `ParseParameters`' safety rests on an unstated coupling with `ParseCore`'s switch

**Files:** `Pooshit.Scripts/Parser/ScriptParser.cs` — `ParseParameters` (indexer and control-statement argument lists, terminators `)` and `]`) and `ParseCore` (the recursive-descent switch it calls into via `Parse`).
**Origin:** DiVoid #9350 (the dictionary-parse EOF hang) and its sibling fixes — `ParseTokenList`'s progress guard (`ScriptParser.cs:989-994`) and `ParseDictionaryKey`'s progress guard (`ScriptParser.cs:1628-1643`). QA re-review #9397 asked for this note when it upheld the claim below as "structurally unreachable today" rather than as a permanent guarantee.

## The invariant

Unlike `ParseTokenList` and `ParseDictionaryKey`, `ParseParameters` does **not** guard its inner `Parse` call against a no-progress return. That omission is safe only because of a fact about `ParseCore` that is not stated anywhere in code:

> Every character `ParseCore` can return `null` on without advancing `index` — `','` and `']'` (`ScriptParser.cs:1456-1458`), and `':'` under `suppressformat == true` (`:1360-1364`) — is already consumed by `ParseParameters`'s own switch (`:1013-1019`) before it reaches the unguarded `Parse` call, or throws on the path `ParseParameters` actually uses (`suppressformat: false`, where an empty-token `':'` throws `"Formatter needs a token to format"` instead of returning null).

`ParseParameters`' switch must remain a **superset** of `ParseCore`'s no-advance `done` cases. Nothing enforces this; it is true today by inspection only.

## What breaks it

Adding a new `case 'X': done = true;` to `ParseCore`'s switch (`:1452` onward) for a character `ParseParameters` does not itself special-case, or removing `ParseCore`'s `index == starttoken && !done` backstop (`ScriptParser.cs:1523-1524`), reopens the same unkillable hang `ParseTokenList` and `ParseDictionary` had before this fix — reachable only through indexer arguments (`$a[...]`) or control-statement arguments (`if(...)`), which the dictionary and token-list tests do not exercise. Nothing in the suite would go red.

## Why there is no guard here today

Design Contracts §6 (defensive code for impossible scenarios): the no-progress path is provably unreachable given the current switch, so adding a guard now would be defensive code for a scenario that cannot happen. Confirmed by reading and empirically (12,600 context-prefixed inputs plus a full 1–3 character brute force, zero hangs) — see QA re-review #9397's adjudication.

## What the next person touching `ParseCore`'s switch needs to do

Before adding a no-progress case to `ParseCore`, check whether the new character reaches `ParseParameters` unconsumed. If it does, `ParseParameters` needs the same progress guard `ParseTokenList` and `ParseDictionaryKey` already carry: capture `index` before the `Parse` call, throw when it has not moved.
