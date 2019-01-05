namespace NightlyCode.Scripting.Control {

    /// <summary>
    /// breaks execution of a loop
    /// </summary>
    public class Break : IScriptToken {

        /// <inheritdoc />
        public object Execute() {
            return this;
        }

        /// <inheritdoc />
        public override string ToString() {
            return "break";
        }
    }
}