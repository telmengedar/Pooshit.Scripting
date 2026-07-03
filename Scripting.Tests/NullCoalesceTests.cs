using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Formatters;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Tokens;
using Pooshit.Scripting.Visitors;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class NullCoalesceTests {
        readonly ScriptParser parser = new();

        class VariableCollector : ScriptVisitor {
            readonly List<string> names = [];
            public IReadOnlyList<string> Names => names;
            public override void VisitVariable(ScriptVariable variable) => names.Add(variable.Name);
        }

        [Test, Parallelizable]
        public void NullLhsReturnsRhs() {
            Assert.AreEqual(5, parser.Parse("null ?? 5").Execute());
        }

        [Test, Parallelizable]
        public void NonNullLhsReturnsLhs() {
            Assert.AreEqual(5, parser.Parse("5 ?? 6").Execute());
        }

        [Test, Parallelizable]
        public void ShortCircuitNonNullLhs() {
            Assert.AreEqual(5, parser.Parse("5 ?? 1/0").Execute());
        }

        [Test, Parallelizable]
        public void NullLhsEvaluatesRhs() {
            Assert.Throws<ScriptRuntimeException>(() => parser.Parse("null ?? 1/0").Execute());
        }

        [Test, Parallelizable]
        public void ChainingBothNull() {
            Assert.AreEqual(7, parser.Parse("null ?? null ?? 7").Execute());
        }

        [Test, Parallelizable]
        public void ChainingShortCircuitsOnFirstNonNull() {
            Assert.AreEqual(3, parser.Parse("null ?? 3 ?? 1/0").Execute());
        }

        [Test, Parallelizable]
        public void PrecedenceVsAssignment() {
            Assert.AreEqual(9, parser.Parse("$x = null ?? 9; $x").Execute());
        }

        [Test, Parallelizable]
        public void PrecedenceVsArithmetic() {
            Assert.AreEqual(5, parser.Parse("null ?? 2 + 3").Execute());
        }

        [Test, Parallelizable]
        public void InteractionWithTernary() {
            Assert.AreEqual(10, parser.Parse("true ?? false ? 10 : 20").Execute());
        }

        [Test, Parallelizable]
        public void FormatterRoundTripSingle() {
            IScript script = parser.Parse("$a ?? $b");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a ?? $b", formatted);
        }

        [Test, Parallelizable]
        public void FormatterRoundTripChained() {
            IScript script = parser.Parse("$a ?? $b ?? $c");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a ?? $b ?? $c", formatted);
        }

        [Test, Parallelizable]
        public void TraversalVisitsBothOperands() {
            IScript script = parser.Parse("$a ?? $b");
            VariableCollector collector = new();
            collector.Visit(script);
            CollectionAssert.Contains(collector.Names, "a");
            CollectionAssert.Contains(collector.Names, "b");
        }

        [Test, Parallelizable]
        public void CompiledPathNullLhs() {
            ScriptParser compiledParser = new();
            Func<object, object, object> function = compiledParser.ParseDelegate<Func<object, object, object>>(
                "a ?? b",
                new LambdaParameter<object>("a"),
                new LambdaParameter<object>("b"));
            Assert.AreEqual(5, function(null, 5));
        }

        [Test, Parallelizable]
        public void CompiledPathNonNullLhs() {
            ScriptParser compiledParser = new();
            Func<object, object, object> function = compiledParser.ParseDelegate<Func<object, object, object>>(
                "a ?? b",
                new LambdaParameter<object>("a"),
                new LambdaParameter<object>("b"));
            Assert.AreEqual(3, function(3, 5));
        }

        [Test, Parallelizable]
        public void ParserRejectsNullCoalesceWithoutLeftOperand() {
            ScriptParserException ex = Assert.Throws<ScriptParserException>(() => parser.Parse("?? 1"));
            StringAssert.Contains("Left operand expected", ex.Message);
        }
    }
}
