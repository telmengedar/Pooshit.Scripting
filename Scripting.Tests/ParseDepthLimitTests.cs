using System.Text;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises <see cref="ScriptLimits.MaxParseDepth"/> (DiVoid #7869): a parse-time recursion ceiling
    /// distinct from <see cref="ScriptLimits.MaxDepth"/>, which bounds runtime call depth and is far too
    /// tight for expression nesting. Every deep-nesting payload here is sized well under the measured
    /// native-stack-overflow boundary (~600-1300 frames depending on build configuration and nesting form),
    /// so a regression that removes the guard fails the assertion rather than crashing the test process
    /// </summary>
    [TestFixture, Parallelizable]
    public class ParseDepthLimitTests {

        static string NestedParens(int depth) => new string('(', depth) + "1" + new string(')', depth);

        static string NestedBraces(int depth) => new string('{', depth) + new string('}', depth);

        static string NestedCalls(int depth) {
            StringBuilder open = new();
            StringBuilder close = new();
            for (int i = 0; i < depth; ++i) {
                open.Append("$f(");
                close.Append(')');
            }

            return open.Append('1').Append(close).ToString();
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void Default_MaxParseDepthIsOneHundred() {
            Assert.That(ScriptLimits.DefaultMaxParseDepth, Is.EqualTo(100));
            Assert.That(ScriptLimits.Default.MaxParseDepth, Is.EqualTo(100));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7869: parenthesized nesting past the configured ceiling raises a catchable ScriptParserException at parse time instead of overflowing the native stack.")]
        public void ParenNesting_PastConfiguredLimitThrowsScriptParserException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxParseDepth = 20}
            };

            ScriptParserException exception = Assert.Throws<ScriptParserException>(() => parser.Parse(NestedParens(21)));
            Assert.That(exception.Message, Does.Contain("20"));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7869: the bug also reproduces through statement/dictionary-block nesting ('{'...'}'), a separate mutually-recursive path (Parse <-> ParseStatementBlock) from the paren form (Parse <-> ParseBlock).")]
        public void BraceNesting_PastConfiguredLimitThrowsScriptParserException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxParseDepth = 20}
            };

            Assert.Throws<ScriptParserException>(() => parser.Parse(NestedBraces(21)));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("DiVoid #7869: the bug also reproduces through nested method-call syntax ($f($f($f(...)))), a third recursive path (Parse -> ParseTokenList -> Parse) distinct from both parens and braces.")]
        public void NestedCallSyntax_PastConfiguredLimitThrowsScriptParserException() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxParseDepth = 20}
            };

            Assert.Throws<ScriptParserException>(() => parser.Parse(NestedCalls(21)));
        }

        [Test, Parallelizable, MaxTime(2000)]
        public void ParenNesting_UnderConfiguredLimitParsesAndExecutesNormally() {
            ScriptParser parser = new() {
                Limits = new ScriptLimits {MaxParseDepth = 20}
            };

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse(NestedParens(19)));
            Assert.That(script.Execute(), Is.EqualTo(1));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("A realistic nesting depth must still parse and execute under the actual production default (ScriptLimits.Default), not just a test-configured limit - the ceiling exists to stop pathological input, not ordinary expressions.")]
        public void ParenNesting_RealisticDepthParsesUnderProductionDefault() {
            ScriptParser parser = new();

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse(NestedParens(30)));
            Assert.That(script.Execute(), Is.EqualTo(1));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("DiVoid #7869 PoC shape, scaled to the production default rather than the original 3001-char/depth-1500 payload that took down the process: ScriptLimits.Default alone (no per-test override) refuses deep nesting with a typed exception, well short of the measured native-stack-overflow boundary of ~600-1300 frames.")]
        public void ParenNesting_PastProductionDefaultThrowsScriptParserException() {
            ScriptParser parser = new();

            Assert.Throws<ScriptParserException>(() => parser.Parse(NestedParens(ScriptLimits.DefaultMaxParseDepth + 1)));
        }

        [Test, Parallelizable, MaxTime(10000)]
        [Description("ScriptLimits.None must opt out of the parse-depth ceiling entirely, exactly like every other knob - depth is kept well above the default but comfortably below the measured native-stack-overflow boundary (~600 frames, the tightest configuration measured) so a regression here fails the assertion rather than crashing the test process.")]
        public void ParenNesting_WithNoneLimitsExceedsDefaultWithoutThrowing() {
            ScriptParser parser = new() {
                Limits = ScriptLimits.None
            };

            IScript script = null;
            Assert.DoesNotThrow(() => script = parser.Parse(NestedParens(300)));
            Assert.That(script.Execute(), Is.EqualTo(1));
        }
    }
}
