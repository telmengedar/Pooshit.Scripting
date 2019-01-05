namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// returns a value and end execution of current method
    /// </summary>
    public class Return : IScriptToken {
        readonly IScriptToken value;

        /// <summary>
        /// creates a new <see cref="Return"/>
        /// </summary>
        /// <param name="value">token to return</param>
        public Return(IScriptToken value) {
            this.value = value;
        }

        /// <summary>
        /// token resulting in value to return
        /// </summary>
        public IScriptToken Value => value;

        /// <inheritdoc />
        public object Execute() {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"return {value}";
        }
    }
}