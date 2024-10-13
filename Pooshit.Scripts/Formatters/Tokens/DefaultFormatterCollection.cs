using Pooshit.Scripting.Control;
using Pooshit.Scripting.Operations;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Formatters.Tokens {

    /// <summary>
    /// formatter collection with default entries
    /// </summary>
    public class DefaultFormatterCollection : FormatterCollection {

        /// <summary>
        /// creates a new <see cref="DefaultFormatterCollection"/>
        /// </summary>
        public DefaultFormatterCollection() {
            AddFormatter(typeof(IScriptToken), new DefaultFormatter());
            AddFormatter(typeof(IBinaryToken), new BinaryTokenFormatter());
            AddFormatter(typeof(IUnaryToken), new UnaryTokenFormatter());
            AddFormatter(typeof(StatementBlock), new StatementBlockFormatter());
            AddFormatter(typeof(ScriptValue), new ValueFormatter());
            AddFormatter(typeof(ScriptVariable), new VariableFormatter());
            AddFormatter(typeof(DictionaryToken), new DictionaryFormatter());
            AddFormatter(typeof(ScriptArray), new ArrayFormatter());
            AddFormatter(typeof(ScriptMember), new MemberFormatter());
            AddFormatter(typeof(ScriptMethod), new MethodFormatter());
            AddFormatter(typeof(ScriptIndexer), new IndexerFormatter());
            AddFormatter(typeof(StringInterpolation), new InterpolationFormatter());
            AddFormatter(typeof(Comment), new CommentFormatter());
            AddFormatter(typeof(ArithmeticBlock), new ArithmeticFormatter());
            AddFormatter(typeof(NewInstance), new NewFormatter());
            AddFormatter(typeof(Switch), new SwitchFormatter());
            AddFormatter(typeof(NewLine), new NewLineFormatter());
            AddFormatter(typeof(Throw), new ThrowFormatter());
            AddFormatter(typeof(Try), new TryFormatter());
            AddFormatter(typeof(ImpliciteTypeCast), new CastFormatter());
            AddFormatter(typeof(If), new IfFormatter());
        }
    }
}