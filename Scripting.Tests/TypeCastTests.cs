using System;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TypeCastTests {
        readonly IScriptParser parser=new ScriptParser();

        [Test, Parallelizable]
        public void Integer() {
            Assert.AreEqual(722, parser.Parse("int(\"722\")").Execute());
        }

        [Test, Parallelizable]
        public void Decimal() {
            Assert.AreEqual(722m, parser.Parse("decimal(\"722\")").Execute());
        }

        [Test, Parallelizable]
        public void String() {
            Assert.AreEqual("722", parser.Parse("string(722)").Execute());
        }
    }
}