
using System;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Tokens;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Parser.Extract;
using Pooshit.Scripting.Tokens;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ExtractTokenTests {
        readonly ITokenExtractor extractor = new TokenExtractor(new ScriptParser());

        [TestCase("host.meth.bof", 9, nameof(ScriptMember),"$host.meth","$host.meth")]
        [TestCase("host.meth.bof", 11, nameof(ScriptMember),"$host.meth.b","$host.meth.bof")]
        [TestCase("host.meth", 9, nameof(ScriptMember),"$host.meth","$host.meth")]
        [TestCase("host.meth", 7, nameof(ScriptMember),"$host.me","$host.meth")]
        [TestCase("host.meth", 2, nameof(ScriptVariable),"$ho","$host")]
        [TestCase("host.", 5, nameof(ScriptVariable),"$host","$host")]
        [TestCase("host(", 5, nameof(ScriptMethod),"$host.invoke()","$host.invoke()")]
        [TestCase("host(5", 6, nameof(ScriptValue),"5","5")]
        [TestCase("host(\"for\",", 5, nameof(ScriptMethod),"$host.invoke()","$host.invoke()")]
        [TestCase("host.boll(\"for\",", 11, nameof(ScriptValue),"\"\"","\"for\"")]
        [TestCase("host.boll(\"for\",7,", 13, nameof(ScriptValue),"\"fo\"","\"for\"")]
        [TestCase("await($productionapi.v1.call(7))", 26, nameof(ScriptMember),"$productionapi.v1.ca","$productionapi.v1.call")]
        [TestCase("backend[\"bla\"]", 8, nameof(ScriptIndexer),"$backend[]","$backend[]")]
        [TestCase("$Fields.add($zipher.field(\"Date\", $holidays.addworkdays($Order.from, 3, \"RhinelandPalatinate\")))", 48, nameof(ScriptMember), "$holidays.addw", "$holidays.addworkdays")]
        [TestCase("$log.info($\"Creating device task for {$parcel.name}\")", 48,nameof(ScriptMember), "$parcel.na", "$parcel.name")]
        [TestCase("\t  } ", 2, null, null, null)]
        [TestCase("$product+$moduct", 3, nameof(ScriptVariable), "$pr", "$product")]
        [TestCase("$backend[$serial]",12,nameof(ScriptVariable), "$se", "$serial")]
        public void WildExtract(string script, int position, string expectedtype, string expectedresult, string expectedfullresult) {
            IScriptToken token= extractor.ExtractToken(script, position, false);
            if (string.IsNullOrEmpty(expectedtype)) {
                Assert.Null(token);
                return;
            }

            Assert.NotNull(token);

            Console.WriteLine(token.ToString());
            Assert.AreEqual(expectedtype, token.GetType().Name);
            Assert.AreEqual(expectedresult, token.ToString());

            token= extractor.ExtractToken(script, position);
            Assert.NotNull(token);

            Console.WriteLine(token.ToString());
            Assert.AreEqual(expectedtype, token.GetType().Name);
            Assert.AreEqual(expectedfullresult, token.ToString());
        }

        [TestCase("$productionapi.v1.devices.tasks.createtask()", 21, 18, 7)]
        [TestCase("using($productionapi.createtask()){ return(3) }", 32, 21, 12)]
        public void TokenPositionAndLength(string script, int position, int index, int length) {
            IScriptToken token = extractor.ExtractToken(script, position);
            ICodePositionToken codeposition=token as ICodePositionToken;
            Assert.NotNull(codeposition);
            Assert.AreEqual(index, codeposition.TextIndex, "Tokenposition not correct");
            Assert.AreEqual(length, codeposition.TokenLength, "Tokenlength not correct");
        }

        [Test]
        public void FullScriptMethodLength() {
            IScript script = new ScriptParser().Parse("$log.info($\"I have some {$Parcel.gettype()} in my hands\")");
            ScriptMethod method = ((((script.Body as StatementBlock).Children.FirstOrDefault() as ScriptMethod).Parameters.First() as StringInterpolation).Children.Skip(1)
                .FirstOrDefault() as StatementBlock).Children.FirstOrDefault() as ScriptMethod;
            Assert.NotNull(method);
            Assert.AreEqual(33, method.TextIndex);
            Assert.AreEqual(9, method.TokenLength);
        }
    }
}