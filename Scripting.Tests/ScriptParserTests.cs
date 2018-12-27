using System;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture]
    public class ScriptParserTests {

        [TestCase("test.member=value", typeof(ScriptAssignment))]
        [TestCase("test.method(service,user,parameter)", typeof(ScriptMethod))]
        [TestCase("test.method(test.member)", typeof(ScriptMethod))]
        [TestCase("$test", typeof(ScriptVariable))]
        [TestCase("test.method($test,2)", typeof(ScriptMethod))]
        [TestCase("test.method(\"\",clean)", typeof(ScriptMethod))]
        [TestCase("test.speak(\"It is quite simple\",\"CereVoice Stuart - English (Scotland)\")", typeof(ScriptMethod))]
        [TestCase("test.method(1,2,3,[4,4])", typeof(ScriptMethod))]
        [TestCase("test.property=255.34", typeof(ScriptAssignment))]
        [TestCase("\"TestValue\"", typeof(ScriptValue))]
        [TestCase("122.3", typeof(ScriptValue))]
        public void TestValidStatements(string statement, Type expectedroot) {
            ScriptHosts hostpool = new ScriptHosts {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            IScriptToken token = parser.Parse(statement, new VariableContext(new Tuple<string, object>("test", "test")));
            Assert.AreEqual(expectedroot, token.GetType());
        }

        [Test]
        public void TestMethodCallWithArray() {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            IScriptToken token = parser.Parse("test.testmethod(fuck,[you])");
            Assert.DoesNotThrow(() => token.Execute());
        }

        [Test]
        public void TestMethodCallWithSpaces() {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            IScriptToken token = parser.Parse("test.testmethod( fuck ,[ you , \"for\",real ])");
            string result = token.Execute() as string;
            Assert.AreEqual("fuck_you,for,real", result);
        }

        [Test]
        public void TestTabInParameter() {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            IScriptToken token = parser.Parse("test.testmethod( \\\" ,[   \"\\t\"])");
            string result = token.Execute() as string;
            Assert.AreEqual("\"_\t", result);
        }

        [Test]
        public void AssignVariable() {
            VariableContext variables =new VariableContext();
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual(7, parser.Parse("$number=7", variables).Execute());
            Assert.AreEqual(7, parser.Parse("$number", variables).Execute());
        }

        [Test]
        public void CallVariableMember() {
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual(7, parser.Parse("$number=7", variables).Execute());
            Assert.AreEqual("7", parser.Parse("$number.tostring()", variables).Execute());
        }

        [Test]
        public void ReadVariableMember()
        {
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual("longstring", parser.Parse("$number=\"longstring\"", variables).Execute());
            Assert.AreEqual(10, parser.Parse("$number.length", variables).Execute());
        }

        [Test]
        public void ReadMemberChain()
        {
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual("longstring", parser.Parse("$number=\"longstring\"", variables).Execute());
            Assert.AreEqual("10", parser.Parse("$number.length.tostring()", variables).Execute());
        }

        [Test]
        public void ExtensionMethods() {
            ScriptHosts scripthost = new ScriptHosts();
            scripthost.AddExtensions<TestExtensions>();
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(scripthost);
            Assert.AreEqual("longstring", parser.Parse("\"long\".append(string)", variables).Execute());
        }

        [Test]
        public void ExtensionsForBaseTypes()
        {
            ScriptHosts scripthost = new ScriptHosts();
            scripthost.AddExtensions<TestExtensions>();
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(scripthost);
            Assert.DoesNotThrow(() => parser.Parse("\"long\".hashy()", variables).Execute());
        }

        [Test]
        public void MethodParametersWithMethodCalls()
        {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            IScriptToken token = parser.Parse("test.testmethod(test.testmethod(a,[b]),test.testmethod(c,[d,e]))");
            Assert.AreEqual("a_b_c_d,e", token.Execute());
        }

        [Test]
        public void CallIndexer()
        {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            ScriptParser parser = new ScriptParser(hostpool);
            parser.Parse("test[data]=8").Execute();
            Assert.AreEqual(8, parser.Parse("test[data]").Execute());
        }

        [Test]
        public void CallIndexerOnString()
        {
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual('s', parser.Parse("testedstuff[6]").Execute());
        }

        [Test]
        public void CallIndexerOnArray()
        {
            ScriptParser parser = new ScriptParser(new ScriptHosts());
            Assert.AreEqual(9, parser.Parse("[9,7,3,3,2,9,0][5]").Execute());
        }

        [Test]
        public void SetArrayValue() {
            VariableContext variables=new VariableContext();
            ScriptParser parser=new ScriptParser(new ScriptHosts());
            parser.Parse("$array=[0,1,2,3,4,5]", variables).Execute();
            parser.Parse("$array[2]=7", variables).Execute();
            Assert.IsTrue(new[] {0L, 1L, 7L, 3L, 4L, 5L}.Cast<object>().SequenceEqual((IEnumerable<object>)parser.Parse("$array", variables).Execute()));
        }

        [Test]
        public void MethodCallWithParameter() {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(hostpool);
            parser.Parse("$parameter1=test1", variables).Execute();
            parser.Parse("$parameter2=test2", variables).Execute();
            Assert.AreEqual("test1_test2",parser.Parse("test.testmethod($parameter1,[$parameter2])", variables).Execute());
        }

        [Test]
        public void ObjectParameterCallWithParameter()
        {
            ScriptHosts hostpool = new ScriptHosts
            {
                ["test"] = new TestHost()
            };
            VariableContext variables = new VariableContext();
            ScriptParser parser = new ScriptParser(hostpool);
            parser.Parse("$parameter=test", variables).Execute();
            parser.Parse("test.addtesthost(host,$parameter)", variables).Execute();
            Assert.AreEqual(hostpool["test"], parser.Parse("test[host]", variables).Execute());
        }
    }
}