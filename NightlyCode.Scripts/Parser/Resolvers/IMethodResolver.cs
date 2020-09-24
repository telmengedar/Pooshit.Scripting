using System;
using System.Reflection;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Resolvers {

    /// <summary>
    /// resolves method calls for <see cref="ScriptMethod"/> tokens
    /// </summary>
    public interface IMethodResolver {

        /// <summary>
        /// resolves the method to call
        /// </summary>
        /// <param name="host">host on which to call a method</param>
        /// <param name="methodname">name of method to call</param>
        /// <param name="parameters">method parameters</param>
        /// <param name="referenceparameters">reference parameter information</param>
        /// <param name="genericparameters">parameters for generic method templates</param>
        /// <returns>resolved method to call</returns>
        IResolvedMethod Resolve(object host, string methodname, object[] parameters, ReferenceParameter[] referenceparameters, Type[] genericparameters=null);

        /// <summary>
        /// resolves a constructor to call
        /// </summary>
        /// <param name="type">type of which to call constructor</param>
        /// <param name="parameters">parameters for constructor call</param>
        /// <returns>constructor to be called</returns>
        ConstructorInfo ResolveConstructor(Type type, object[] parameters);
    }
}