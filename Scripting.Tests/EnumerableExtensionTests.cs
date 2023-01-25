using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions.Script;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {
    
    [TestFixture, Parallelizable]
    public class EnumerableExtensionTests {

        [Test]
        public void IndexOf() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.indexof(7))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(5, result);
        }

        [Test]
        public void IndexOfNotFound() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.indexof(8))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void LastIndexOf() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.lastindexof(7))");
            int result = script.Execute<int>(new Dictionary<string, object>() {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(9, result);
        }
        
        [Test]
        public void LastIndexOfNotFound() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.lastindexof(8))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(-1, result);
        }
        
        [Test]
        public void IndexOfPredicate() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.indexof(i=>i==7))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(5, result);
        }

        [Test]
        public void IndexOfStringPredicate() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($data.indexof(i=>i=='s'))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["data"] = "andopsgendet"
            });
            Assert.AreEqual(5, result);
        }

        [Test]
        public void IndexOfPredicateNotFound() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.indexof(i=>i==8))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void LastIndexOfPredicate() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.lastindexof(i=>i==7))");
            int result = script.Execute<int>(new Dictionary<string, object>() {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(9, result);
        }
        
        [Test]
        public void LastIndexOfPredicateNotFound() {
            ScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse("return($array.lastindexof(i=>i==8))");
            int result = script.Execute<int>(new Dictionary<string, object> {
                ["array"] = new[]{1, 9, 4, 4, 2, 7, 2, 1, 9, 7, 2}
            });
            Assert.AreEqual(-1, result);
        }
    }
}