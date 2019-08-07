using System;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class WaitTests {
        readonly IScriptParser parser = new ScriptParser();

        [TestCase("0")]
        [TestCase("100")]
        [TestCase("219.7d")]
        [TestCase("\"0:0:0.29\"")]
        [Parallelizable]
        public void ValidWaitStatement(string timeargument) {
            IScript script = parser.Parse($"wait({timeargument})");

            DateTime now = DateTime.Now;
            Assert.DoesNotThrow(() => script.Execute());
            TimeSpan executiontime = DateTime.Now - now;

            Assert.Greater(TimeSpan.FromSeconds(1), executiontime);
        }
    }
}