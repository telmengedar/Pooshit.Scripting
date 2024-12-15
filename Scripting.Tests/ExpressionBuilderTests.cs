using System;
using NUnit.Framework;
using Pooshit.Scripting;
using Pooshit.Scripting.Expressions;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Parser;
using Scripting.Tests.Data;

namespace Scripting.Tests;

[TestFixture, Parallelizable]
public class ExpressionBuilderTests {
		
	[Test, Parallelizable]
	public void ComputeParameter() {
		ScriptParser parser = new();

		Delegate function = parser.ParseDelegate("x+y", new LambdaParameter<int>("x"), new LambdaParameter<int>("y"));
		object result = function.DynamicInvoke(5, 11);

		Assert.AreEqual(16, result);
	}

	[Test, Parallelizable]
	public void SetIndexer() {
		ScriptParser parser = new();
		Delegate function = parser.ParseDelegate("host[\"hello\"]=\"bums\"", new LambdaParameter<TestHost>("host"));
		
		TestHost host = new();
		function.DynamicInvoke(host);

		Assert.AreEqual("bums", host["hello"]);
	}
	
	[Test, Parallelizable]
	public void GetIndexer() {
		ScriptParser parser = new();
		TestHost host = new() {
			["hello"] = "bums"
		};

		Delegate function = parser.ParseDelegate("host[\"hello\"]", new LambdaParameter<TestHost>("host"));
		object result=function.DynamicInvoke(host);

		Assert.AreEqual("bums", result);
	}

	[Test, Parallelizable]
	public void AccessProperty() {
		ScriptParser parser = new();
		TestHost host = new() {
			Property = 67
		};

		Delegate function = parser.ParseDelegate("host.Property", new LambdaParameter<TestHost>("host"));
		object result = function.DynamicInvoke(host);

		Assert.AreEqual(67, result);
	}

	[Test, Parallelizable]
	public void AccessPropertyIgnoreCase() {
		ScriptParser parser = new();
		TestHost host = new() {
			Property = 67
		};

		Delegate function = parser.ParseDelegate("host.property", new LambdaParameter<TestHost>("host"));
		object result = function.DynamicInvoke(host);

		Assert.AreEqual(67, result);
	}

	[Test, Parallelizable]
	public void CallMethod() {
		ScriptParser parser = new();
		TestHost host = new();

		Delegate function = parser.ParseDelegate("host.TestMethod(\"x\", [\"y\", \"z\"])", new LambdaParameter<TestHost>("host"));
		object result=function.DynamicInvoke(host);

		Assert.AreEqual("x_y,z", result);
	}

	[Test, Parallelizable]
	public void CallMethodIgnoreCase() {
		ScriptParser parser = new();
		TestHost host = new();

		Delegate function = parser.ParseDelegate("host.testmethod(\"x\", [\"y\", \"z\"])", new LambdaParameter<TestHost>("host"));
		object result=function.DynamicInvoke(host);

		Assert.AreEqual("x_y,z", result);
	}

	[Test, Parallelizable]
	public void ArrayIndexer() {
		ScriptParser parser = new();

		Delegate function = parser.ParseDelegate("array[2]", new LambdaParameter<int[]>("array"));
		object result = function.DynamicInvoke(new[] { 7, 2, 11, 15 });

		Assert.AreEqual(11, result);
	}

	[Test, Parallelizable]
	public void ArithmeticBlock() {
		ScriptParser parser = new();

		Delegate function = parser.ParseDelegate("(4+6)*(3-1)");
		object result = function.DynamicInvoke();

		Assert.AreEqual(20, result);
	}
	
	[Test, Parallelizable]
	public void OperatorPrecedence() {
		ScriptParser parser = new();

		Delegate function = parser.ParseDelegate("4+6*3-1");
		object result = function.DynamicInvoke();

		Assert.AreEqual(21, result);
	}

}