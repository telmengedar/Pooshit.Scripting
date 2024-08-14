using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class SwitchTests {
        readonly ScriptParser parser = new ScriptParser();

        enum TestEnum {
            Test,
            Loig,
            Bimmel
        }

        [Test, Parallelizable]
        public void Int() {
            IScript script = parser.Parse(
                "$condition=7;" +
                "switch($condition)" +
                "case(2)" +
                "  $result=0;" +
                "case(7)" +
                "  $result=9;" +
                "case(11)" +
                "  $result=2;" +
                "$result"
            );

            Assert.AreEqual(9, script.Execute());
        }

        [Test, Parallelizable]
        public void WithDefault() {
            IScript script = parser.Parse(
                "$condition=3;" +
                "switch($condition)" +
                "case(2)" +
                "  $result=0;" +
                "case(7)" +
                "  $result=9;" +
                "case(11)" +
                "  $result=2;" +
                "default" +
                "  $result=200;" +
                "$result"
            );

            Assert.AreEqual(200, script.Execute());
        }

        [Test, Parallelizable]
        public void MultipleCaseCondition() {
            IScript script = parser.Parse(
                "$condition=11;" +
                "switch($condition)" +
                "case(2,7,11)" +
                "  $result=32;" +
                "default" +
                "  $result=200;" +
                "$result"
            );

            Assert.AreEqual(32, script.Execute());
        }

        [Test, Parallelizable]
        public void EnumByName() {
            IScript script = parser.Parse(ScriptCode.Create(
                "$variable=0",
                "switch($condition)",
                "case(\"Bimmel\")",
                "  $variable=1",
                "return($variable)"
            ));
            int result = script.Execute<int>(new VariableProvider(new Variable("condition", TestEnum.Bimmel)));
            Assert.AreEqual(1, result);
        }
        
        [Test, Parallelizable]
        public void SwitchOneliner() {
            IScript script = parser.Parse("switch(\"Vollzeit\") case(\"Vollzeit\") \"Fulltime\" case(\"Teilzeit\") \"Parttime\" default \"Unknown\"");
            string result = script.Execute<string>();
            Assert.AreEqual("Fulltime", result);
        }
    }
}