using System;
using NightlyCode.Scripting.Extensions;
using NightlyCode.Scripting.Tokens;

namespace NightlyCode.Scripting.Parser.Resolvers {

    /// <summary>
    /// key for a method call
    /// </summary>
    public class MethodCacheKey {

        /// <summary>
        /// creates a new <see cref="MethodCacheKey"/>
        /// </summary>
        /// <param name="hostType">type of host to call method on</param>
        /// <param name="methodName">method to call</param>
        /// <param name="parameterTypes">parameter value types</param>
        /// <param name="referenceInformation">information about reference parameters (optional)</param>
        /// <param name="genericarguments">arguments for generic method templates</param>
        public MethodCacheKey(Type hostType, string methodName, Type[] parameterTypes, ReferenceParameter[] referenceInformation, Type[] genericarguments) {
            HostType = hostType;
            MethodName = methodName;
            ParameterTypes = parameterTypes;
            ReferenceInformation = referenceInformation;
            GenericArguments = genericarguments;
        }

        /// <summary>
        /// creates a new <see cref="MethodCacheKey"/>
        /// </summary>
        /// <param name="hostType">type of host to call method on</param>
        /// <param name="parameterTypes">parameter value types</param>
        public MethodCacheKey(Type hostType, Type[] parameterTypes)
        {
            HostType = hostType;
            ParameterTypes = parameterTypes;
        }

        /// <summary>
        /// type of host to call method on
        /// </summary>
        public Type HostType { get; }

        /// <summary>
        /// method to call
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// parameter value types
        /// </summary>
        public Type[] ParameterTypes { get; }

        /// <summary>
        /// arguments for generic method templates
        /// </summary>
        public Type[] GenericArguments { get; }

        /// <summary>
        /// information about reference parameters
        /// </summary>
        public ReferenceParameter[] ReferenceInformation { get; }

        bool Equals(MethodCacheKey other) {
            return HostType == other.HostType
                   && MethodName == other.MethodName
                   && ArrayExtensions.Equals(ParameterTypes, other.ParameterTypes)
                   && ArrayExtensions.Equals(ReferenceInformation, other.ReferenceInformation)
                   && ArrayExtensions.Equals(GenericArguments, other.GenericArguments);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MethodCacheKey) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (HostType != null ? HostType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MethodName != null ? MethodName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ParameterTypes != null ? ArrayExtensions.GetHashCode(ParameterTypes) : 0);
                hashCode = (hashCode * 397) ^ (ReferenceInformation != null ? ArrayExtensions.GetHashCode(ReferenceInformation) : 0);
                hashCode = (hashCode * 397) ^ (GenericArguments != null ? ArrayExtensions.GetHashCode(GenericArguments) : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MethodCacheKey left, MethodCacheKey right) {
            return Equals(left, right);
        }

        public static bool operator !=(MethodCacheKey left, MethodCacheKey right) {
            return !Equals(left, right);
        }
    }
}