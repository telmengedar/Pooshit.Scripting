using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class SandboxEscapeTests {

        class HostBag {
            public StringBuilder Sb { get; } = new("host");
            public object Make() => new StringBuilder("graph");
            public T Echo<T>(T value) => value;
        }

        static IScriptParser NewParser() => new ScriptParser();

        static void AssertContained(string code, IVariableProvider variables = null) {
            Assert.That(() => {
                IScript script = NewParser().Parse(code);
                script.Execute(variables);
            }, Throws.InstanceOf<ScriptException>(), code);
        }

        static object TryExecute(string code, IVariableProvider variables = null) {
            try {
                return NewParser().Parse(code).Execute(variables);
            }
            catch {
                return null;
            }
        }

        static string Esc(string path) => path.Replace("\\", "/");

        static string PlantSecret(out string secret) {
            secret = "SECRET-" + Guid.NewGuid().ToString("N");
            string path = Path.Combine(Path.GetTempPath(), "poosec_" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, secret);
            return path;
        }

        static string ReserveMarker() {
            string path = Path.Combine(Path.GetTempPath(), "poomark_" + Guid.NewGuid().ToString("N") + ".txt");
            if (File.Exists(path))
                File.Delete(path);
            return path;
        }

        // ---------------------------------------------------------------------
        // CONTAINMENT BATTERY - hostile capability must be unreachable.
        // Every test here is EXPECTED TO FAIL on current master (the escape works)
        // and to pass only once raw System.Type genuinely never reaches script.
        // ---------------------------------------------------------------------

        [Parallelizable]
        [TestCase("(1).getType()")]
        [TestCase("typeof(1)")]
        [TestCase("\"hello\".getType()")]
        [TestCase("(1).getType().getType()")]
        [TestCase("typeof(typeof(1))")]
        [TestCase("int")]
        [Description("#7787 a live System.Type reached via any phrasing must not be a reflective handle")]
        public void Containment_ReachedTypeIsNotReflective(string typeExpr) {
            AssertContained($"{typeExpr}.getMethods()");
        }

        [Parallelizable]
        [TestCase("typeof(1).getMethods()")]
        [TestCase("typeof(1).getConstructors()")]
        [TestCase("typeof(1).getFields()")]
        [TestCase("typeof(1).getInterfaces()")]
        [TestCase("typeof(1).get_Assembly()")]
        [TestCase("typeof(1).assembly.fullName")]
        [Description("#7787 every reflective member of a reached Type must be refused")]
        public void Containment_ReflectiveMembersRefused(string expr) {
            AssertContained(expr);
        }

        [Parallelizable]
        [TestCase(344)]
        [TestCase(348)]
        [TestCase(376)]
        [Description("#7787 InvokeMember reflection must stay contained regardless of BindingFlags integer")]
        public void Containment_InvokeMember_VariedFlags(int flags) {
            AssertContained($"typeof(typeof(1)).invokeMember(\"GetType\", {flags}, null, null, [\"System.Environment\"]).invokeMember(\"get_UserName\", {flags}, null, null, [])");
        }

        [Parallelizable]
        [TestCase("System.Environment")]
        [TestCase("System.IO.File")]
        [TestCase("System.AppDomain")]
        [TestCase("System.Activator")]
        [Description("#7787 resolving an arbitrary non-registered type via reflection must be contained")]
        public void Containment_ArbitraryTypeResolution(string typename) {
            AssertContained($"typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"{typename}\"]).fullName");
        }

        [Test, Parallelizable]
        [Description("#7787 getType() on a returned host object graph must not yield a reflective handle")]
        public void Containment_ObjectGraphGetType() {
            AssertContained(
                "$h.make().getType().getMethods()",
                new VariableProvider(new Variable("h", new HostBag())));
        }

        [Parallelizable]
        [TestCase("System.Environment", "get_UserName")]
        [TestCase("System.Environment", "get_MachineName")]
        [TestCase("System.Environment", "get_UserDomainName")]
        [Description("#7787 host identity disclosure via reflected Environment members must be contained")]
        public void Containment_EnvironmentDisclosure(string typename, string member) {
            string code = $"typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"{typename}\"]).invokeMember(\"{member}\", 344, null, null, [])";
            object result = TryExecute(code);
            Assert.That(result, Is.Null.Or.Not.EqualTo(Environment.UserName)
                                       .And.Not.EqualTo(Environment.MachineName)
                                       .And.Not.EqualTo(Environment.UserDomainName),
                        "hostile script disclosed host identity");
        }

        [Test, Parallelizable]
        [Description("#7787 File.ReadAllText via reflection must not disclose file contents")]
        public void Containment_FileRead_ReadAllText() {
            string path = PlantSecret(out string secret);
            try {
                string code = $"typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"System.IO.File\"]).invokeMember(\"ReadAllText\", 344, null, null, [\"{Esc(path)}\"])";
                object result = TryExecute(code);
                Assert.That(result, Is.Not.EqualTo(secret), "hostile script disclosed file contents via ReadAllText");
            }
            finally {
                File.Delete(path);
            }
        }

        [Test, Parallelizable]
        [Description("#7787 a second reflective filesystem path (ReadAllBytes, assembly-qualified name) must not disclose contents")]
        public void Containment_FileRead_ReadAllBytes() {
            string path = PlantSecret(out string secret);
            try {
                string code = $"typeof(typeof(1)).invokeMember(\"GetType\", 376, null, null, [\"System.IO.File, System.Private.CoreLib\"]).invokeMember(\"ReadAllBytes\", 376, null, null, [\"{Esc(path)}\"])";
                object result = TryExecute(code);
                string disclosed = result is byte[] bytes ? Encoding.UTF8.GetString(bytes) : result?.ToString();
                Assert.That(disclosed, Is.Not.EqualTo(secret), "hostile script disclosed file contents via ReadAllBytes");
            }
            finally {
                File.Delete(path);
            }
        }

        [Test, Parallelizable]
        [Description("#7787 File.WriteAllText via reflection must not create the file (assert side effect, not just throw)")]
        public void Containment_FileWrite_WriteAllText() {
            string path = ReserveMarker();
            try {
                string code = $"typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"System.IO.File\"]).invokeMember(\"WriteAllText\", 344, null, null, [\"{Esc(path)}\", \"PWNED-BY-UNTRUSTED-SCRIPT\"])";
                TryExecute(code);
                Assert.That(File.Exists(path), Is.False, "hostile script wrote a file to disk");
            }
            finally {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test, Parallelizable]
        [Description("#7787 File.AppendAllText via reflection (second write method) must not create the file")]
        public void Containment_FileWrite_AppendAllText() {
            string path = ReserveMarker();
            try {
                string code = $"typeof(typeof(1)).invokeMember(\"GetType\", 348, null, null, [\"System.IO.File\"]).invokeMember(\"AppendAllText\", 348, null, null, [\"{Esc(path)}\", \"PWNED\"])";
                TryExecute(code);
                Assert.That(File.Exists(path), Is.False, "hostile script appended a file to disk");
            }
            finally {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test, Parallelizable]
        [Description("#7787 Process.Start via reflection must not spawn a child process (bounded, self-terminating probe)")]
        public void Containment_ProcessStart() {
            string marker = ReserveMarker();
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string fileName = windows ? "cmd" : "/bin/sh";
            string arguments = windows
                ? $"/c echo pwned> \"{marker}\""
                : $"-c \"echo pwned > '{marker}'\"";
            try {
                string code =
                    "$p = typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"System.Diagnostics.Process, System.Diagnostics.Process\"])" +
                    $".invokeMember(\"Start\", 344, null, null, [\"{Esc(fileName)}\", \"{arguments.Replace("\\", "/").Replace("\"", "\\\"")}\"])\n" +
                    "$p.waitForExit()";
                TryExecute(code);
                Assert.That(File.Exists(marker), Is.False, "hostile script spawned a child process");
            }
            finally {
                if (File.Exists(marker))
                    File.Delete(marker);
            }
        }

        [Test, Parallelizable]
        [Description("#7787 the cast/DetermineType door must not resolve a non-registered assembly-qualified type")]
        public void Containment_CastResolvesUnregisteredType() {
            AssertContained(
                "cast($sb, \"System.Text.StringBuilder, System.Private.CoreLib\")",
                new VariableProvider(new Variable("sb", new StringBuilder("x"))));
        }

        [Test, Parallelizable]
        [Description("#7787 Assembly.Load reachability via reflection must be contained")]
        public void Containment_AssemblyLoad() {
            AssertContained(
                "typeof(typeof(1)).invokeMember(\"GetType\", 344, null, null, [\"System.Reflection.Assembly\"]).invokeMember(\"Load\", 344, null, null, [\"System.Xml\"])");
        }

        // ---------------------------------------------------------------------
        // LEGITIMATE-BEHAVIOUR BATTERY - the type system a real host relies on.
        // Every test here is EXPECTED TO PASS on master and must STILL PASS after
        // the fix; a red here means the fix (or Sarah's design) over-corrected.
        // ---------------------------------------------------------------------

        [Test, Parallelizable]
        [Description("#7787 legitimate: new on a host-registered type must still work")]
        public void Legit_NewRegisteredType() {
            IScriptParser parser = NewParser();
            parser.Types.AddType<DateTime>("datetime");
            Assert.AreEqual(new DateTime(2012, 9, 4), parser.Parse("new datetime(2012,9,4)").Execute());
        }

        [Parallelizable]
        [TestCase("int(\"722\")", 722)]
        [TestCase("cast(5,\"int\")", 5)]
        [Description("#7787 legitimate: casts among registered primitive types must still work")]
        public void Legit_RegisteredPrimitiveCast(string code, int expected) {
            Assert.AreEqual(expected, NewParser().Parse(code).Execute());
        }

        [Test, Parallelizable]
        [Description("#7787 legitimate: generic call with an explicit type argument must still work")]
        public void Legit_GenericCall() {
            IScript script = NewParser().Parse("$h.echo<int>(7)");
            Assert.AreEqual(7, script.Execute(new VariableProvider(new Variable("h", new HostBag()))));
        }

        [Parallelizable]
        [TestCase("typeof(1) == int")]
        [TestCase("typeof(\"x\") == string")]
        [Description("#7787 legitimate: type-identity comparison must be preserved")]
        public void Legit_TypeIdentityComparison(string code) {
            Assert.AreEqual(true, NewParser().Parse(code).Execute());
        }

        [Test, Parallelizable]
        [Description("#7787 legitimate (design-conditional): reading a type's name where the design keeps it")]
        public void Legit_ReadTypeName() {
            Assert.AreEqual("Int32", NewParser().Parse("typeof(1).name").Execute());
        }
    }
}
