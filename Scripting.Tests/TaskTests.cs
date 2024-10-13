﻿using System;
using System.Threading.Tasks;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Hosts;
using Pooshit.Scripting.Parser;

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
                "$t=task.run([]=>{$result=5})",
                "$t.wait()",
                "$result"
            ));

            Assert.AreEqual(5, script.Execute(new VariableProvider(new Variable("task", new TaskHost()))));
        }

        [Test, Parallelizable]
        public void ExecuteMultipleTasksInParallel() {

            IScript script = parser.Parse(ScriptCode.Create(
                "$result=0",
                "$tasks=new list()",
                "for($i=0,$i<5,++$i){",
                "  $tasks.add(task.run([]=>{",
                "    for($k=0,$k<150,++$k)",
                "      ++$result",
                "  }))",
                "}",
                "task.waitall($tasks.toarray())",
                "$result"
            ));
            int result = script.Execute<int>(new VariableProvider(new Variable("task", new TaskHost())));
            Assert.Less(100, result);
        }

        [Test, Parallelizable]
        public void AwaitTask() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$t=task.run([]=>{",
                "  3+8",
                "})",
                "await($t)"
            ));
            int result = script.Execute<int>(new VariableProvider(new Variable("task", new TaskHost())));
            Assert.AreEqual(11, result);
        }

        [Test, Parallelizable]
        public void AwaitGetsInnerException() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$t=task.run([]=>{",
                "  throw(\"Bullshit\")",
                "})",
                "$result=await($t)"
            ));
            try {
                script.Execute(new VariableProvider(new Variable("task", new TaskHost())));
                Assert.Fail("No Exception was thrown");
            }
            catch(Exception e) {
                Assert.AreEqual("Bullshit", e.Message);
            }
        }

        [Test, Parallelizable]
        public void AwaitMethodGetsInnerException() {
            IScript script = parser.Parse("$result=await(this.taskmethod())");
            try {
                script.Execute(new VariableProvider(new Variable("this", this)));
                Assert.Fail("No Exception was thrown");
            }
            catch(Exception e) {
                Assert.AreEqual("Unable to execute 'await($this.taskmethod())'\nBullshit", e.Message);
            }
        }

    }
}