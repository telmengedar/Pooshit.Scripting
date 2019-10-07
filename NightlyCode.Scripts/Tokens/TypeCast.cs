using System;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// changes the type of an expression result
    /// </summary>
    public class TypeCast : ScriptToken {
        readonly Type targettype;
        readonly IScriptToken token;

        /// <summary>
        /// creates a new <see cref="TypeCast"/>
        /// </summary>
        /// <param name="keyword">keyword used in script when casting</param>
        /// <param name="targettype">target type</param>
        /// <param name="token">token resulting in value to cast</param>
        public TypeCast(string keyword, Type targettype, IScriptToken token) {
            Keyword = keyword;
            this.targettype = targettype;
            this.token = token;
        }

        /// <summary>
        /// keyword used for cast
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// type to cast argument to
        /// </summary>
        public Type TargetType => targettype;

        /// <summary>
        /// argument to cast
        /// </summary>
        public IScriptToken Argument => token;

        /// <inheritdoc />
        public override string Literal => "type()";

        /// <inheritdoc />
        protected override object ExecuteToken(ScriptContext context) {
            return Converter.Convert(token.Execute(context), targettype);
        }
    }
}