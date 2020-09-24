using System;
using System.Collections.Generic;

namespace NightlyCode.Scripting.Parser {

    /// <summary>
    /// provides types which can be created in script
    /// </summary>
    public interface ITypeProvider {

        /// <summary>
        /// get type instance provider which is registered under the specified name
        /// </summary>
        /// <param name="name">name of type to get</param>
        /// <returns>instance provider for requested type</returns>
        ITypeInstanceProvider GetType(string name);

        /// <summary>
        /// adds an instance provider for a type
        /// </summary>
        /// <param name="name">name of type to add</param>
        /// <param name="provider">instance provider</param>
        void AddType(string name, ITypeInstanceProvider provider);

        /// <summary>
        /// adds an instance provider for a type
        /// </summary>
        /// <param name="name">name of type to add</param>
        /// <param name="type">type to add</param>
        void AddType(Type type, string name=null);

        /// <summary>
        /// adds a type using a type provider calling existing constructors
        /// </summary>
        /// <typeparam name="T">type to create</typeparam>
        /// <param name="name">name to use to create type</param>
        void AddType<T>(string name=null);

        /// <summary>
        /// removes the specified type instance provider
        /// </summary>
        /// <param name="name">name of type to remove</param>
        void RemoveType(string name);

        /// <summary>
        /// determines whether the type provider contains a specific type
        /// </summary>
        /// <param name="name">name of type</param>
        /// <returns>true if type information exists, false otherwise</returns>
        bool HasType(string name);

        /// <summary>
        /// types provdided
        /// </summary>
        IEnumerable<string> ProvidedTypes { get; }
    }
}