using System.Collections;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ListTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void AddValues() {
            IScript script = parser.Parse(
                "$list=new list()" +
                "$list.add(1)" +
                "$list.add(7)" +
                "$list.add(9)" +
                "$list"
            );


            Assert.That(new[] {1, 7, 9}.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }

        [Test, Parallelizable]
        public void ListWithArrayParameter() {
            IScript script = parser.Parse(
                "$list=new list([1,7,9])"+
                "$list"
            );

            Assert.That(new[] { 1, 7, 9 }.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }

        [Test, Parallelizable]
        public void ListWithCapacityParameter() {
            IScript script = parser.Parse(
                "$list=new list(64)" +
                "$list.capacity"
            );
            Assert.AreEqual(64, script.Execute());
        }
    }
}