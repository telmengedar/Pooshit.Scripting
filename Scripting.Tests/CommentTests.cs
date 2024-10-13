using NightlyCode.Scripting;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class CommentTests {
        readonly IScriptParser parser = new ScriptParser();

        [Test, Parallelizable]
        public void SingleLineComment() {
            IScript script = parser.Parse(
                "// this is a comment\n" +
                "$result=4"
            );
            Assert.AreEqual(4, script.Execute());
        }

        [Test, Parallelizable]
        public void MultiLineComment() {
            IScript script = parser.Parse(
                "$result=0\n"+
                "/* multiple lines of words\n" +
                "$result=8\n"+
                "   which should ignore the line in between*/\n"+
                "$result+=4"
            );
            Assert.AreEqual(4, script.Execute());
        }

        [Test, Parallelizable]
        public void CommentAtEndOfBlock() {
            Assert.DoesNotThrow(() => {
                parser.Parse(ScriptCode.Create(
                    "$count=5",
                    "while(count-->0){",
                    "  if(true) {",
                    "    $result=0",
                    "  }",
                    "  // comment at end of block",
                    "}"
                ));
            });
        }

        [Test, Parallelizable]
        public void MixedMultiLineAndLineComments() {
            Assert.DoesNotThrow(() => {
                parser.Parse(ScriptCode.Create(
                    "/* ----",
                    "// bla bla bla ",
                    "// bla bla bla ",
                    "*/"
                ));
            });
        }
    }
}