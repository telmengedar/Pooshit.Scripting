using System;
using System.Reflection;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Parser.Resolvers;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class ScriptTypeBoundaryTests {

        class TypeParameterHost {
            public string Describe(Type t) => t.Name;
        }

        class StaticMethodHost {
            public static string StaticEcho() => "static";
            public string InstanceEcho() => "instance";
        }

        static IScriptParser NewParser() => new ScriptParser();

        [Test, Parallelizable]
        public void Of_WrapsType() {
            ScriptType wrapped = ScriptType.Of(typeof(int));
            Assert.AreEqual("Int32", wrapped.Name);
            Assert.AreEqual("System.Int32", wrapped.FullName);
        }

        [Test, Parallelizable]
        public void Of_NullPropagates() {
            Assert.IsNull(ScriptType.Of(null));
        }

        [Test, Parallelizable]
        public void Unwrap_ReturnsWrappedType() {
            Assert.AreEqual(typeof(string), ScriptType.Of(typeof(string)).Unwrap());
        }

        [Test, Parallelizable]
        public void Equals_SameWrappedTypeAreEqual() {
            Assert.AreEqual(ScriptType.Of(typeof(int)), ScriptType.Of(typeof(int)));
            Assert.IsTrue(ScriptType.Of(typeof(int)) == ScriptType.Of(typeof(int)));
        }

        [Test, Parallelizable]
        public void Equals_DifferentWrappedTypeAreNotEqual() {
            Assert.AreNotEqual(ScriptType.Of(typeof(int)), ScriptType.Of(typeof(long)));
            Assert.IsTrue(ScriptType.Of(typeof(int)) != ScriptType.Of(typeof(long)));
        }

        [Test, Parallelizable]
        public void GetHashCode_MatchesWrappedTypeHashCode() {
            Assert.AreEqual(typeof(int).GetHashCode(), ScriptType.Of(typeof(int)).GetHashCode());
        }

        [Test, Parallelizable]
        public void ToString_ReturnsFullName() {
            Assert.AreEqual("System.Int32", ScriptType.Of(typeof(int)).ToString());
        }

        [Test, Parallelizable]
        [Description("no public member of ScriptType returns a Type, MemberInfo or Assembly - the handle is inert by construction")]
        public void PublicSurface_ExposesNoReflectiveMember() {
            foreach (PropertyInfo property in typeof(ScriptType).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                Assert.That(typeof(Type).IsAssignableFrom(property.PropertyType), Is.False, property.Name);
                Assert.That(typeof(MemberInfo).IsAssignableFrom(property.PropertyType), Is.False, property.Name);
            }

            // Object.GetType() can't be overridden and is intercepted before dispatch, so skipping it here is not a leak
            foreach (MethodInfo method in typeof(ScriptType).GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                if (method.DeclaringType == typeof(object))
                    continue;
                Assert.That(typeof(Type).IsAssignableFrom(method.ReturnType), Is.False, method.Name);
                Assert.That(typeof(MemberInfo).IsAssignableFrom(method.ReturnType), Is.False, method.Name);
            }
        }

        [Parallelizable]
        [TestCase("typeof(1)")]
        [TestCase("int")]
        [TestCase("(1).getType()")]
        [Description("S1/S2/S3 sources hand script a ScriptType, never a raw Type")]
        public void Sources_YieldScriptTypeInstance(string code) {
            Assert.IsInstanceOf<ScriptType>(NewParser().Parse(code).Execute());
        }

        [TestCase(typeof(Type))]
        [TestCase(typeof(MemberInfo))]
        [TestCase(typeof(MethodInfo))]
        [TestCase(typeof(Assembly))]
        [TestCase(typeof(Module))]
        [TestCase(typeof(Activator))]
        [TestCase(typeof(AppDomain))]
        [TestCase(typeof(RuntimeTypeHandle))]
        [Description("deny-family receivers refused by TypeGuard's structural predicate")]
        public void TypeGuard_DeniesReflectiveReceivers(Type receiver) {
            Assert.IsTrue(TypeGuard.IsForbiddenReflectiveReceiver(receiver));
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(string))]
        [TestCase(typeof(ScriptType))]
        [Description("ordinary host/script types are never refused by TypeGuard")]
        public void TypeGuard_AllowsOrdinaryReceivers(Type receiver) {
            Assert.IsFalse(TypeGuard.IsForbiddenReflectiveReceiver(receiver));
        }

        [Test, Parallelizable]
        public void TypeGuard_DeniesActualRuntimeTypeInstance() {
            Assert.IsTrue(TypeGuard.IsForbiddenReflectiveReceiver(typeof(int).GetType()));
        }

        [TestCase("invokemember")]
        [TestCase("getmethods")]
        [TestCase("createinstance")]
        [Description("Layer 2 method-name backstop")]
        public void TypeGuard_DeniesReflectiveMethodNames(string name) {
            Assert.IsTrue(TypeGuard.IsForbiddenReflectiveMethodName(name));
        }

        [TestCase("invoke")]
        [TestCase("gettype")]
        [TestCase("tostring")]
        [Description("legitimate method names must never hit the backstop")]
        public void TypeGuard_AllowsOrdinaryMethodNames(string name) {
            Assert.IsFalse(TypeGuard.IsForbiddenReflectiveMethodName(name));
        }

        [Test, Parallelizable]
        [Description("K7: a host method parameter typed System.Type receives the real type via the ScriptType->Type converter")]
        public void Sink_K7_HostMethodTypeParameter() {
            IScript script = NewParser().Parse("$h.describe(typeof(1))");
            Assert.AreEqual("Int32", script.Execute(new VariableProvider(new Variable("h", new TypeParameterHost()))));
        }

        [Test, Parallelizable]
        [Description("K6: Converter unwraps a ScriptType to the real Type it wraps")]
        public void Sink_K6_ConverterUnwrapsScriptType() {
            Assert.AreEqual(typeof(int), Converter.Convert<Type>(ScriptType.Of(typeof(int))));
        }

        [Test, Parallelizable]
        [Description("dropping Static from MethodResolver's binding flags: a static member is not reachable through instance dispatch")]
        public void StaticMethodsAreNotResolvable() {
            IScript script = NewParser().Parse("$h.staticecho()");
            Assert.Throws<ScriptRuntimeException>(() => script.Execute(new VariableProvider(new Variable("h", new StaticMethodHost()))));
        }

        [Test, Parallelizable]
        public void InstanceMethodsRemainResolvable() {
            IScript script = NewParser().Parse("$h.instanceecho()");
            Assert.AreEqual("instance", script.Execute(new VariableProvider(new Variable("h", new StaticMethodHost()))));
        }

        [Test, Parallelizable]
        [Description("S3 compiled: a bare registered type name compiles to a ScriptType constant, not a raw Type")]
        public void Compiled_TypeTokenYieldsScriptType() {
            Delegate function = NewParser().ParseDelegate("int");
            Assert.IsInstanceOf<ScriptType>(function.DynamicInvoke());
        }

        [Test, Parallelizable]
        [Description("S2 compiled: getType() compiles to a ScriptType handle, not a bound Object.GetType() call")]
        public void Compiled_GetTypeYieldsScriptType() {
            Delegate function = NewParser().ParseDelegate("x.getType()", new LambdaParameter<int>("x"));
            Assert.IsInstanceOf<ScriptType>(function.DynamicInvoke(5));
        }

        [Test, Parallelizable]
        [Description("Layer 1 compiled: reflective receivers are refused at build time")]
        public void Compiled_ReflectiveReceiverRefused() {
            Assert.Throws<NotSupportedException>(() => NewParser().ParseDelegate("x.getType().getMethods()", new LambdaParameter<int>("x")));
        }
    }
}
