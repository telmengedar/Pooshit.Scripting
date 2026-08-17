using System;
using System.Linq;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extensions.Script;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    /// <summary>
    /// exercises the secure-by-default flip (docs/architecture/execution-guards-depth-memory.md §18):
    /// <see cref="ScriptLimits.Default"/> as the new <see cref="ScriptParser.Limits"/> initial value, and
    /// <see cref="ScriptLimits.None"/> as the intentional boundless opt-out
    /// </summary>
    [TestFixture, Parallelizable]
    public class SecureByDefaultTests {

        /// <summary>
        /// generous ceiling for the single-shot pre-allocation guard's allocation-delta proof tests (T70/T71,
        /// design §8), mirroring <c>ExecutionGuardTests.MaxPreAllocationGuardDeltaBytes</c>
        /// </summary>
        const long MaxPreAllocationGuardDeltaBytes = 50_000_000;

        /// <summary>
        /// ceiling for T69's allocation delta: unlike a single-shot pre-allocation charge, the geometric
        /// addrange loop legitimately performs several real, successful doublings before the one that
        /// breaches the budget is refused, so the delta reflects that real, expected churn rather than being
        /// near-zero - this only needs to rule out the unbounded extrapolation the fix closes, not approach it
        /// </summary>
        const long MaxAddRangeChurnBytes = 300_000_000;

        static IScript ParseRecursiveFactorial(ScriptParser parser) {
            return parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invoke(1000000)"
            ));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T34: a default-constructed ScriptParser aborts a recursion deeper than DefaultMaxDepth without any Limits configured.")]
        public void T34_DefaultParserAbortsRecursionPastDefaultMaxDepth() {
            ScriptParser parser = new();
            IScript script = ParseRecursiveFactorial(parser);

            ScriptDepthLimitExceededException exception = Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
            Assert.That(exception.Limit, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T35: a shallow, sequential iterative transform over a large collection does not consume the default depth budget, since .where callbacks are sequential rather than nested.")]
        public void T35_DefaultParserCompletesShallowIterativeTransform() {
            ScriptParser parser = new();
            parser.Extensions.AddExtensions<EnumerableExtensions>();
            IScript script = parser.Parse("$src.where($x=>$x>0).count()");

            int[] source = Enumerable.Range(-5000, 10000).ToArray();
            int result = script.Execute<int>(new VariableProvider(new Variable("src", source)));

            Assert.That(result, Is.EqualTo(source.Count(x => x > 0)));
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("T36: a default-constructed ScriptParser aborts a script growing its own variable footprint past DefaultMaxVariableBytes without any Limits configured.")]
        public void T36_DefaultParserAbortsVariableGrowthPastDefaultMaxVariableBytes() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "while(true) {",
                "  $l.add(\"" + new string('x', 100_000) + "\")",
                "}"
            ));

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T37: assigning ScriptLimits.None restores the pre-flip boundless behavior, so a bounded (not truly unbounded) recursion past DefaultMaxDepth does not abort.")]
        public void T37_NoneOptOutRestoresUnboundedRecursion() {
            ScriptParser parser = new() {Limits = ScriptLimits.None};
            IScript script = parser.Parse(ScriptCode.Create(
                "$fac = $n=>{",
                "  if($n>0) {",
                "    return($fac.invoke($n-1))",
                "  }",
                "  return(0)",
                "}",
                "$fac.invoke(" + (ScriptLimits.DefaultMaxDepth + 5) + ")"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(10000)]
        [Description("T40 (DiVoid #7836/#7837): the round-4 red-team's exact reproduction - new list(100000000) assigned and held on a bare default-constructed ScriptParser - must now abort under DefaultMaxVariableBytes instead of allocating its ~800MB backing array silently.")]
        public void T40_DefaultParserAbortsHeldMegaPreSizedList() {
            ScriptParser parser = new();
            IScript script = parser.Parse("$a = new list(100000000)");

            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
        }

        [Test]
        [Description("T38: ScriptLimits.Default pins the four secured knobs (DiVoid #7869 MaxParseDepth, #7871 RegexTimeout, alongside the original MaxDepth/MaxVariableBytes) and leaves every other knob unset.")]
        public void T38_DefaultInstanceValues() {
            Assert.That(ScriptLimits.Default.MaxDepth, Is.EqualTo(ScriptLimits.DefaultMaxDepth));
            Assert.That(ScriptLimits.Default.MaxVariableBytes, Is.EqualTo(ScriptLimits.DefaultMaxVariableBytes));
            Assert.That(ScriptLimits.Default.MaxParseDepth, Is.EqualTo(ScriptLimits.DefaultMaxParseDepth));
            Assert.That(ScriptLimits.Default.RegexTimeout, Is.EqualTo(ScriptLimits.DefaultRegexTimeout));
            Assert.That(ScriptLimits.Default.MaxSteps, Is.Null);
            Assert.That(ScriptLimits.Default.Timeout, Is.Null);
            Assert.That(ScriptLimits.Default.MaxVariables, Is.Null);
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T39: ScriptLimits.Default is a safe shared instance under init-only knobs - repointing one parser to None must not affect another default-configured parser.")]
        public void T39_DefaultInstanceIsSafeSharedAcrossParsers() {
            ScriptParser optedOutParser = new() {Limits = ScriptLimits.None};
            ScriptParser defaultParser = new();

            Assert.That(optedOutParser.Limits, Is.SameAs(ScriptLimits.None));
            Assert.That(defaultParser.Limits, Is.SameAs(ScriptLimits.Default));

            IScript script = ParseRecursiveFactorial(defaultParser);
            Assert.Throws<ScriptDepthLimitExceededException>(() => script.Execute());
        }

        [Test, Parallelizable, MaxTime(5000)]
        [Description("T69 (DiVoid #7870, design §8.1 rows 7-8): the geometric $l.addrange($l) bypass throws ScriptVariableLimitExceededException instead of completing 4-6x over the byte ceiling; the allocation delta reflects the real, legitimate doublings that succeeded before the refused one, not the unbounded multi-gigabyte extrapolation the fix closes. Control: the linear add-loop equivalent still throws, unaffected by this change.")]
        public void T69_AddRangeGeometricBypassThrowsBeforeAllocation() {
            ScriptParser parser = new();
            IScript script = parser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$l.add(1)",
                "$i = 0",
                "while($i < 26) {",
                "  $l.addrange($l)",
                "  $i = $i + 1",
                "}"
            ));

            long before = GC.GetAllocatedBytesForCurrentThread();
            ScriptVariableLimitExceededException exception = Assert.Throws<ScriptVariableLimitExceededException>(() => script.Execute());
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(exception.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(delta, Is.LessThan(MaxAddRangeChurnBytes));

            ScriptParser controlParser = new() {Limits = new ScriptLimits {MaxVariableBytes = 800_000}};
            IScript controlScript = controlParser.Parse(ScriptCode.Create(
                "$l = new list()",
                "$i = 0",
                "while($i < 200000) {",
                "  $l.add(1)",
                "  $i = $i + 1",
                "}"
            ));
            Assert.Throws<ScriptVariableLimitExceededException>(() => controlScript.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T70 (DiVoid #7868 F2, design §8.1 rows 4-5): String.Replace/ReplaceLineEndings amplifying by an attacker-chosen factor throw before allocation on a bare parser; a small, everyday replace still completes.")]
        public void T70_ReplaceAmplificationThrowsBeforeAllocation() {
            ScriptParser parser = new();

            IScript replaceScript = parser.Parse(ScriptCode.Create(
                "$r = new string('b',1000)",
                "$s = new string('a',200000)",
                "$s.replace(\"a\",$r)"
            ));
            long before = GC.GetAllocatedBytesForCurrentThread();
            ScriptVariableLimitExceededException replaceException = Assert.Throws<ScriptVariableLimitExceededException>(() => replaceScript.Execute());
            long replaceDelta = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(replaceException.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(replaceDelta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));

            IScript replaceLineEndingsScript = parser.Parse(ScriptCode.Create(
                "$r = new string('b',300)",
                "$s = new string('a',500000)",
                "$s.replacelineendings($r)"
            ));
            ScriptVariableLimitExceededException lineEndingsException = Assert.Throws<ScriptVariableLimitExceededException>(() => replaceLineEndingsScript.Execute());
            Assert.That(lineEndingsException.Kind, Is.EqualTo(VariableLimitKind.Bytes));

            IScript legitScript = parser.Parse(ScriptCode.Create(
                "$s = \"hello world\"",
                "$s.replace(\"o\",\"OO\")"
            ));
            Assert.DoesNotThrow(() => legitScript.Execute());
        }

        [Test, Parallelizable, MaxTime(2000)]
        [Description("T71 (DiVoid #7868 F3, design §8.1 row 6): String.Split - the string-separator, char-separator and count-capped overloads alike - throws before allocation on a receiver long enough to breach the budget, since the projection is receiver-derived and unaffected by the count cap; a small, everyday split still completes.")]
        public void T71_SplitReceiverDerivedProjectionThrowsBeforeAllocation() {
            ScriptParser parser = new();

            IScript stringSeparatorScript = parser.Parse(ScriptCode.Create(
                "$s = new string(',', 20000000)",
                "$s.split(\",\")"
            ));
            long before = GC.GetAllocatedBytesForCurrentThread();
            ScriptVariableLimitExceededException stringSeparatorException = Assert.Throws<ScriptVariableLimitExceededException>(() => stringSeparatorScript.Execute());
            long stringSeparatorDelta = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(stringSeparatorException.Kind, Is.EqualTo(VariableLimitKind.Bytes));
            Assert.That(stringSeparatorDelta, Is.LessThan(MaxPreAllocationGuardDeltaBytes));

            IScript charSeparatorScript = parser.Parse(ScriptCode.Create(
                "$s = new string(',', 20000000)",
                "$s.split(',')"
            ));
            Assert.Throws<ScriptVariableLimitExceededException>(() => charSeparatorScript.Execute());

            IScript countCappedScript = parser.Parse(ScriptCode.Create(
                "$s = new string(',', 20000000)",
                "$s.split(\",\",5)"
            ));
            Assert.Throws<ScriptVariableLimitExceededException>(() => countCappedScript.Execute());

            IScript legitScript = parser.Parse("\"a,b,c\".split(\",\")");
            Assert.DoesNotThrow(() => legitScript.Execute());
        }
    }
}
