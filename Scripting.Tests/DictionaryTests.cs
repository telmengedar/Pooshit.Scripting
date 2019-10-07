using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class DictionaryTests {
        readonly ScriptParser parser = new ScriptParser();

        [OneTimeSetUp]
        public void SetUp() {
            parser.Types.AddType<List<object>>("list");
        }

        [Test, Parallelizable]
        public void InitializeDictionary() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$dic={",
                "  \"value\": 70",
                "}",
                "return($dic[\"value\"])"
            ));
            Assert.AreEqual(70, script.Execute<int>());
        }

        [Test, Parallelizable]
        public void VariableAsValue() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value=70",
                "$dic={",
                "  \"value\": $value",
                "}",
                "return($dic[\"value\"])"
            ));
            Assert.AreEqual(70, script.Execute<int>());
        }

        [Test, Parallelizable]
        public void ReturnDictionary() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$dic={",
                "  \"value\": 70",
                "}"
            ));

            Dictionary<object, object> result = script.Execute<Dictionary<object, object>>();
            Assert.AreEqual(70, result["value"]);
        }

        [Test, Parallelizable]
        public void DictionaryInArray() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$dic=[{",
                "  \"value\": 70",
                "},{" +
                "  \"value\": 42",
                "}]",
                "return($dic[1])"
            ));

            Dictionary<object, object> result = script.Execute<Dictionary<object, object>>();
            Assert.AreEqual(42, result["value"]);
        }

        [Test, Parallelizable]
        public void MultipleValuesInDictionary() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$dic={",
                "  \"value\": 70,",
                "  \"number\": 60",
                "}"
            ));

            Dictionary<object, object> result = script.Execute<Dictionary<object, object>>();
            Assert.AreEqual(60, result["number"]);
        }

        [Test, Parallelizable]
        public void DictionaryAsMethodArgument() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$result=new list()",
                "$result.add({",
                "  \"value\": 70,",
                "  \"number\": 60",
                "})",
                "return ($result)"
            ));

            List<object> result = script.Execute<List<object>>();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(60, (result[0] as Dictionary<object, object>)["number"]);
        }

        [Test, Parallelizable]
        public void DictionaryAsMethodArgumentInBlock() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$result=new list()",
                "for($i=0,$i<5,++$i)",
                "{",
                "  $result.add({",
                "    \"value\": 70,",
                "    \"number\": 60",
                "  })",
                "}",
                "return ($result)"
            ));

            List<object> result = script.Execute<List<object>>();
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(60, (result[0] as Dictionary<object, object>)["number"]);
        }

        [Test, Parallelizable]
        public void DictionaryInSwitch() {
            IScript script = parser.Parse(ScriptCode.Create(
                "switch(1)",
                "case(1)",
                "{",
                "  return({",
                "    \"value\": 70,",
                "    \"number\": 60",
                "  })",
                "}"
            ));

            Dictionary<object,object> result = script.Execute<Dictionary<object, object>>();
            Assert.NotNull(result);
            Assert.AreEqual(60, result["number"]);
        }

        [Test, Parallelizable]
        public void ReadEntryAsProperty() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value = {",
                "  \"value\": 70,",
                "  \"number\": 60",
                "}",
                "return($value.number)"
            ));


            Assert.AreEqual(60, script.Execute());
        }

        [Test, Parallelizable]
        public void SetEntryAsProperty() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value = {}",
                "$value.number=60",
                "return($value.number)"
            ));


            Assert.AreEqual(60, script.Execute());
        }

        [Test, Parallelizable]
        public void ReadCasedEntryAsProperty() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$value = {",
                "  \"Number\" : 60",
                "}",
                "return($value.Number)"
            ));


            Assert.AreEqual(60, script.Execute());
        }

    }
}