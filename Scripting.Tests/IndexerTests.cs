using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class IndexerTests {

        [Test, Parallelizable]
        public void DictionaryAccess() {
            IScriptParser parser = new ScriptParser(new Variable("dic", new Dictionary<string, string[]>()));
            IScript script = parser.Parse(
                "$k=\"key\"\n" +
                "dic[\"key\"]=[\"value1\",\"value2\",\"value3\"]\n" +
                "$result=\"\"\n" +
                "foreach($v,dic[$k])\n" +
                "$result+=$v+\",\"\n" +
                "$result"
            );

            Assert.AreEqual("value1,value2,value3,", script.Execute());
        }
    }
}