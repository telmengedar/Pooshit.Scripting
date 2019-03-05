
using Newtonsoft.Json.Serialization;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class FlowControlTests {
        readonly ScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void TestIf() {
            IScript script = parser.Parse(
                "if(10>3)" +
                "  $result=5;" +
                "$result");
            Assert.AreEqual(5, script.Execute());
        }

        [Test, Parallelizable]
        public void TestElse() {
            IScript script = parser.Parse(
                "if(3>10)" +
                "  $result=5;" +
                "else $result=3;" +
                "$result");
            Assert.AreEqual(3, script.Execute());
        }

        [Test, Parallelizable]
        public void TestFor() {
            IScript script = parser.Parse(
                "$result=0;"+
                "for($i=0,$i<10,++$i)" +
                "  $result=$result+$i*10;" +
                "$result"
            );
            Assert.AreEqual(450, script.Execute());
        }

        [Test, Parallelizable]
        public void SingleEmptyLoop() {
            IScript script = parser.Parse(
                "for($i=0,$i<10,++$i)"
            );
            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable]
        public void ForWithoutBody() {
            IScript script = parser.Parse(
                "$result=0\n" +
                "for($i=0,$i<10,++$i);\n" +
                "++$result"
            );
            Assert.AreEqual(1, script.Execute());
        }

        [Test, Parallelizable]
        public void ForWithCommentBeforeBody()
        {
            IScript script = parser.Parse(
                "$result=0\n" +
                "for($i=0,$i<10,++$i)\n" +
                "// increment result\n"+
                "++$result\n"+
                "$result"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [Test, Parallelizable]
        public void TestForeach() {
            IScript script = parser.Parse(
                "$result=0;" +
                "foreach($i,[1,2,3,4,5,6,7,8,9])" +
                "  $result=$result+$i;" +
                "$result"
            );
            Assert.AreEqual(45, script.Execute());
        }

        [Test, Parallelizable]
        public void TestWhile() {
            IScript script = parser.Parse(
                "$result=2;" +
                "while($result<20)" +
                "  $result=$result*$result;" +
                "$result"
            );
            Assert.AreEqual(256, script.Execute());
        }

        [Test, Parallelizable]
        public void TestSwitch() {
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
        public void SwitchWithDefault()
        {
            IScript script = parser.Parse(
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
        public void IfWithStatementBlockTrue() {
            IScript script = parser.Parse(
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
            IScript script = parser.Parse(
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
        public void ElseIf() {
            Assert.DoesNotThrow(() => parser.Parse(
                "$result=0\n" +
                "if($result>0)\n" +
                "  $result=1\n" +
                "else if($result<0)\n" +
                "  $result=-1\n" +
                "else $result=0\n" +
                "$result"
            ));
        }

        [Test, Parallelizable]
        public void WhileWithNestedIf() {
            IScript script= parser.Parse(
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
            IScript script = parser.Parse(
                "$result=0\n" +
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
            IScript script = parser.Parse(
                "$result=0;" +
                "$result=15;" +
                "return($result);" +
                "$result=7;" +
                "$result"
            );
            Assert.AreEqual(15, script.Execute());
        }

        [Test, Parallelizable]
        public void ReturnVoid()
        {
            IScript script = parser.Parse(
                "$result=0\n" +
                "$result=15\n" +
                "return\n" +
                "$result=7\n" +
                "$result"
            );
            Assert.AreEqual(null, script.Execute());
        }

        [Test, Parallelizable]
        public void ReturnInInnerBlock()
        {
            IScript script = parser.Parse(
                "$result=0;" +
                "foreach($i,[1,2,3,4,5,6,7,8,9]) {" +
                "  if($result>=10)"+
                "    return($result)\n"+
                "  $result=$result+$i;" +
                "}"+
                "$result"
            );
            Assert.AreEqual(10, script.Execute());
        }

        [TestCase("throw(\"message\")")]
        [TestCase("throw(\"message\", \"data\")")]
        [Parallelizable]
        public void ThrowException(string data) {
            Assert.Throws<ScriptExecutionException>(() => parser.Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void BreakWhile() {
            IScript script = parser.Parse(
                "$result=0\n" +
                "while($result<16384) {" +
                "  ++$result\n" +
                "  if($result>=8)\n" +
                "    break\n" +
                "}" +
                "$result"
            );
            Assert.AreEqual(8, script.Execute());
        }

        [Test, Parallelizable]
        public void MultiBreak() {
            IScript script = parser.Parse(
                "$result=0\n" +
                "while(true){\n" +
                "  while($result<128){\n" +
                "    ++$result\n" +
                "    if($result>=8)\n" +
                "      break(2)\n" +
                "  }\n" +
                "  throw(\"boo\")\n" +
                "}\n" +
                "$result"
            );
            Assert.AreEqual(8, script.Execute());
        }

        [Test, Parallelizable]
        public void BreakForeach() {
            IScript script = parser.Parse(
                "$result=0;" +
                "foreach($i,[1,2,3,4,5,6,7,8,9]) {" +
                "  if($i==5)" +
                "    break;" +
                "  $result=$i;" +
                "}" +
                "$result;"
            );
            Assert.AreEqual(4, script.Execute());
        }

        [Test, Parallelizable]
        public void BreakFor() {
            IScript script = parser.Parse(
                "$result=0;" +
                "for($i=0,$i<64,++$i) {" +
                "  if($i==10)"+
                "    break;"+
                "  $result=$i;" +
                "}"+
                "$result;"
            );
            Assert.AreEqual(9, script.Execute());
        }

        [Test, Parallelizable]
        public void ContinueFor() {
            IScript script = parser.Parse(
                "$result=0\n"+
                "for($i=0,$i<10,++$i) {"+
                "  if($i&1==1) continue\n"+
                "  $result+=$i\n"+
                "}"+
                "$result"
            );
            Assert.AreEqual(20, script.Execute());
        }

        [Test, Parallelizable]
        public void MultiContinueFor() {
            IScript script = parser.Parse(
                "$result=0\n" +
                "for($i=0,$i<4,++$i) {" +
                "  for($k=0,$k<4,++$k) {"+
                "    if($i&1==1 && $k>2) continue(2)\n" +
                "    $result+=$k\n" +
                "  }"+
                "  ++$result\n"+
                "}" +
                "$result"
            );
            Assert.AreEqual(20, script.Execute());
        }

        [Test, Parallelizable]
        public void TryWithoutCatch() {
            IScript script = parser.Parse("try throw(\"error\") return(0)");
            Assert.AreEqual(0, script.Execute());
        }

        [Test, Parallelizable]
        public void TryWithCatch() {
            IScript script = parser.Parse("try throw(\"error\") catch return($exception.message) return(0)");
            Assert.AreEqual("error", script.Execute());
        }
    }
}