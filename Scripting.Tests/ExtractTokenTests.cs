
using System;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Parser.Extract;
using NightlyCode.Scripting.Tokens;
using NUnit.Framework;

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
        [TestCase("host(5", 6, nameof(ScriptMethod),"$host.invoke(5)","$host.invoke(5)")]
        [TestCase("host(\"for\",", 10, nameof(ScriptMethod),"$host.invoke(\"for\")","$host.invoke(\"for\")")]
        [TestCase("host.boll(\"for\",", 11, nameof(ScriptMethod),"$host.boll(\"\")","$host.boll(\"for\")")]
        [TestCase("host.boll(\"for\",7,", 13, nameof(ScriptMethod),"$host.boll(\"fo\")","$host.boll(\"for\")")]
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
    }
}