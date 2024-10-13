using Pooshit.Scripting.Data;
using Pooshit.Scripting.Extensions;

namespace Pooshit.Scripting.Operations.Logic {

    /// <summary>
    /// computes logical AND of lhs and rhs
    /// </summary>
    public class LogicAnd : LogicOperation {
        internal LogicAnd() {
        }

        /// <inheritdoc />
        protected override object Operate(ScriptContext context) {
            return Lhs.Execute(context).ToBoolean() && Rhs.Execute(context).ToBoolean();
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.LogicAnd;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Lhs} && {Rhs}";
        }

        /// <inheritdoc />
        public override string Literal => "&&";
    }
}