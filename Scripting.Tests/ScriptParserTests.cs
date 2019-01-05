using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Operations.Assign;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Tokens;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture]
    public class ScriptParserTests {

        [Test]
        public void TestMethodCallWithArray() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod(fuck,[you])");
            Assert.DoesNotThrow(() => token.Execute());
        }

        [Test]
        public void TestMethodCallWithSpaces() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod( fuck ,[ you , \"for\",real ])");
            string result = token.Execute() as string;
            Assert.AreEqual("fuck_you,for,real", result);
        }

        [Test]
        public void TestTabInParameter() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod( \\\" ,[   \"\\t\"])");
            string result = token.Execute() as string;
            Assert.AreEqual("\"_\t", result);
        }

        [Test]
        public void AssignVariable() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=7\n" +
                "$number"
            );
            Assert.AreEqual(7, script.Execute());
        }

        [Test]
        public void CallVariableMember() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=7\n"+
                "$number.tostring()"
            );
            Assert.AreEqual("7", script.Execute());
        }

        [Test]
        public void ReadVariableMember()
        {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test]
        public void ReadMemberChain()
        {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$number=\"longstring\"\n" +
                "$number.length.tostring()"
            );
            Assert.AreEqual("10", script.Execute());
        }

        [Test]
        public void ExtensionMethods() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<TestExtensions>();
            Assert.AreEqual("longstring", parser.Parse("\"long\".append(string)").Execute());
        }

        [Test]
        public void ExtensionsForBaseTypes()
        {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<TestExtensions>();
            Assert.DoesNotThrow(() => parser.Parse("\"long\".hashy()").Execute());
        }

        [Test]
        public void MethodParametersWithMethodCalls()
        {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript token = parser.Parse("test.testmethod(test.testmethod(a,[b]),test.testmethod(c,[d,e]))");
            Assert.AreEqual("a_b_c_d,e", token.Execute());
        }

        [Test]
        public void CallIndexer()
        {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            parser.Parse("test[data]=8").Execute();
            Assert.AreEqual(8, parser.Parse("test[data]").Execute());
        }

        [Test]
        public void CallIndexerOnString()
        {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual('s', parser.Parse("testedstuff[6]").Execute());
        }

        [Test]
        public void CallIndexerOnArray()
        {
            ScriptParser parser = new ScriptParser();
            Assert.AreEqual(9, parser.Parse("[9,7,3,3,2,9,0][5]").Execute());
        }

        [Test]
        public void SetArrayValue() {
            ScriptParser parser=new ScriptParser();
            IScript script=parser.Parse(
                "$array=[0,1,2,3,4,5]\n" +
                "$array[2]=7\n" +
                "$array"
            );
            Assert.IsTrue(new[] {0, 1, 7, 3, 4, 5}.Cast<object>().SequenceEqual((IEnumerable<object>) script.Execute()));
        }

        [Test]
        public void MethodCallWithParameter() {
            ScriptParser parser = new ScriptParser(new Variable("test", new TestHost()));
            IScript script = parser.Parse(
                "$parameter1=test1\n" +
                "$parameter2=test2\n" +
                "test.testmethod($parameter1,[$parameter2])"
            );
            Assert.AreEqual("test1_test2",script.Execute());
        }

        [Test]
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
    }
}