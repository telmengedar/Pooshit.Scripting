using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Extensions.Script;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class IndexerTests {

        [Test, Parallelizable]
        public void DictionaryAccess() {
            IScriptParser parser = new ScriptParser();
            IScript script = parser.Parse(
                "$k=\"key\"\n" +
                "dic[\"key\"]=[\"value1\",\"value2\",\"value3\"]\n" +
                "$result=\"\"\n" +
                "foreach($v,dic[$k])\n" +
                "$result+=$v+\",\"\n" +
                "$result"
            );

            Assert.AreEqual("value1,value2,value3,", script.Execute(new VariableProvider(new Variable("dic", new Dictionary<string, string[]>()))));
        }

        [Test, Parallelizable]
        public void IndexerToOrderedArray() {
            IScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse(ScriptCode.Create(
                "$var1=6",
                "$var2=8",
                "$var3=1",
                "$orderedarray=[$var1,$var2,$var3].order()",
                "$o1=$orderedarray[0]",
                "$o2=$orderedarray[1]",
                "$o3=$orderedarray[2]"
            ));

            Assert.DoesNotThrow(() => script.Execute());
        }

        [Test, Parallelizable]
        public void CallGenericExtensionMethod() {
            IScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            IScript script = parser.Parse(ScriptCode.Create(
                "$input",
                "$firstelement=$input.farstordefault()",
                "$firstelement"
            ));

            Assert.AreEqual(5, script.Execute(new VariableProvider(new Variable("input", new[] { 5, 9, 1 }))));
        }

        [Test, Parallelizable]
        public void PreferGenericExtension() {
            IScriptParser parser = new ScriptParser();
            parser.Extensions.AddExtensions<EnumerableExtensions>();

            // the generic extension actually returns the last instead of the first
            IScript script = parser.Parse(ScriptCode.Create(
                "$input",
                "$firstelement=$input.first()",
                "$firstelement"
            ));

            Assert.AreEqual(1, script.Execute(new VariableProvider(new Variable("input", new[] { 5, 9, 1 }))));
        }

    }
}