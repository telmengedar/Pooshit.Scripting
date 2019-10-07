using System.Collections;
using System.Linq;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;
using NightlyCode.Scripting.Providers;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class LambdaTests {

        public static IEnumerable Select(IEnumerable enumeration, LambdaMethod lambda) {
            return enumeration.Cast<object>().Select(i => lambda.Invoke(i));
        }

        [Test, Parallelizable]
        public void SelectValueUsingLambda() {
            IScriptParser parser=new ScriptParser();
            parser.Extensions.AddExtensions<LambdaTests>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$collection=[",
                "{ \"Name\":\"Three\", \"Value\":3},",
                "{ \"Name\":\"Five\", \"Value\":5},",
                "{ \"Name\":\"Seven\", \"Value\":7}",
                "]",
                "$values=$collection.select($item=>$item.Value)",
                "return($values.toarray())"
            ));

            Assert.That(new[]{3,5,7}.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }

        [Test, Parallelizable]
        public void SelectValueUsingLambdaBlock() {
            IScriptParser parser=new ScriptParser();
            parser.Extensions.AddExtensions<LambdaTests>();
            IScript script = parser.Parse(ScriptCode.Create(
                "$collection=[",
                "{ \"Name\":\"Three\", \"Value\":3},",
                "{ \"Name\":\"Five\", \"Value\":5},",
                "{ \"Name\":\"Seven\", \"Value\":7}",
                "]",
                "$values=$collection.select([$item]=>{$item.Value})",
                "return($values.toarray())"
            ));

            Assert.That(new[]{3,5,7}.SequenceEqual(script.Execute<IEnumerable>().Cast<int>()));
        }

    }
}