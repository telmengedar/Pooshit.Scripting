using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// token containing body for an <see cref="If"/> statement of which condition is not met
    /// </summary>
    class Else : IControlToken {

        /// <inheritdoc />
        public object Execute() {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public IScriptToken Body { get; set; }
    }
}