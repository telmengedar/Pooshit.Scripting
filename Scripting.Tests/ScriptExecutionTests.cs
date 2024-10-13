
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ScriptExecutionTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable, MaxTime(10000)]
        public async Task CancelAsyncScript() {
            IScript script = parser.Parse(ScriptCode.Create(
                "while(true)",
                "  wait(100)"
            ));

            CancellationTokenSource tokensource = new CancellationTokenSource();

            Task execution = script.ExecuteAsync((IVariableProvider)null, tokensource.Token);

            tokensource.CancelAfter(1000);

            await execution.ContinueWith(t => {
                Assert.That(t.IsCanceled);
            });
        }

        [Test, Parallelizable]
        public void VariablesArePropagatedCorrectly() {
            IScript script = parser.Parse(ScriptCode.Create(
                "if(false) {",
                "  for($i=0,$i<5,++$i) {",
                "    return($log)",
                "  }",
                "}",
                "else if(true) {",
                "  for($i=0,$i<5,++$i) {",
                "    return($log)",
                "  }",
                "}"
            ));

            Assert.AreEqual(42, script.Execute(new VariableProvider(new Variable("log", 42))));
        }
    }
}