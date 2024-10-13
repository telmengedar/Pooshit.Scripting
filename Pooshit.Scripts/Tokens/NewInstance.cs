using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// creates a new instance of a type
    /// </summary>
    public class NewInstance : ScriptToken, IParameterContainer {
        readonly ITypeInstanceProvider provider;
        readonly IScriptToken[] parameters;

        /// <summary>
        /// creates a new <see cref="NewInstance"/>
        /// </summary>
        /// <param name="typename">name of type to instanciate like specified in code</param>
        /// <param name="type">type information to instanciate</param>
        /// <param name="provider">type provider to use</param>
        /// <param name="parameters">parameters for constructor</param>
        internal NewInstance(string typename, Type type, ITypeInstanceProvider provider, IScriptToken[] parameters) {
            TypeName = typename;
            Type = type;
            this.provider = provider;
            this.parameters = parameters;
        }

        /// <summary>
        /// name of type in script
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// type to be created (just an indicator for reflection)
        /// </summary>
        /// <remarks>
        /// when in doubt use typeof(object)
        /// </remarks>
        public Type Type { get; }

        /// <summary>
        /// initializer used to set initial properties of object
        /// </summary>
        public IScriptToken Initializer { get; set; }

        /// <inheritdoc />
        public override string Literal => "new";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            try {
                object instance = provider.Create(parameters, context);

                if (Initializer != null) {
                    object init = Initializer.Execute(context);
                    if (init is Dictionary<object, object> dic)
                        return dic.FillType(instance);
                    throw new ArgumentException("Invalid object initializer value");
                }

                return instance;
            }
            catch (ScriptRuntimeException e) {
                throw new ScriptRuntimeException(e.Message, this, e.InnerException);
            }
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"new {provider}({string.Join<IScriptToken>(", ", parameters)})";
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters => parameters;

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}