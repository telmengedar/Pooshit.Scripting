using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {
    
    [TestFixture, Parallelizable]
    public class ReturnTests {
        
        [Test, Parallelizable]
        public void ReturnLastExpression() {
            ScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(ScriptCode.Create(
                "$i=2 i=int(2) {\"value\": 70}"
            ));

            Dictionary<object, object> result = script.Execute<Dictionary<object, object>>();
            Assert.AreEqual(70, result["value"]);
        }

    }
}