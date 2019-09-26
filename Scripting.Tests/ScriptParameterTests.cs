using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Visitors;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ScriptParameterTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void DetectScriptParameter() {
            IScript script = parser.Parse("method.call($parameter)", new Variable("method"));
            ParameterExtractor extractor=new ParameterExtractor();
            extractor.Visit(script);
            Assert.That(new[] {"parameter"}.SequenceEqual(extractor.Parameters));
        }

        [Test, Parallelizable]
        public void DetectVariableInitialization() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$parameter=8",
                "method.call($parameter)"
            ), new Variable("method"));
            ParameterExtractor extractor=new ParameterExtractor();
            extractor.Visit(script);
            Assert.That(!extractor.Parameters.Any());
        }

        [Test, Parallelizable]
        public void DetectParameterAfterVariableInitializationInInnerBlock() {
            IScript script = parser.Parse(ScriptCode.Create(
                "if(20) {",
                "  $parameter=8",
                "}",
                "method.call($parameter)"
            ), new Variable("method"));
            ParameterExtractor extractor=new ParameterExtractor();
            extractor.Visit(script);
            Assert.That(new[] {"parameter"}.SequenceEqual(extractor.Parameters));
        }

        [Test, Parallelizable]
        public void ExceptionInCatchBlockIsResolved() {
            IScript script = parser.Parse(ScriptCode.Create(
                "try {",
                "   method.call(3)",
                "}",
                "catch {",
                "   return($exception.message)",
                "}"
            ), new Variable("method"));

            ParameterExtractor extractor = new ParameterExtractor();
            extractor.Visit(script);
            Assert.That(!extractor.Parameters.Contains("exception"));
        }

        [Test, Parallelizable]
        public void ExceptionInCatchStatementIsResolved() {
            IScript script = parser.Parse(ScriptCode.Create(
                "try",
                "  method.call(3)",
                "catch",
                "  return($exception.message)"
            ), new Variable("method"));

            ParameterExtractor extractor = new ParameterExtractor();
            extractor.Visit(script);
            Assert.That(!extractor.Parameters.Contains("exception"));
        }

    }
}