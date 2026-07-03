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
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TernaryTests {
        readonly ScriptParser parser = new();

        class VariableCollector : ScriptVisitor {
            readonly List<string> names = [];
            public IReadOnlyList<string> Names => names;
            public override void VisitVariable(ScriptVariable variable) => names.Add(variable.Name);
        }

        [Test, Parallelizable]
        public void TernaryBasicTrue() {
            Assert.AreEqual(1, parser.Parse("true ? 1 : 2").Execute());
        }

        [Test, Parallelizable]
        public void TernaryBasicFalse() {
            Assert.AreEqual(2, parser.Parse("false ? 1 : 2").Execute());
        }

        [Test, Parallelizable]
        public void TernaryWithComparisonCondition() {
            Assert.AreEqual("yes", parser.Parse("3>2 ? \"yes\" : \"no\"").Execute());
        }

        [Test, Parallelizable]
        public void TernaryPrecedenceVsAssignment() {
            Assert.AreEqual(10, parser.Parse("$x = 3>2 ? 10 : 20; $x").Execute());
        }

        [Test, Parallelizable]
        public void TernaryRightAssociative() {
            Assert.AreEqual(3, parser.Parse("false ? 1 : false ? 2 : 3").Execute());
        }

        [Test, Parallelizable]
        public void TernaryNestedInTrueBranch() {
            Assert.AreEqual(2, parser.Parse("true ? false ? 1 : 2 : 3").Execute());
        }

        [Test, Parallelizable]
        public void TernaryShortCircuitTrueBranch() {
            Assert.AreEqual(1, parser.Parse("true ? 1 : 1/0").Execute());
        }

        [Test, Parallelizable]
        public void TernaryShortCircuitFalseBranch() {
            Assert.AreEqual(2, parser.Parse("false ? 1/0 : 2").Execute());
        }

        [Test, Parallelizable]
        public void TernaryAsDictionaryValue() {
            Assert.AreEqual(1, parser.Parse("{\"k\": true ? 1 : 2}[\"k\"]").Execute());
        }

        [Test, Parallelizable]
        public void TernaryWithDictionaryBranch() {
            Assert.AreEqual(1, parser.Parse("(true ? {\"a\":1} : {\"b\":2})[\"a\"]").Execute());
        }

        [Test, Parallelizable]
        public void PlainDictionaryRegressionAfterTernary() {
            Assert.AreEqual(1, parser.Parse("{\"a\":1}[\"a\"]").Execute());
        }

        [Test, Parallelizable]
        public void TernaryInArrayPosition() {
            Assert.AreEqual(2, parser.Parse("[1, true ? 2 : 3, 4][1]").Execute());
        }

        [Test, Parallelizable]
        public void TernaryAsMethodArgument() {
            TestHost host = new();
            IScript script = parser.Parse("$host.integer(true ? 1 : 2)");
            Assert.AreEqual(1, script.Execute(new VariableProvider(new Variable("host", host))));
        }

        [Test, Parallelizable]
        public void TernaryMissingConditionThrows() {
            ScriptParserException ex = Assert.Throws<ScriptParserException>(() => parser.Parse("? 1 : 2"));
            StringAssert.Contains("Condition expected", ex.Message);
        }

        [Test, Parallelizable]
        public void TernaryMissingColonThrows() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("true ? 1"));
        }

        [Test, Parallelizable]
        public void TernaryFormatterRoundTrip() {
            IScript script = parser.Parse("$a ? $b : $c");
            string formatted = new ScriptFormatter().FormatScript(script);
            Assert.AreEqual("$a ? $b : $c", formatted);
        }

        [Test, Parallelizable]
        public void TernaryCompiledDelegate() {
            ScriptParser compiledParser = new();
            Func<bool, int, int, int> function = compiledParser.ParseDelegate<Func<bool, int, int, int>>(
                "c ? t : f",
                new LambdaParameter<bool>("c"),
                new LambdaParameter<int>("t"),
                new LambdaParameter<int>("f"));
            Assert.AreEqual(10, function(true, 10, 20));
            Assert.AreEqual(20, function(false, 10, 20));
        }

        [Test, Parallelizable]
        public void TernaryVisitorRecursesIntoAllBranches() {
            IScript script = parser.Parse("$c ? $t : $f");
            VariableCollector collector = new();
            collector.Visit(script);
            CollectionAssert.Contains(collector.Names, "c");
            CollectionAssert.Contains(collector.Names, "t");
            CollectionAssert.Contains(collector.Names, "f");
        }

        [Test, Parallelizable]
        public void TernaryCompiledDelegateMismatchedBranchTypes() {
            ScriptParser compiledParser = new();
            Func<bool, object> function = compiledParser.ParseDelegate<Func<bool, object>>(
                "c ? 1 : \"x\"",
                new LambdaParameter<bool>("c"));
            Assert.AreEqual(1, function(true));
            Assert.AreEqual("x", function(false));
        }

        [Test, Parallelizable]
        public void TernaryOperatorAssignGraftTrueCondition() {
            Assert.AreEqual(15, parser.Parse("$x=5; $x += 3>2 ? 10 : 20; $x").Execute());
        }

        [Test, Parallelizable]
        public void TernaryOperatorAssignGraftFalseCondition() {
            Assert.AreEqual(25, parser.Parse("$x=5; $x += 1>2 ? 10 : 20; $x").Execute());
        }
    }
}
