using System;
using System.Collections.Generic;
using System.IO;
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
            if (args.Length<1) {
                Console.WriteLine("You need to specify the script file to execute");
                Console.WriteLine("Syntax: nc <scriptfile> [args...]");
                return;
            }

            string scriptfile = args[0];
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
                    ["arguments"] = args
                });
            }
            catch (Exception e) {
                logger.Error($"Error executing script", e);
            }
        }
    }
}