using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ListTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test]
        public void AddValues() {
            IScript script = parser.Parse(
                "$list=list" +
                "$list.add(1)" +
                "$list.add(7)" +
                "$list.add(9)" +
                "$list"
            );


            Assert.That(new[] {1, 7, 9}.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }
    }
}