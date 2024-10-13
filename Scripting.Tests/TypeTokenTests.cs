using System;
using NUnit.Framework;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {
    
    [TestFixture, Parallelizable]
    public class TypeTokenTests {
        readonly IScriptParser parser = new ScriptParser();
        
        [Test, Parallelizable]
        public void ParseType() {

            Type type = parser.Parse("int").Execute<Type>();
            Assert.AreEqual(typeof(int), type);
        }
        
        [Test, Parallelizable]
        public void ParseTypeArray() {

            Type type = parser.Parse("int[]").Execute<Type>();
            Assert.AreEqual(typeof(int[]), type);
        }
        
        [Test, Parallelizable]
        public void ParseTypeArrayWithTrailingSpace() {

            Type type = parser.Parse("int[] ").Execute<Type>();
            Assert.AreEqual(typeof(int[]), type);
        }
    }
}