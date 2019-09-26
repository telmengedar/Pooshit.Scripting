
using System.Threading;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

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

            Task execution = script.ExecuteAsync(tokensource.Token);

            tokensource.CancelAfter(1000);

            await execution.ContinueWith(t => { });
        }
    }
}