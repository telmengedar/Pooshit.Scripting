using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using Pooshit.Scripting;

namespace Scripting.Tests {

    /// <summary>
    /// runs the built ScriptExecutor CLI (DiVoid #7887; #114 ruling 2026-08-08 forbids widening accessibility
    /// to reach internals for testing) as a real process and asserts on its exit code and console output -
    /// the surface a user of the CLI actually meets, covering argument parsing, wiring and guard firing end to end
    /// </summary>
    [TestFixture, Parallelizable]
    public class ScriptExecutorSmokeTests {

        const int ProcessTimeoutMilliseconds = 15000;

        static string NestedParens(int depth) => new string('(', depth) + "1" + new string(')', depth);

        [Test, Parallelizable]
        [Description("DiVoid #7887: --timeout is an unrelated flag and must not silently disable the parse-depth guard - a 150-deep script must be refused with the guard message and a non-zero exit code, not run to completion.")]
        public void UnrelatedFlag_DeepNestedScript_RefusedByParseDepthGuard() {
            string scriptPath = WriteTempScript(NestedParens(150) + ";");
            try {
                if (!TryRunScriptExecutor(new[] {"--timeout", "5000", scriptPath}, out (int ExitCode, string StdOut, string StdErr) result)) {
                    Assert.Ignore("ScriptExecutor build output not found - run 'dotnet build -c Release' before this smoke test.");
                    return;
                }

                Assert.That(result.ExitCode, Is.Not.EqualTo(0), $"stdout:\n{result.StdOut}\nstderr:\n{result.StdErr}");
                Assert.That(result.StdOut, Does.Contain("Parser exceeded the configured nesting depth limit of 100"));
            }
            finally {
                File.Delete(scriptPath);
            }
        }

        [Test, Parallelizable]
        [Description("DiVoid #7887: --regex-timeout usage text must state the actual configured default (1000ms since PR #18), not the stale '(default unset)'.")]
        public void NoArguments_UsageOutput_StatesConfiguredRegexTimeoutDefault() {
            if (!TryRunScriptExecutor(Array.Empty<string>(), out (int ExitCode, string StdOut, string StdErr) result)) {
                Assert.Ignore("ScriptExecutor build output not found - run 'dotnet build -c Release' before this smoke test.");
                return;
            }

            Assert.That(result.StdOut, Does.Contain($"override RegexTimeout in milliseconds (default {(int) ScriptLimits.DefaultRegexTimeout.TotalMilliseconds})"));
            Assert.That(result.StdOut, Does.Not.Contain("RegexTimeout in milliseconds (default unset)"));
        }

        static string WriteTempScript(string contents) {
            string path = Path.Combine(Path.GetTempPath(), $"scriptexecutor-smoke-{Guid.NewGuid():N}.ns");
            File.WriteAllText(path, contents);
            return path;
        }

        static bool TryRunScriptExecutor(string[] arguments, out (int ExitCode, string StdOut, string StdErr) result) {
            string executorDirectory = FindScriptExecutorOutputDirectory();
            string exePath = executorDirectory == null ? null : Path.Combine(executorDirectory, "NC.exe");
            string dllPath = executorDirectory == null ? null : Path.Combine(executorDirectory, "NC.dll");

            ProcessStartInfo startInfo;
            if (exePath != null && File.Exists(exePath)) {
                startInfo = new ProcessStartInfo(exePath);
            }
            else if (dllPath != null && File.Exists(dllPath)) {
                startInfo = new ProcessStartInfo("dotnet");
                startInfo.ArgumentList.Add(dllPath);
            }
            else {
                result = default;
                return false;
            }

            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            StringBuilder stdout = new();
            StringBuilder stderr = new();

            using Process process = new() {StartInfo = startInfo, EnableRaisingEvents = true};
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(ProcessTimeoutMilliseconds)) {
                process.Kill(true);
                Assert.Fail($"ScriptExecutor did not exit within {ProcessTimeoutMilliseconds}ms - killed to avoid hanging the test run.");
            }
            process.WaitForExit();

            result = (process.ExitCode, stdout.ToString(), stderr.ToString());
            return true;
        }

        static string FindScriptExecutorOutputDirectory() {
            DirectoryInfo testOutputDirectory = new(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            DirectoryInfo configurationDirectory = testOutputDirectory.Parent;
            DirectoryInfo repositoryRoot = configurationDirectory?.Parent?.Parent?.Parent;
            if (repositoryRoot == null)
                return null;

            return Path.Combine(repositoryRoot.FullName, "ScriptExecutor", "bin", configurationDirectory.Name, testOutputDirectory.Name);
        }
    }
}
