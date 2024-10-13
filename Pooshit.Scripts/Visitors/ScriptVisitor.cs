using System.Collections.Generic;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Operations;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Visitors;

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
        else if (token is IStatementContainer control)
            VisitControlToken(control);
        else if (token is IBinaryToken binary)
            VisitBinaryToken(binary);
        else if (token is IUnaryToken unary)
            VisitUnary(unary);
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
        else if (token is Await awaittoken)
            VisitAwait(awaittoken);
        else if (token is ImpliciteTypeCast typecast)
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
        else if (token is DictionaryToken dictionary)
            VisitDictionary(dictionary);
        else if (token is ScriptParameter parameter)
            VisitScriptParameter(parameter);
        else if (token is ExpliciteTypeCast explicittypecast)
            VisitExplicitTypeCast(explicittypecast);
    }

    /// <summary>
    /// visits an explicit type cast
    /// </summary>
    /// <param name="explicittypecast">type cast token</param>
    public virtual void VisitExplicitTypeCast(ExpliciteTypeCast explicittypecast) {
        VisitParameterContainer(explicittypecast);
    }

    /// <summary>
    /// visits a script parameter declaration
    /// </summary>
    /// <param name="parameter">script parameter token</param>
    public virtual void VisitScriptParameter(ScriptParameter parameter) {
        VisitParameterContainer(parameter);
    }

    /// <summary>
    /// visits an await token
    /// </summary>
    /// <param name="awaittoken">await token to visit</param>
    public virtual void VisitAwait(Await awaittoken) {
        VisitToken(awaittoken.Task);
    }

    /// <summary>
    /// visits a dictionary token
    /// </summary>
    /// <param name="dictionary">dictionary to visit</param>
    public virtual void VisitDictionary(DictionaryToken dictionary) {
        foreach (KeyValuePair<IScriptToken, IScriptToken> entry in dictionary.Entries) {
            VisitToken(entry.Key);
            VisitToken(entry.Value);
        }
    }

    /// <summary>
    /// visits a member of a host
    /// </summary>
    /// <param name="member">member to visit</param>
    public virtual void VisitMember(ScriptMember member) {
        VisitToken(member.Host);
    }

    /// <summary>
    /// visits an array
    /// </summary>
    /// <param name="array">array to visit</param>
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
    /// visits an unary token
    /// </summary>
    /// <param name="unary"></param>
    public virtual void VisitUnary(IUnaryToken unary) {
        VisitToken(unary.Operand);
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
        VisitParameters(import);
    }

    /// <summary>
    /// visits a type cast token
    /// </summary>
    /// <param name="typecast">token to visit</param>
    public virtual void VisitTypeCast(ImpliciteTypeCast typecast) {
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
    /// visits a parameter container
    /// </summary>
    /// <param name="parametercontainer">token which takes parameters for execution</param>
    public virtual void VisitParameterContainer(IParameterContainer parametercontainer) {
        foreach (IScriptToken parameter in parametercontainer.Parameters)
            VisitToken(parameter);
    }

    /// <summary>
    /// visits a control token
    /// </summary>
    /// <param name="controltoken">control token to visit</param>
    public virtual void VisitControlToken(IStatementContainer controltoken) {
        if (controltoken is IParameterContainer parametercontrol)
            VisitParameterContainer(parametercontrol);
        VisitToken(controltoken.Body);
        if (controltoken is Try @try)
            VisitToken(@try.Catch);
        if (controltoken is If @if && @if.Else != null)
            VisitToken(@if.Else);
    }

    /// <summary>
    /// visits parameters of a token
    /// </summary>
    /// <param name="token">control token to visit</param>
    public virtual void VisitParameters(IParameterContainer token) {
        foreach(IScriptToken parameter in token.Parameters)
            VisitToken(parameter);
    }
}