using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightlyCode.Scripting;
using Pooshit.Scripting;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Parser;

namespace NightlyCode.ScriptExecutor {
    class Program {
        static readonly ScriptParser parser = new ScriptParser {
            ImportProvider = new ImportProvider()
        };

        static void Main(string[] args) {
            if (!TryParseOptions(args, out ScriptLimits limits, out string[] remaining, out string optionError)) {
                Console.WriteLine(optionError);
                PrintUsage(Console.Out);
                return;
            }
            parser.Limits = limits;

            if (remaining.Length < 1) {
                PrintUsage(Console.Out);
                return;
            }

            string scriptfile = remaining[0];
            Logger logger = new Logger();

            string scripttext;
            try {
                scripttext = File.ReadAllText(scriptfile);
            }
            catch (Exception e) {
                logger.Error($"Unable to read scriptfile '{scriptfile}'", e);
                return;
            }

            IScript script;
            try {
                script=parser.Parse(scripttext);
            }
            catch (ScriptParserException parserexception) {
                logger.Error($"Parsing error on line {parserexception.Line}", parserexception);
                return;
            }
            catch (Exception e) {
                logger.Error($"Error parsing script", e);
                return;
            }

            try {
                script.Execute(new Dictionary<string, object> {
                    ["log"] = logger,
                    ["arguments"] = remaining
                });
            }
            catch (Exception e) {
                logger.Error($"Error executing script", e);
            }
        }

        internal static void PrintUsage(TextWriter writer) {
            writer.WriteLine("You need to specify the script file to execute");
            writer.WriteLine("Syntax: nc [options] <scriptfile> [args...]");
            writer.WriteLine("Options:");
            writer.WriteLine($"  --max-depth N            override MaxDepth (default {ScriptLimits.DefaultMaxDepth})");
            writer.WriteLine("  --max-steps N            override MaxSteps (default unset)");
            writer.WriteLine($"  --max-variable-bytes N   override MaxVariableBytes (default {ScriptLimits.DefaultMaxVariableBytes})");
            writer.WriteLine("  --timeout MS             override Timeout in milliseconds (default unset)");
            writer.WriteLine($"  --regex-timeout MS       override RegexTimeout in milliseconds (default {(int) ScriptLimits.DefaultRegexTimeout.TotalMilliseconds})");
            writer.WriteLine("  --unbounded              disable all execution guards (ScriptLimits.None)");
        }

        internal static bool TryParseOptions(string[] args, out ScriptLimits limits, out string[] remaining, out string error) {
            int? maxDepth = null;
            long? maxSteps = null;
            long? maxVariableBytes = null;
            int? timeoutMs = null;
            int? regexTimeoutMs = null;
            bool unbounded = false;

            int index = 0;
            while (index < args.Length && args[index].StartsWith("--")) {
                string flag = args[index];
                switch (flag) {
                    case "--unbounded":
                    case "--no-limits":
                        unbounded = true;
                        index++;
                        break;
                    case "--max-depth":
                        if (!TryReadInt(args, flag, ref index, out int depth, out error)) {
                            limits = null;
                            remaining = null;
                            return false;
                        }
                        maxDepth = depth;
                        break;
                    case "--max-steps":
                        if (!TryReadLong(args, flag, ref index, out long steps, out error)) {
                            limits = null;
                            remaining = null;
                            return false;
                        }
                        maxSteps = steps;
                        break;
                    case "--max-variable-bytes":
                        if (!TryReadLong(args, flag, ref index, out long bytes, out error)) {
                            limits = null;
                            remaining = null;
                            return false;
                        }
                        maxVariableBytes = bytes;
                        break;
                    case "--timeout":
                        if (!TryReadInt(args, flag, ref index, out int timeout, out error)) {
                            limits = null;
                            remaining = null;
                            return false;
                        }
                        timeoutMs = timeout;
                        break;
                    case "--regex-timeout":
                        if (!TryReadInt(args, flag, ref index, out int regexTimeout, out error)) {
                            limits = null;
                            remaining = null;
                            return false;
                        }
                        regexTimeoutMs = regexTimeout;
                        break;
                    default:
                        error = $"Unknown option '{flag}'";
                        limits = null;
                        remaining = null;
                        return false;
                }
            }

            remaining = args.Skip(index).ToArray();

            ScriptLimits baseLimits = unbounded ? ScriptLimits.None : ScriptLimits.Default;
            bool hasOverride = maxDepth.HasValue || maxSteps.HasValue || maxVariableBytes.HasValue || timeoutMs.HasValue || regexTimeoutMs.HasValue;
            limits = hasOverride
                ? new ScriptLimits {
                    MaxDepth = maxDepth ?? baseLimits.MaxDepth,
                    MaxParseDepth = baseLimits.MaxParseDepth,
                    MaxSteps = maxSteps ?? baseLimits.MaxSteps,
                    MaxVariableBytes = maxVariableBytes ?? baseLimits.MaxVariableBytes,
                    MaxVariables = baseLimits.MaxVariables,
                    Timeout = timeoutMs.HasValue ? TimeSpan.FromMilliseconds(timeoutMs.Value) : baseLimits.Timeout,
                    RegexTimeout = regexTimeoutMs.HasValue ? TimeSpan.FromMilliseconds(regexTimeoutMs.Value) : baseLimits.RegexTimeout
                }
                : baseLimits;
            error = null;
            return true;
        }

        static bool TryReadInt(string[] args, string flag, ref int index, out int value, out string error) {
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out value)) {
                error = $"Option '{flag}' requires a numeric value";
                value = 0;
                return false;
            }
            index += 2;
            error = null;
            return true;
        }

        static bool TryReadLong(string[] args, string flag, ref int index, out long value, out string error) {
            if (index + 1 >= args.Length || !long.TryParse(args[index + 1], out value)) {
                error = $"Option '{flag}' requires a numeric value";
                value = 0;
                return false;
            }
            index += 2;
            error = null;
            return true;
        }
    }
}
