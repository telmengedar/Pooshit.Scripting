using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Formatters;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Tokens;
using Pooshit.Scripting.Visitors;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class NullSafeMemberTests {
        readonly ScriptParser parser = new();

        class Outer {
            public Inner Inner { get; set; }
            public int Value { get; set; }
        }

        class Inner {
            public string Name { get; set; }
        }

        class CountingReceiver {
            int count;
            public int Count => count;
            public CountingReceiver Next() { count++; return this; }
            public string Tag { get; set; }
        }

        class VariableCollector : ScriptVisitor {
            readonly List<string> names = [];
            public IReadOnlyList<string> Names => names;
            public override void VisitVariable(ScriptVariable variable) => names.Add(variable.Name);
        }

        IVariableProvider Vars(string name, object value) =>
            new VariableProvider(new Variable(name, value));

        [Test, Parallelizable]
        public void DiscriminatorANullReturnsNull() {
            Assert.IsNull(parser.Parse("$a?.Inner.Name").Execute(Vars("a", null)));
        }

        [Test, Parallelizable]
        public void DiscriminatorANonNullBNullThrows() {
            Outer a = new() { Inner = null };
            Assert.Throws<ScriptRuntimeException>(() =>
                parser.Parse("$a?.Inner.Name").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void DiscriminatorAllNonNullReturnsValue() {
            Outer a = new() { Inner = new() { Name = "hello" } };
            Assert.AreEqual("hello", parser.Parse("$a?.Inner.Name").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void ChainedNullCondANullReturnsNull() {
            Assert.IsNull(parser.Parse("$a?.Inner?.Name").Execute(Vars("a", null)));
        }

        [Test, Parallelizable]
        public void ChainedNullCondBNullReturnsNull() {
            Outer a = new() { Inner = null };
            Assert.IsNull(parser.Parse("$a?.Inner?.Name").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void ChainedNullCondAllNonNullReturnsValue() {
            Outer a = new() { Inner = new() { Name = "hi" } };
            Assert.AreEqual("hi", parser.Parse("$a?.Inner?.Name").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void SimpleMemberNonNullReceiver() {
            Outer a = new() { Value = 7 };
            Assert.AreEqual(7, parser.Parse("$a?.Value").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void SimpleMemberNullReceiver() {
            Assert.IsNull(parser.Parse("$a?.Value").Execute(Vars("a", null)));
        }

        [Test, Parallelizable]
        public void ShortCircuitNullReceiverSkipsContinuation() {
            Assert.IsNull(parser.Parse("$a?.Value").Execute(Vars("a", null)));
        }

        [Test, Parallelizable]
        public void ReceiverEvaluatedOnce() {
            CountingReceiver rcv = new() { Tag = "ok" };
            IVariableProvider vars = new VariableProvider(new Variable("rcv", rcv));
            object result = parser.Parse("$rcv.Next()?.Tag").Execute(vars);
            Assert.AreEqual("ok", result);
            Assert.AreEqual(1, rcv.Count);
        }

        [Test, Parallelizable]
        public void InteractionWithNullCoalesceNullReceiver() {
            Assert.AreEqual("fallback", parser.Parse("$a?.Value ?? \"fallback\"").Execute(Vars("a", null)));
        }

        [Test, Parallelizable]
        public void InteractionWithNullCoalesceNonNullReceiver() {
            Outer a = new() { Value = 3 };
            Assert.AreEqual(3, parser.Parse("$a?.Value ?? 99").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void InteractionWithTernary() {
            Outer a = new() { Value = 5 };
            Assert.AreEqual("pos", parser.Parse("$a?.Value > 0 ? \"pos\" : \"neg\"").Execute(Vars("a", a)));
        }

        [Test, Parallelizable]
        public void DisambiguationTriple() {
            Outer a = new() { Value = 1 };
            IVariableProvider vars = Vars("a", a);
            Assert.AreEqual(1, parser.Parse("$a?.Value").Execute(vars));
            Assert.AreEqual(1, parser.Parse("$a.Value ?? 99").Execute(vars));
            Assert.AreEqual("pos", parser.Parse("$a.Value > 0 ? \"pos\" : \"neg\"").Execute(vars));
        }

        [Test, Parallelizable]
        public void FormatterRoundTripSimple() {
            IScript script = parser.Parse("$a?.Value");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a?.Value", formatted);
        }

        [Test, Parallelizable]
        public void FormatterRoundTripChained() {
            IScript script = parser.Parse("$a?.Inner.Name");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a?.Inner.Name", formatted);
        }

        [Test, Parallelizable]
        public void FormatterRoundTripDoubleConditional() {
            IScript script = parser.Parse("$a?.Inner?.Name");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a?.Inner?.Name", formatted);
        }

        [Test, Parallelizable]
        public void FormatterRoundTripMemberThenConditional() {
            IScript script = parser.Parse("$a.Inner?.Name");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a.Inner?.Name", formatted);
        }

        [Test, Parallelizable]
        public void CompiledPathNullReceiver() {
            Func<Outer, object> f = parser.ParseDelegate<Func<Outer, object>>(
                "a?.Value",
                new LambdaParameter<Outer>("a"));
            Assert.IsNull(f(null));
        }

        [Test, Parallelizable]
        public void CompiledPathNonNullReceiver() {
            Func<Outer, object> f = parser.ParseDelegate<Func<Outer, object>>(
                "a?.Value",
                new LambdaParameter<Outer>("a"));
            Assert.AreEqual(42, f(new Outer { Value = 42 }));
        }

        [Test, Parallelizable]
        public void CompiledPathValueTypeMemberBoxedToObject() {
            Func<Outer, object> f = parser.ParseDelegate<Func<Outer, object>>(
                "a?.Value",
                new LambdaParameter<Outer>("a"));
            Assert.AreEqual(99, f(new Outer { Value = 99 }));
            Assert.IsNull(f(null));
        }

        [Test, Parallelizable]
        public void CompiledPathDiscriminatorThrowsWhenInnerNull() {
            Func<Outer, object> f = parser.ParseDelegate<Func<Outer, object>>(
                "a?.Inner.Name",
                new LambdaParameter<Outer>("a"));
            Assert.IsNull(f(null));
            Assert.Throws<NullReferenceException>(() => f(new Outer { Inner = null }));
        }

        [Test, Parallelizable]
        public void TraversalVisitsReceiverNotPlaceholder() {
            IScript script = parser.Parse("$a?.Inner");
            VariableCollector collector = new();
            collector.Visit(script);
            CollectionAssert.Contains(collector.Names, "a");
            CollectionAssert.DoesNotContain(collector.Names, "?receiver");
        }

        [Test, Parallelizable]
        public void ParserRejectsLeadingNullConditional() {
            ScriptParserException ex = Assert.Throws<ScriptParserException>(() => parser.Parse("?.Value"));
            StringAssert.Contains("Receiver expected", ex.Message);
        }

        [Test, Parallelizable]
        public void ParserRejectsAssignmentToNullConditional() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("$a?.Value = 5"));
        }
    }
}
