﻿using System.Collections.Generic;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control.Internal {
    class ListStatementBlock : ITokenContainer, IScriptToken {
        readonly List<IScriptToken> body=new List<IScriptToken>();

        public void Add(IScriptToken token) {
            body.Add(token);
        }

        public string Literal => "???";

        public object Execute(ScriptContext context) {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IScriptToken> Children => body;
    }
}