using System;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Parser.Resolvers;
using NUnit.Framework;
using Scripting.Tests.Data;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class MethodResolverTests {

        public int SomeWeirdMethod(int first, int second, object third, string fourth) {
            return first + second;
        }

        public TimeSpan SomeWeirdMethod(string first, object second, DateTime third, DateTime fourth) {
            return fourth - third;
        }

        [Test, Parallelizable]
        public void CachingMethodsHasSomeEffect() {
            IScriptParser parser = new ScriptParser(new Variable("host", this));
            ((MethodResolver) ((ScriptParser) parser).MethodCallResolver).EnableCaching = false;
            IScript script1 = parser.Parse("$host.someweirdmethod(122,111,null,\"weird\")");
            IScript script2 = parser.Parse("$host.someweirdmethod(\"weird\",null,\"2019-10-02\",\"2019-10-03\")");
            TimeSpan expected = new TimeSpan(1, 0, 0, 0);

            DateTime start = DateTime.Now;
            for (int i = 0; i < 1024; ++i) {
                Assert.AreEqual(233, script1.Execute());
                Assert.AreEqual(expected, script2.Execute());
            }
            TimeSpan withoutcache = DateTime.Now - start;
            Console.WriteLine($"Without cache: {withoutcache}");

            ((MethodResolver)((ScriptParser)parser).MethodCallResolver).EnableCaching = true;

            start = DateTime.Now;
            for (int i = 0; i < 1024; ++i)
            {
                Assert.AreEqual(233, script1.Execute());
                Assert.AreEqual(expected, script2.Execute());
            }
            TimeSpan withcache = DateTime.Now - start;
            Console.WriteLine($"With cache: {withcache}");

            Assert.Less(withcache, withoutcache, "Caching has no effect");

        }

        [Test, Parallelizable]
        public void CachingConstructorsHasSomeEffect()
        {
            IScriptParser parser = new ScriptParser();
            parser.Types.AddType<ComplexType>();
            ((MethodResolver)((ScriptParser)parser).MethodCallResolver).EnableCaching = false;
            IScript script = parser.Parse("$value=new complextype({\"name\":\"name\",\"value\":\"3\"}, 7)");

            DateTime start = DateTime.Now;
            for (int i = 0; i < 1024; ++i) {
                script.Execute();
            }
            TimeSpan withoutcache = DateTime.Now - start;
            Console.WriteLine($"Without cache: {withoutcache}");

            ((MethodResolver)((ScriptParser)parser).MethodCallResolver).EnableCaching = true;

            start = DateTime.Now;
            for (int i = 0; i < 1024; ++i)
            {
                script.Execute();
            }
            TimeSpan withcache = DateTime.Now - start;
            Console.WriteLine($"With cache: {withcache}");

            Assert.Less(withcache, withoutcache, "Caching has no effect");

        }

    }
}