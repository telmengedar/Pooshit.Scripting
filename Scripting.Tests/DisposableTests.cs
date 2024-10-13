using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class DisposableTests {
        IScriptParser parser;


        [SetUp]
        public void Setup() {
            parser = new ScriptParser();
            parser.Types.AddType<Disposable>();
        }

        [Test, Parallelizable]
        public void DisposeSingle() {
            IScript script = parser.Parse(
                "$data=new disposable()\n" +
                "using($data)\n" +
                "\"weird statement\"\n" +
                "$data.disposed"
            );

            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void DisposeBlock() {
            IScript script = parser.Parse(
                "$data=new disposable()\n" +
                "using($data) {\n" +
                "\"weird statement\"\n" + 
                "\"another statement\"\n" +
                "}\n" +
                "$data.disposed"
            );
            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void UseMultiple() {
            IScript script = parser.Parse(
                "$data1=new disposable()\n" +
                "$data2=new disposable()\n" +
                "using($data1,$data2) {\n" +
                "\"weird statement\"\n" +
                "\"another statement\"\n" +
                "}\n" +
                "$data1.disposed&&$data2.disposed"
            );
            Assert.AreEqual(true, script.Execute());
        }
    }
}