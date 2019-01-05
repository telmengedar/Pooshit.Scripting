﻿using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// access to variable in script
    /// </summary>
    class ScriptVariable : IAssignableToken {
        readonly IVariableContext variablehost;

        /// <summary>
        /// creates a new <see cref="ScriptVariable"/>
        /// </summary>
        /// <param name="variablehost">host containing variable information</param>
        /// <param name="name">name of variable</param>
        public ScriptVariable(IVariableContext variablehost, string name) {
            this.variablehost = variablehost;
            Name = name;
        }

        /// <summary>
        /// name of variable
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        public object Execute() {
            return variablehost.GetVariable(Name);
        }

        /// <inheritdoc />
        public object Assign(IScriptToken token) {
            object value = token.Execute();
            variablehost.SetVariable(Name, value);
            return value;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"${Name}";
        }
    }
}