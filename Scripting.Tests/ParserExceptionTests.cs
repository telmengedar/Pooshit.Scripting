using System;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ParserExceptionTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void UnterminatedForLoop() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("for(i="));
        }

        [Test, Parallelizable]
        public void ForWithoutParameters() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("for"));
        }

        [Test, Parallelizable]
        public void UnterminatedControlBlock() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("while(true){"));
        }

        [Test, Parallelizable]
        public void AssignmentToNonassignable() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("3=9"));
        }

        [Test, Parallelizable]
        public void EmptyArithmeticBlock() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("()"));
        }

        [Test, Parallelizable]
        public void EmptyLoopBlock() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("for($i=0,$i<5,++$i){\n}"));
        }

        [Test, Parallelizable]
        public void EmptyBlockAfterStatement() {
            try {
                parser.Parse("$i=0\nfor($i=0,$i<5,++$i){\n}");
                Assert.Fail("No exception thrown");
            }
            catch (ScriptParserException parserexception) {
                Assert.AreEqual("Empty statement block detected",parserexception.Message);
            }
            catch (Exception) {
                Assert.Fail("Wrong exception type");
            }
        }

        [Test, Parallelizable]
        public void UnterminatedString() {
            Assert.Throws<ScriptParserException>(() => parser.Parse("\"\"\"==\"\""));
        }

    }
}