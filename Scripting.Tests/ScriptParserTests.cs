using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture]
    public class ScriptParserTests {
        IScriptParser parser=new ScriptParser();

        public static IEnumerable<string> IncompleteScripts {
            get {
                foreach(string resource in typeof(ScriptParserTests).Assembly.GetManifestResourceNames().Where(r=>r.StartsWith("Scripting.Tests.Scripts.Incomplete.")))
                    using (StreamReader reader = new StreamReader(typeof(ScriptParserTests).Assembly.GetManifestResourceStream(resource)))
                        yield return reader.ReadToEnd();
            }
        }

        [Test, Parallelizable]
        public void TestMethodCallWithArray() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod(fuck,[you])");
            Assert.DoesNotThrow(() => token.Execute());
        }

        [Test, Parallelizable]
        public void TestMethodCallWithSpaces() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod( fuck ,[ you , \"for\",real ])");
            string result = token.Execute() as string;
            Assert.AreEqual("fuck_you,for,real", result);
        }

        [Test, Parallelizable]
        public void TestTabInParameter() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod( \\\" ,[   \"\\t\"])");
            string result = token.Execute() as string;
            Assert.AreEqual("\"_\t", result);
        }

        [Test, Parallelizable]
        public void AssignVariable() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=7\n" +
                "$number"
            );
            Assert.AreEqual(7, script.Execute());
        }

        [Test, Parallelizable]
        public void CallVariableMember() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=7\n"+
                "$number.tostring()"
            );
            Assert.AreEqual("7", script.Execute());
        }

        [Test, Parallelizable]
        public void ReadVariableMember()
        {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test, Parallelizable]
        public void ReadMemberChain()
        {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length.tostring()"
            );
            Assert.AreEqual("10", script.Execute());
        }

        [Test, Parallelizable]
        public void ExtensionMethods() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<TestExtensions>();
            Assert.AreEqual("longstring", parser.Parse("\"long\".append(string)").Execute());
        }

        [Test, Parallelizable]
        public void ExtensionsForBaseTypes()
        {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<TestExtensions>();
            Assert.DoesNotThrow(() => parser.Parse("\"long\".hashy()").Execute());
        }

        [Test, Parallelizable]
        public void MethodParametersWithMethodCalls()
        {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod(test.testmethod(a,[b]),test.testmethod(c,[d,e]))");
            Assert.AreEqual("a_b_c_d,e", token.Execute());
        }

        [Test, Parallelizable]
        public void CallIndexer()
        {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            parser.Parse("test[data]=8").Execute();
            Assert.AreEqual(8, parser.Parse("test[data]").Execute());
        }

        [Test, Parallelizable]
        public void CallIndexerOnString()
        {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual('s', parser.Parse("testedstuff[6]").Execute());
        }

        [Test, Parallelizable]
        public void CallIndexerOnArray()
        {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual(9, parser.Parse("[9,7,3,3,2,9,0][5]").Execute());
        }

        [Test, Parallelizable]
        public void SetArrayValue() {
            ScriptParser parser=new ScriptParser();
            IScript script=parser.Parse(
                "$array=[0,1,2,3,4,5]\n" +
                "$array[2]=7\n" +
                "$array"
            );
            Assert.IsTrue(new[] {0, 1, 7, 3, 4, 5}.Cast<object>().SequenceEqual((IEnumerable<object>) script.Execute()));
        }

        [Test, Parallelizable]
        public void MethodCallWithParameter() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript script = parser.Parse(
                "$parameter1=test1\n" +
                "$parameter2=test2\n" +
                "test.testmethod($parameter1,[$parameter2])"
            );
            Assert.AreEqual("test1_test2",script.Execute());
        }

        [Test, Parallelizable]
        public void ObjectParameterCallWithParameter() {
            TestHost testhost = new TestHost();
            ScriptParser parser = new ScriptParser(new Variable("test", testhost));
            IScript script = parser.Parse(
                "$parameter=test\n" +
                "test.addtesthost(host,$parameter)\n" +
                "test[host]"
            );

            object result = script.Execute();
            Assert.AreEqual(testhost["host"], result);
        }

        [Test, Parallelizable]
        [Description("Provides some incomplete scripts and ensures that the parser doesn't freeze.")]
        public async Task ParseIncompleteScript([ValueSource(nameof(IncompleteScripts))]string scriptdata) {
            IScriptParser parser = new ScriptParser();

            Task parsetask = Task.Run(() => parser.Parse(scriptdata));
            if (await Task.WhenAny(parsetask, Task.Delay(200)) != parsetask) {
                Assert.Fail("Parser did not succeed in time");
            }

            Assert.Pass();
        }

        [Test, Parallelizable]
        public void EmptyLineAtEnd()
        {
            IScriptParser parser = new ScriptParser();
            Assert.DoesNotThrow(() => parser.Parse(
                "for ($variable=3,$variable<7,++$variable)\n" +
                "{\n" +
                "    $variable\n" +
                "}\n" +
                " "));
        }

        [Test, Parallelizable]
        public void CommentAtEnd()
        {
            IScriptParser parser = new ScriptParser();
            Assert.DoesNotThrow(() => parser.Parse(
                "for ($variable=3,$variable<7,++$variable)\n" +
                "{\n" +
                "    $variable\n" +
                "}\n" +
                "// some comment"));
        }

        [Test, Parallelizable]
        public void SingleBreakInWhile() {
            IScript script = parser.Parse("while(true) break");
            Assert.DoesNotThrow(() => script.Execute());
        }
    }
}