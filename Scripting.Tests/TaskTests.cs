using System;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TaskTests {
        readonly IScriptParser parser = new ScriptParser();

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
    }
}