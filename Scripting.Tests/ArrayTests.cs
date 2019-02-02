using System;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ArrayTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void EmptyArray() {
            IScript script = parser.Parse("[]");
            Array array = script.Execute<Array>();
            Assert.NotNull(array);
            Assert.AreEqual(0, array.Length);
        }

        [Test, Parallelizable]
        public void EmptyArrayWithSpacesInDefinition() {
            IScript script = parser.Parse("[   ]");
            Array array = script.Execute<Array>();
            Assert.NotNull(array);
            Assert.AreEqual(0, array.Length);
        }

        [Test, Parallelizable]
        public void EmptyArrayWithLineBreakInDefinition()
        {
            IScript script = parser.Parse("[\n]");
            Array array = script.Execute<Array>();
            Assert.NotNull(array);
            Assert.AreEqual(0, array.Length);
        }
    }
}