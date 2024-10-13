using System.Collections.Generic;
using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;

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