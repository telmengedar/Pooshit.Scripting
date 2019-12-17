using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Extern;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token used to specify type of a parameter
    /// </summary>
    public class ScriptParameter : ScriptToken, IParameterContainer {

        /// <summary>
        /// creates a new <see cref="ScriptParameter"/>
        /// </summary>
        /// <param name="typeProvider">provider used to resolve type name</param>
        /// <param name="variable">parameter variable</param>
        /// <param name="type">value which expresses the parameter type</param>
        /// <param name="defaultvalue">default value to use when parameter value is not provided</param>
        public ScriptParameter(ITypeProvider typeProvider, ScriptVariable variable, ScriptValue type, IScriptToken defaultvalue) {
            Variable = variable;
            Type = type;
            TypeProvider = typeProvider;
            DefaultValue = defaultvalue;
        }

        /// <summary>
        /// type provider used to resolve type
        /// </summary>
        public ITypeProvider TypeProvider { get; }

        /// <summary>
        /// parameter value
        /// </summary>
        public ScriptVariable Variable { get; }

        /// <summary>
        /// type parameter is forced to
        /// </summary>
        public ScriptValue Type { get; }

        /// <summary>
        /// default value when parameter value is not provided
        /// </summary>
        public IScriptToken DefaultValue { get; }

        /// <inheritdoc />
        public override string Literal => "parameter";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            string typename = Type.Execute(context)?.ToString();
            if (typename == null)
                throw new ScriptRuntimeException("Invalid parameter type specification", this);

            bool isarray = typename.EndsWith("[]");
            if (isarray)
                typename = typename.Substring(0, typename.Length - 2);

            Type type = TypeProvider.DetermineType(this, typename);
            Type elementtype = null;

            if (isarray) {
                elementtype = type;
                type = typeof(IEnumerable<>).MakeGenericType(type);
            }

            object value;
            if(context.Variables.ContainsVariable(Variable.Name)||(context.Arguments.ContainsVariable(Variable.Name)))
                value = Variable.Execute(context);
            else {
                if (DefaultValue != null)
                    value = DefaultValue.Execute(context);
                else throw new ScriptRuntimeException($"Variable {Variable.Name} not provided by script call and no default value provided", this);
            }

            if (value == null) {
                if(type.IsValueType)
                    throw new ScriptRuntimeException("Unable to pass null to a value parameter", this);
                return null;
            }

            if (!type.IsInstanceOfType(value)) {
                try {
                    if (isarray) {
                        if (value is IEnumerable enumeration) {
                            object[] items = enumeration.Cast<object>().ToArray();
                            Array array = Array.CreateInstance(elementtype, items.Length);
                            int index = 0;
                            foreach (object item in items)
                                array.SetValue(Converter.Convert(item, elementtype), index++);
                            value = array;
                        }
                        else {
                            Array array = Array.CreateInstance(elementtype, 1);
                            array.SetValue(Converter.Convert(value, elementtype), 0);
                            value = array;
                        }
                    }
                    else {
                        value = Converter.Convert(value, type);
                    }
                }
                catch (Exception e) {
                    throw new ScriptRuntimeException($"Unable to convert parameter '{value}' to '{typename}'", this, e);
                }
            }

            ((VariableProvider)context.Arguments).ReplaceVariable(Variable.Name, value);
            return value;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                yield return Variable;
                yield return Type;
                if (DefaultValue != null)
                    yield return DefaultValue;
            }
        }

        /// <inheritdoc />
        public bool ParametersOptional => false;
    }
}