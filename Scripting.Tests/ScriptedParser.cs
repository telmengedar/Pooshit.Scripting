using System;
using System.Threading;
using System.Threading.Tasks;
using Pooshit.Scripting;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Providers;

namespace Scripting.Tests {

    /// <summary>
    /// fake parser that throws on the prefix length <see cref="ThrowAt"/> and hangs on the prefix length
    /// <see cref="HangAt"/>, otherwise returns immediately
    /// </summary>
    class ScriptedParser : IScriptParser {
        public int ThrowAt = -1;
        public int HangAt = -1;

        public IExtensionProvider Extensions => throw new NotImplementedException();
        public ITypeProvider Types => throw new NotImplementedException();
        public IImportProvider ImportProvider { get; set; }

        public IScript Parse(string data) {
            if (data.Length == ThrowAt)
                throw new InvalidOperationException();
            if (data.Length == HangAt)
                Thread.Sleep(Timeout.Infinite);
            return null;
        }

        public Task<IScript> ParseAsync(string data) => throw new NotImplementedException();
        public Delegate ParseDelegate(string data, params LambdaParameter[] parameters) => throw new NotImplementedException();
        public T ParseDelegate<T>(string data, params LambdaParameter[] parameters) => throw new NotImplementedException();
    }
}
