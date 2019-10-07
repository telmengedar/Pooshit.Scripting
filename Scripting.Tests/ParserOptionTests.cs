using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;
using NightlyCode.Scripting.Visitors;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParserOptionTests {

        [Test, Parallelizable]
        public void TypeInstanceProvidersDisabled() {
            IScriptParser parser = new ScriptParser {
                TypeInstanceProvidersEnabled = false
            };

            IScript script = parser.Parse("$collection=new list() $collection.count");

            MetricVisitor visitor=new MetricVisitor();
            visitor.Visit(script);
            Assert.AreEqual(0, visitor.NewInstances);
        }

        [Test, Parallelizable]
        public void TypeCastsDisabled() {
            IScriptParser parser = new ScriptParser {
                TypeCastsEnabled = false
            };

            IScript script = parser.Parse("$number=int(\"7\")");
            MetricVisitor visitor=new MetricVisitor();
            visitor.Visit(script);
            Assert.AreEqual(0, visitor.TypeCasts);
        }

        [TestCase("wait(0)")]
        [TestCase("if(true) 7 else 3")]
        [TestCase("for($i=0,$i<7,++$i) return")]
        [TestCase("foreach($item,[1,2,3,4,5]) break")]
        [TestCase("while(false) return")]
        [TestCase("switch(7) case(7) break default break")]
        [TestCase("return 3")]
        [TestCase("throw(\"Test\")")]
        [TestCase("break")]
        [TestCase("continue")]
        [TestCase("$fake=0 using($fake) return")]
        [TestCase("try throw(\"Error\") catch return $exception")]
        [Parallelizable]
        public void ControlTokensDisabled(string code) {
            MetricVisitor visitor=new MetricVisitor();
            IScriptParser enabledparser = new ScriptParser();
            IScriptParser disabledparser = new ScriptParser {
                ControlTokensEnabled = false
            };

            IScript enabledscript = enabledparser.Parse(code);
            visitor.Visit(enabledscript);
            Assert.Greater(visitor.ControlTokens+visitor.FlowTokens, 0);

            IScript disabledscript=disabledparser.Parse(code);
            visitor.Visit(disabledscript);
            Assert.AreEqual(0, visitor.ControlTokens+visitor.FlowTokens);
        }

        [Test, Parallelizable]
        public void ImportsDisabled() {
            ScriptParser parser = new ScriptParser {
                ImportsEnabled = false,
            };
            parser.MethodResolver = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);

            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import(\"Scripting.Tests.Scripts.External.increasedbyone.ns\")",
                    "return($method.invoke(10))"
                ));

            MetricVisitor visitor=new MetricVisitor();
            visitor.Visit(script);
            Assert.AreEqual(0, visitor.Imports);
        }
    }
}