using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class DisposableTests {

        static IScriptParser CreateParser() {
            IScriptParser parser = new ScriptParser();
            parser.Types.AddType<Disposable>();
            return parser;
        }

        [Test, Parallelizable]
        public void DisposeSingle() {
            IScript script = CreateParser().Parse(
                "$data=new disposable()\n" +
                "using($data)\n" +
                "\"weird statement\"\n" +
                "$data.disposed"
            );

            Assert.AreEqual(true, script.Execute());
        }

        [Test, Parallelizable]
        public void DisposeBlock() {
            IScript script = CreateParser().Parse(
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
            IScript script = CreateParser().Parse(
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