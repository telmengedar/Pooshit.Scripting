using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Tokens;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Tokens;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture]
    public class ScriptParserTests {
        readonly IScriptParser globalparser = new ScriptParser();

        public static IEnumerable<string> IncompleteScripts {
            get {
                foreach(string resource in typeof(ScriptParserTests).Assembly.GetManifestResourceNames().Where(r => r.StartsWith("Scripting.Tests.Scripts.Incomplete.")))
                    using(StreamReader reader = new StreamReader(typeof(ScriptParserTests).Assembly.GetManifestResourceStream(resource)))
                        yield return reader.ReadToEnd();
            }
        }

        public static IEnumerable<string> ValidScripts {
            get {
                foreach(string resource in typeof(ScriptParserTests).Assembly.GetManifestResourceNames().Where(r => r.StartsWith("Scripting.Tests.Scripts.Valid.")))
                    using(StreamReader reader = new StreamReader(typeof(ScriptParserTests).Assembly.GetManifestResourceStream(resource)))
                        yield return reader.ReadToEnd();
            }
        }

        [Test, Parallelizable]
        public void TestMethodCallWithArray() {
            ScriptParser parser = new ScriptParser();
            IScript token = parser.Parse("test.testmethod(\"fuck\",[\"you\"])");
            Assert.DoesNotThrow(() => token.Execute(new VariableProvider(new Variable("test", new TestHost()))));
        }

        [Test, Parallelizable]
        public void TestMethodCallWithSpaces() {
            ScriptParser parser = new ScriptParser();
            IScript token = parser.Parse("test.testmethod( \"fuck\" ,[ \"you\" , \"for\",\"real\" ])");
            string result = token.Execute(new VariableProvider(new Variable("test", new TestHost()))) as string;
            Assert.AreEqual("fuck_you,for,real", result);
        }

        [Test, Parallelizable]
        public void TestTabInParameter() {
            ScriptParser parser = new ScriptParser();
            IScript token = parser.Parse("test.testmethod( \"\\\"\" ,[   \"\\t\"])");
            string result = token.Execute(new VariableProvider(new Variable("test", new TestHost()))) as string;
            Assert.AreEqual("\"_\t", result);
        }

        [Test, Parallelizable]
        public void AssignVariable() {
            IScript script = globalparser.Parse(
                "$number=7\n" +
                "$number"
            );
            Assert.AreEqual(7, script.Execute());
        }

        [Test, Parallelizable]
        public void CallVariableMember() {
            IScript script = globalparser.Parse(
                "$number=7\n" +
                "$number.tostring()"
            );
            Assert.AreEqual("7", script.Execute());
        }

        [Test, Parallelizable]
        public void ReadVariableMember() {
            IScript script = globalparser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test, Parallelizable]
        public void ReadMemberChain() {
            IScript script = globalparser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length.tostring()"
            );
            Assert.AreEqual("10", script.Execute());
        }

        [Test, Parallelizable]
        public void ExtensionMethods() {
            globalparser.Extensions.AddExtensions<TestExtensions>();
            Assert.AreEqual("longstring", globalparser.Parse("\"long\".append(\"string\")").Execute());
        }

        [Test, Parallelizable]
        public void ExtensionsForBaseTypes() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<TestExtensions>();
            Assert.DoesNotThrow(() => parser.Parse("\"long\".hashy()").Execute());
        }

        [Test, Parallelizable]
        public void MethodParametersWithMethodCalls() {
            ScriptParser parser = new ScriptParser();
            IScript token = parser.Parse("test.testmethod(test.testmethod(\"a\",[\"b\"]),test.testmethod(\"c\",[\"d\",\"e\"]))");
            Assert.AreEqual("a_b_c_d,e", token.Execute(new VariableProvider(new Variable("test", new TestHost()))));
        }

        [Test, Parallelizable]
        public void CallIndexer() {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser();
            parser.Parse("test[\"data\"]=8").Execute(new VariableProvider(new Variable("test", testhost)));
            Assert.AreEqual(8, parser.Parse("test[\"data\"]").Execute(new VariableProvider(new Variable("test", testhost))));
        }

        [Test, Parallelizable]
        public void CallIndexerOnString() {
            Assert.AreEqual('s', globalparser.Parse("\"testedstuff\"[6]").Execute());
        }

        [Test, Parallelizable]
        public void CallIndexerOnArray() {
            Assert.AreEqual(9, globalparser.Parse("[9,7,3,3,2,9,0][5]").Execute());
        }

        [Test, Parallelizable]
        public void SetArrayValue() {
            IScript script = globalparser.Parse(
                "$array=[0,1,2,3,4,5]\n" +
                "$array[2]=7\n" +
                "$array"
            );
            Assert.IsTrue(new[] { 0, 1, 7, 3, 4, 5 }.Cast<object>().SequenceEqual((IEnumerable<object>)script.Execute()));
        }

        [Test, Parallelizable]
        public void MethodCallWithParameter() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$parameter1=\"test1\"\n" +
                "$parameter2=\"test2\"\n" +
                "test.testmethod($parameter1,[$parameter2])"
            );
            Assert.AreEqual("test1_test2", script.Execute(new VariableProvider(new Variable("test", new TestHost()))));
        }

        [Test, Parallelizable]
        public void ObjectParameterCallWithParameter() {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$parameter=test\n" +
                "test.addtesthost(\"host\",$parameter)\n" +
                "test[\"host\"]"
            );

            object result = script.Execute(new VariableProvider(new Variable("test", testhost)));
            Assert.AreEqual(testhost["host"], result);
        }

        [Test, Parallelizable]
        [Description("Provides some incomplete scripts and ensures that the parser doesn't freeze.")]
        public async Task ParseIncompleteScript([ValueSource(nameof(IncompleteScripts))]string scriptdata) {
            ScriptParser parser = new ScriptParser();
            Task parsetask = Task.Run(() => parser.Parse(scriptdata));
            if(await Task.WhenAny(parsetask, Task.Delay(200)) != parsetask) {
                Assert.Fail("Parser did not succeed in time");
            }

            Assert.Pass();
        }

        [Test, Parallelizable]
        public void ParseValidScript([ValueSource(nameof(ValidScripts))]string scriptdata) {
            ScriptParser parser = new ScriptParser();
            Assert.DoesNotThrow(() => parser.Parse(scriptdata));
        }

        [Test, Parallelizable]
        public void EmptyLineAtEnd() {
            Assert.DoesNotThrow(() => globalparser.Parse(
                "for ($variable=3,$variable<7,++$variable)\n" +
                "{\n" +
                "    $variable\n" +
                "}\n" +
                " "));
        }

        [Test, Parallelizable]
        public void CommentAtEnd() {
            Assert.DoesNotThrow(() => globalparser.Parse(
                "for ($variable=3,$variable<7,++$variable)\n" +
                "{\n" +
                "    $variable\n" +
                "}\n" +
                "// some comment"));
        }

        [Test, Parallelizable]
        public void SingleBreakInWhile() {
            IScript script = globalparser.Parse("while(true) break");
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable]
        public void VariableDeclarationAfterBlock() {
            Assert.DoesNotThrow(() => globalparser.Parse("if($command.arguments.length<2) {\n" +
                                                   "    $channel.sendmessage(\"Syntax: command <name> [@permissions...] <code>\")\n" +
                                                   "    return\n" +
                                                   "}\n" +
                                                   "\n" +
                                                   "$permissions=\"\"\n" +
                                                   "$codestarted=false\n" +
                                                   "$code=\"\"\n"));
        }

        [Test, Parallelizable]
        public void MemberIndex() {
            IScript script = globalparser.Parse("$host.member");
            int textindex = ((script.Body as StatementBlock)?.Children.First() as ICodePositionToken)?.TextIndex ?? -1;
            Assert.AreEqual(6, textindex);
        }

        [Test, Parallelizable]
        public void MethodIndex() {
            IScript script = globalparser.Parse("$host.method(1,2,3)");
            int textindex = ((script.Body as StatementBlock)?.Children.First() as ICodePositionToken)?.TextIndex ?? -1;
            Assert.AreEqual(6, textindex);
        }

        [Test, Parallelizable]
        public void IndexerPositionIndex() {
            IScript script = globalparser.Parse("$host[1,2,3]");
            int textindex = ((script.Body as StatementBlock)?.Children.First() as ICodePositionToken)?.TextIndex ?? -1;
            Assert.AreEqual(5, textindex);
        }

        [Test, Parallelizable]
        public void CompileWeirdCode() {
            string code = File.ReadAllText("Data/weirdcode.ns");
            globalparser.Parse(code);
        }
        
        [Test, Parallelizable]
        public void CompileComplexDictionary() {
            string code = File.ReadAllText("Data/formattedcomplexdictionary.ns");
            globalparser.Parse(code);
        }
    }
}