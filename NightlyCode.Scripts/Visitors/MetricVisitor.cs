using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Visitors {

    /// <summary>
    /// visits a script to determine metrics
    /// </summary>
    public class MetricVisitor : ScriptVisitor {

        /// <summary>
        /// number of variables
        /// </summary>
        public int Variables { get; set; }

        /// <summary>
        /// number of control tokens
        /// </summary>
        public int ControlTokens { get; set; }

        /// <summary>
        /// number of flow tokens
        /// </summary>
        public int FlowTokens { get; set; }

        /// <summary>
        /// number of imports
        /// </summary>
        public int Imports { get; set; }

        /// <summary>
        /// number of type casts
        /// </summary>
        public int TypeCasts { get; set; }

        /// <summary>
        /// number of type creations
        /// </summary>
        public int NewInstances { get; set; }

        /// <inheritdoc />
        public override void VisitNew(NewInstance newinstance) {
            base.VisitNew(newinstance);
            ++NewInstances;
        }

        /// <inheritdoc />
        public override void VisitTypeCast(ImpliciteTypeCast typecast) {
            base.VisitTypeCast(typecast);
            ++TypeCasts;
        }

        /// <inheritdoc />
        public override void VisitImport(Import import) {
            base.VisitImport(import);
            ++Imports;
        }

        /// <inheritdoc />
        public override void Visit(IScript script) {
            Variables = 0;
            ControlTokens = 0;
            FlowTokens = 0;
            Imports = 0;
            TypeCasts = 0;
            NewInstances = 0;
            base.Visit(script);
        }

        /// <inheritdoc />
        public override void VisitWait(Wait wait) {
            base.VisitWait(wait);
            ++FlowTokens;
        }

        /// <inheritdoc />
        public override void VisitThrow(Throw @throw) {
            base.VisitThrow(@throw);
            ++FlowTokens;
        }

        /// <inheritdoc />
        public override void VisitReturn(Return @return) {
            base.VisitReturn(@return);
            ++FlowTokens;
        }

        /// <inheritdoc />
        public override void VisitContinue(Continue @continue) {
            base.VisitContinue(@continue);
            ++FlowTokens;
        }

        /// <inheritdoc />
        public override void VisitBreak(Break @break) {
            base.VisitBreak(@break);
            ++FlowTokens;
        }

        /// <inheritdoc />
        public override void VisitVariable(ScriptVariable variable) {
            base.VisitVariable(variable);
            ++Variables;
        }

        /// <inheritdoc />
        public override void VisitControlToken(IStatementContainer controltoken) {
            base.VisitControlToken(controltoken);
            ++ControlTokens;
        }
    }
}