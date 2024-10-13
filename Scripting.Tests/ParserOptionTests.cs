using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Providers;
using Pooshit.Scripting.Visitors;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParserOptionTests {

        [Test, Parallelizable]
        public void TypeInstanceProvidersDisabled() {
            IScriptParser parser = new ScriptParser {
                TypeInstanceProvidersEnabled = false
            };

            Assert.Throws<ScriptParserException>(() => parser.Parse("$collection=new list() $collection.count"));
        }

        [Test, Parallelizable]
        public void TypeCastsDisabled() {
            IScriptParser parser = new ScriptParser {
                TypeCastsEnabled = false
            };

            Assert.Throws<ScriptParserException>(() => parser.Parse("$number=int(\"7\")"));
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
            MetricVisitor visitor = new MetricVisitor();
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
            parser.ImportProvider = new ResourceScriptMethodProvider(typeof(ImportTests).Assembly, parser);

            IScript script = parser.Parse(
                ScriptCode.Create(
                    "$method=import(\"Scripting.Tests.Scripts.External.increasedbyone.ns\")",
                    "return($method.invoke(10))"
                ));

            MetricVisitor visitor=new MetricVisitor();
            visitor.Visit(script);
            Assert.AreEqual(0, visitor.Imports);
        }

        [Test, Parallelizable]
        public void AllowSingleQuoteForStrings() {
            ScriptParser parser = new ScriptParser {
                AllowSingleQuotesForStrings = true
            };
            IScript script = parser.Parse(
                ScriptCode.Create(
                    "return('bullshit')"
                )
            );

            string value = script.Execute<string>();
            Assert.AreEqual("bullshit", value);
        }
    }
}