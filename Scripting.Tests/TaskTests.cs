using System;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TaskTests {
        readonly IScriptParser parser = new ScriptParser();

        public Task TaskMethod() {
            return Task.Run(() => throw new Exception("Bullshit"));
        }

        [Test, Parallelizable]
        public void ExecuteExpressionInTask() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$result=0",
                "$t=new task($result=5)",
                "$t.start()",
                "$t.wait()",
                "$result"
            ));

            Assert.AreEqual(5, script.Execute());
        }
        
        [Test, Parallelizable]
        public void ExecuteMultipleTasksInParallel() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$result=0",
                "$tasks=new list()",
                "for($i=0,$i<5,++$i){",
                "  $tasks.add(new task({",
                "    for($k=0,$k<150,++$k)",
                "      ++$result",
                "  }))",
                "}",
                "foreach($t,$tasks)",
                "  $t.start()",
                "task.waitall($tasks.toarray())",
                "$result"
            ));
            int result = script.Execute<int>();
            Assert.Less(100, result);
        }

        [Test, Parallelizable]
        public void AwaitTask() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$t=new task({",
                "  3+8",
                "})",
                "await($t)"
            ));
            int result = script.Execute<int>();
            Assert.AreEqual(11, result);
        }

        [Test, Parallelizable]
        public void AwaitGetsInnerException() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$t=new task({",
                "  throw(\"Bullshit\")",
                "})",
                "$result=await($t)"
            ));
            try {
                script.Execute();
                Assert.Fail("No Exception was thrown");
            }
            catch (Exception e) {
                Assert.AreEqual("Bullshit", e.Message);
            }
        }

        [Test, Parallelizable]
        public void AwaitMethodGetsInnerException() {
            IScript script = parser.Parse("$result=await(this.taskmethod())", new Variable("this", this));
            try {
                script.Execute();
                Assert.Fail("No Exception was thrown");
            }
            catch (Exception e) {
                Assert.AreEqual("Unable to execute assignment '$result'\nBullshit", e.Message);
            }
        }

    }
}