using NightlyCode.Scripting.Control;
using NightlyCode.Scripting.Operations;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Visitors {

    /// <inheritdoc />
    public class ScriptVisitor : IScriptVisitor {

        /// <inheritdoc />
        public virtual void Visit(IScript script) {
            VisitToken(script.Body);
        }

        /// <summary>
        /// visits any token in a script
        /// </summary>
        /// <param name="token">token to visit</param>
        public virtual void VisitToken(IScriptToken token) {
            if (token is ScriptVariable variable)
                VisitVariable(variable);
            else if (token is ITokenContainer container)
                VisitContainer(container);
            else if (token is IControlToken control)
                VisitControlToken(control);
            else if (token is IBinaryToken binary)
                VisitBinaryToken(binary);
            else if (token is Break @break)
                VisitBreak(@break);
            else if (token is Continue @continue)
                VisitContinue(@continue);
            else if (token is Return @return)
                VisitReturn(@return);
            else if (token is Throw @throw)
                VisitThrow(@throw);
            else if (token is Wait wait)
                VisitWait(wait);
            else if (token is TypeCast typecast)
                VisitTypeCast(typecast);
            else if (token is Import import)
                VisitImport(import);
            else if (token is NewInstance newinstance)
                VisitNew(newinstance);
            else if (token is ScriptIndexer indexer)
                VisitIndexer(indexer);
            else if (token is ScriptMethod method)
                VisitMethod(method);
            else if (token is ScriptMember member)
                VisitMember(member);
            else if (token is ArithmeticBlock arithmeticblock)
                VisitArithmeticBlock(arithmeticblock);
            else if (token is ScriptArray array)
                VisitArray(array);
        }

        public virtual void VisitMember(ScriptMember member) {
            VisitToken(member.Host);
        }

        public virtual void VisitTryCatchBlock(Try @try) {
            VisitToken(@try.Body);
            VisitToken(@try.Catch);
        }

        public virtual void VisitArray(ScriptArray array) {
            foreach (IScriptToken value in array.Values)
                VisitToken(value);
        }

        /// <summary>
        /// visits an arithmetic block
        /// </summary>
        /// <param name="arithmeticblock">token to visit</param>
        public virtual void VisitArithmeticBlock(ArithmeticBlock arithmeticblock) {
            VisitToken(arithmeticblock.InnerBlock);
        }

        /// <summary>
        /// visits a indexer token
        /// </summary>
        /// <param name="method">token to visit</param>
        public virtual void VisitMethod(ScriptMethod method) {
            VisitToken(method.Host);
            VisitParameters(method);
        }

        /// <summary>
        /// visits a indexer token
        /// </summary>
        /// <param name="indexer">token to visit</param>
        public virtual void VisitIndexer(ScriptIndexer indexer) {
            VisitToken(indexer.Host);
            VisitParameters(indexer);
        }

        /// <summary>
        /// visits a binary token
        /// </summary>
        /// <param name="binary">token to visit</param>
        public virtual void VisitBinaryToken(IBinaryToken binary) {
            VisitToken(binary.Lhs);
            VisitToken(binary.Rhs);
        }

        /// <summary>
        /// visits a new token
        /// </summary>
        /// <param name="newinstance">token to visit</param>
        public virtual void VisitNew(NewInstance newinstance) {
            VisitParameters(newinstance);
        }

        /// <summary>
        /// visits a import token
        /// </summary>
        /// <param name="import">token to visit</param>
        public virtual void VisitImport(Import import) {
            VisitToken(import.Reference);
        }

        /// <summary>
        /// visits a type cast token
        /// </summary>
        /// <param name="typecast">token to visit</param>
        public virtual void VisitTypeCast(TypeCast typecast) {
            VisitToken(typecast.Argument);
        }

        /// <summary>
        /// visits a wait token
        /// </summary>
        /// <param name="wait">token to visit</param>
        public virtual void VisitWait(Wait wait) {
            VisitToken(wait.Time);
        }

        /// <summary>
        /// visits a throw token
        /// </summary>
        /// <param name="throw">token to visit</param>
        public virtual void VisitThrow(Throw @throw) {
            VisitToken(@throw.Message);
            if (@throw.Context != null)
                VisitToken(@throw.Context);
        }

        /// <summary>
        /// visits a return token
        /// </summary>
        /// <param name="return">token to visit</param>
        public virtual void VisitReturn(Return @return) {
            VisitToken(@return.Value);
        }

        /// <summary>
        /// visits a continue token
        /// </summary>
        /// <param name="continue">token to visit</param>
        public virtual void VisitContinue(Continue @continue) {
            VisitToken(@continue.Depth);
        }

        /// <summary>
        /// visits a break token
        /// </summary>
        /// <param name="break">token to visit</param>
        public virtual void VisitBreak(Break @break) {
            VisitToken(@break.Depth);
        }

        /// <summary>
        /// visits a statement block in a script
        /// </summary>
        /// <param name="container">container to visit</param>
        public virtual void VisitContainer(ITokenContainer container) {
            foreach (IScriptToken token in container.Children)
                VisitToken(token);
        }

        /// <summary>
        /// visits a variable in a script
        /// </summary>
        /// <param name="variable">script variable</param>
        public virtual void VisitVariable(ScriptVariable variable) {
        }

        /// <summary>
        /// visits a control token
        /// </summary>
        /// <param name="controltoken">control token to visit</param>
        public virtual void VisitControlToken(IControlToken controltoken) {
            if (controltoken is Try @try)
                VisitTryCatchBlock(@try);
            else VisitToken(controltoken.Body);
        }

        /// <summary>
        /// visits parameters of a token
        /// </summary>
        /// <param name="token">control token to visit</param>
        public virtual void VisitParameters(IParameterizedToken token) {
            foreach(IScriptToken parameter in token.Parameters)
                VisitToken(parameter);
        }
    }
}