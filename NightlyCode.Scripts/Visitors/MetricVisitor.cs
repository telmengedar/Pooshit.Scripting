using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Visitors {

    /// <summary>
    /// visits a script to determine metrics
    /// </summary>
    public class MetricVisitor : ScriptVisitor {

        public int Variables { get; set; }

        public int ControlTokens { get; set; }

        public int FlowTokens { get; set; }

        public int Imports { get; set; }

        public int TypeCasts { get; set; }

        public int NewInstances { get; set; }

        public override void VisitNew(NewInstance newinstance) {
            base.VisitNew(newinstance);
            ++NewInstances;
        }

        public override void VisitTypeCast(TypeCast typecast) {
            base.VisitTypeCast(typecast);
            ++TypeCasts;
        }

        public override void VisitImport(Import import) {
            base.VisitImport(import);
            ++Imports;
        }

        public override void Visit(IScript script) {
            Variables = 0;
            ControlTokens = 0;
            FlowTokens = 0;
            Imports = 0;
            TypeCasts = 0;
            NewInstances = 0;
            base.Visit(script);
        }

        public override void VisitWait(Wait wait) {
            base.VisitWait(wait);
            ++FlowTokens;
        }

        public override void VisitThrow(Throw @throw) {
            base.VisitThrow(@throw);
            ++FlowTokens;
        }

        public override void VisitReturn(Return @return) {
            base.VisitReturn(@return);
            ++FlowTokens;
        }

        public override void VisitContinue(Continue @continue) {
            base.VisitContinue(@continue);
            ++FlowTokens;
        }

        public override void VisitBreak(Break @break) {
            base.VisitBreak(@break);
            ++FlowTokens;
        }

        public override void VisitVariable(ScriptVariable variable) {
            base.VisitVariable(variable);
            ++Variables;
        }

        public override void VisitControlToken(IControlToken controltoken) {
            base.VisitControlToken(controltoken);
            ++ControlTokens;
        }
    }
}