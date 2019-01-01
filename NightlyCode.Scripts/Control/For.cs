using NightlyCode.Scripting.Extensions;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// loop with an initializer, a condition and an increment
    /// </summary>
    public class For : IControlToken {
        readonly IScriptToken initializer;
        readonly IScriptToken condition;
        readonly IScriptToken step;

        /// <summary>
        /// creates a new <see cref="For"/> statement 
        /// </summary>
        /// <param name="loopparameters"></param>
        public For(IScriptToken[] loopparameters) {
            if (loopparameters.Length != 3)
                throw new ScriptException("3 loop parameters needed for a 'for' loop");
            initializer = loopparameters[0];
            condition = loopparameters[1];
            step = loopparameters[2];
        }

        /// <inheritdoc />
        public object Execute() {
            initializer?.Execute();

            while (condition.Execute().ToBoolean()) {
                object value=Body?.Execute();
                if (value is Return)
                    return value;
                step?.Execute();
            }

            return null;
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }

        public override string ToString() {
            return $"for({initializer},{condition},{step}) {Body}";
        }
    }
}