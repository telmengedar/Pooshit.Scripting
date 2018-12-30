
using NightlyCode.Scripting;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class FlowControlTests {

        [Test, Parallelizable]
        public void TestIf() {
            IScriptToken script = new ScriptParser().Parse(
                "if(10>3)" +
                "  $result=5;" +
                "$result");
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void TestElse() {
            IScriptToken script = new ScriptParser().Parse(
                "if(3>10)" +
                "  $result=5;" +
                "else $result=3;" +
                "$result");
            Assert.AreEqual(3, script.Execute());
        }

        [Test, Parallelizable]
        public void TestFor() {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0;"+
                "for($i=0,$i<10,$i=$i+1)" +
                "  $result=$result+$i*10;" +
                "$result"
            );
            Assert.AreEqual(450, script.Execute());
        }

        [Test, Parallelizable]
        public void TestForeach() {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0;" +
                "foreach($i,[1,2,3,4,5,6,7,8,9])" +
                "  $result=$result+$i;" +
                "$result"
            );
            Assert.AreEqual(45, script.Execute());
        }

        [Test, Parallelizable]
        public void TestWhile() {
            IScriptToken script = new ScriptParser().Parse(
                "$result=2;" +
                "while($result<20)" +
                "  $result=$result*$result;" +
                "$result"
            );
            Assert.AreEqual(256, script.Execute());
        }

        [Test, Parallelizable]
        public void TestSwitch() {
            IScriptToken script = new ScriptParser().Parse(
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
        public void SwitchWithDefault()
        {
            IScriptToken script = new ScriptParser().Parse(
                "$condition=3;" +
                "switch($condition)" +
                "case(2)" +
                "  $result=0;" +
                "case(7)" +
                "  $result=9;" +
                "case(11)" +
                "  $result=2;" +
                "default"+
                "  $result=200;"+
                "$result"
            );

            Assert.AreEqual(200, script.Execute());
        }

        [Test, Parallelizable]
        public void SwitchMultipleCaseCondition()
        {
            IScriptToken script = new ScriptParser().Parse(
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
        public void IfWithStatementBlockTrue() {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0;" +
                "if(true) {" +
                "  $result=7;" +
                "  $result=$result+8;" +
                "}" +
                "$result;"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void IfWithStatementBlockFalse()
        {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0;" +
                "if(false) {" +
                "  $result=7;" +
                "  $result=$result+8;" +
                "}" +
                "$result;"
            );
            Assert.AreEqual(0, script.Execute());
        }

        [Test, Parallelizable]
        public void WhileWithNestedIf() {
            IScriptToken script=new ScriptParser().Parse(
                "$result=0;"+
                "while($result<100) {"+
                "  if($result&1==1) {"+
                "    $result=$result<<1;"+
                "  }"+
                "  else {"+
                "    $result=$result+3;"+
                "  }"+
                "}"+
                "$result;"
            );
            Assert.AreEqual(186, script.Execute());
        }

        [Test, Parallelizable]
        public void WhileWithNestedIfWithoutTerminators()
        {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0" +
                "while($result<100) {" +
                "  if($result&1==1) {" +
                "    $result=$result<<1" +
                "  }" +
                "  else {" +
                "    $result=$result+3" +
                "  }" +
                "}" +
                "$result"
            );
            Assert.AreEqual(186, script.Execute());
        }

        [Test, Parallelizable]
        public void Return() {
            IScriptToken script = new ScriptParser().Parse(
                "$result=0;" +
                "$result=15;" +
                "return $result;" +
                "$result=7;" +
                "$result"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void ReturnInInnerBlock()
        {
            IScriptToken script = new ScriptParser(new ExtensionProvider()).Parse(
                "$result=0;" +
                "foreach($i,[1,2,3,4,5,6,7,8,9]) {" +
                "  if($result>=10)"+
                "    return $result;"+
                "  $result=$result+$i;" +
                "}"+
                "$result"
            );
            Assert.AreEqual(10, script.Execute());
        }

    }
}