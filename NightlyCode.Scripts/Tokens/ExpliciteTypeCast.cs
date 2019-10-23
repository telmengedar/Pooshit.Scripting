using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// explicitely casts a value to another type
    /// </summary>
    public class ExpliciteTypeCast : ScriptToken, IParameterContainer {

        /// <summary>
        /// creates an <see cref="ExpliciteTypeCast"/>
        /// </summary>
        /// <param name="typeprovider">type provider used to get type</param>
        /// <param name="value">value to cast</param>
        /// <param name="targetType">target type to cast to</param>
        /// <param name="isdynamic">determines whether to execute a dynamic cast</param>
        public ExpliciteTypeCast(ITypeProvider typeprovider, IScriptToken value, ScriptValue targetType, IScriptToken isdynamic) {
            Value = value;
            TargetType = targetType;
            IsDynamic = isdynamic;
            TypeProvider = typeprovider;
        }

        /// <summary>
        /// type provider used to resolve type
        /// </summary>
        public ITypeProvider TypeProvider { get; }

        /// <summary>
        /// value to cast
        /// </summary>
        public IScriptToken Value { get; }

        /// <summary>
        /// type to cast value to
        /// </summary>
        public ScriptValue TargetType { get; }

        /// <summary>
        /// whether this is a dynamic cast
        /// </summary>
        public IScriptToken IsDynamic { get; }

        /// <inheritdoc />
        public override string Literal => "cast";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            string typename = TargetType.Execute(context)?.ToString();
            if (typename == null)
                throw new ScriptRuntimeException("cannot cast to null", this);
            Type type = TypeProvider.DetermineType(this, typename);

            object value = Value.Execute(context);

            if (IsDynamic?.Execute(context)?.ToBoolean() ?? false) {
                if (value == null) {
                    if (type.IsValueType)
                        return Activator.CreateInstance(type);
                    return null;
                }

                if (type.IsInstanceOfType(value))
                    return value;
                return null;
            }
            
            if (value == null) {
                if (type.IsValueType)
                    throw new ScriptRuntimeException($"null cannot get cast to '{typename}'", this);
                return null;
            }

            if (!type.IsInstanceOfType(value))
                throw new ScriptRuntimeException($"Unable to cast {value} to '{typename}'", this);

            return value;
        }

        /// <inheritdoc />
        public IEnumerable<IScriptToken> Parameters {
            get {
                yield return Value;
                yield return TargetType;
                if (IsDynamic != null)
                    yield return IsDynamic;
            }
        }
    }
}