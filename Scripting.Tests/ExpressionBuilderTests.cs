using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pooshit.Scripting.Expressions;
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
	public void ComputeParameterTyped() {
		ScriptParser parser = new();

		Func<int,int,int> function = parser.ParseDelegate<Func<int,int,int>>("x+y", 
		                                                                     new LambdaParameter<int>("x"), 
		                                                                     new LambdaParameter<int>("y"));
		int result = function(5, 11);

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
	public void SetIndexerTyped() {
		ScriptParser parser = new();
		Action<TestHost> function = parser.ParseDelegate<Action<TestHost>>("host[\"hello\"]=\"bums\"", 
		                                                 new LambdaParameter<TestHost>("host"));
		
		TestHost host = new();
		function(host);

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
	public void GetIndexerTyped() {
		ScriptParser parser = new();
		TestHost host = new() {
			["hello"] = "bums"
		};

		Func<TestHost,object> function = parser.ParseDelegate<Func<TestHost,object>>("host[\"hello\"]", 
		                                                                             new LambdaParameter<TestHost>("host"));
		object result=function(host);

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
	public void AccessPropertyTyped() {
		ScriptParser parser = new();
		TestHost host = new() {
			Property = 67
		};

		Func<TestHost,int> function = parser.ParseDelegate<Func<TestHost,int>>("host.Property", 
		                                                                       new LambdaParameter<TestHost>("host"));
		int result = function(host);

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
	public void AccessPropertyIgnoreCaseTyped() {
		ScriptParser parser = new();
		TestHost host = new() {
			Property = 67
		};

		Func<TestHost,int> function = parser.ParseDelegate<Func<TestHost,int>>("host.property", 
		                                                                       new LambdaParameter<TestHost>("host"));
		int result = function(host);

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
	public void CallMethodTyped() {
		ScriptParser parser = new();
		TestHost host = new();

		Func<TestHost,string> function = parser.ParseDelegate<Func<TestHost,string>>("host.TestMethod(\"x\", [\"y\", \"z\"])", 
		                                                                             new LambdaParameter<TestHost>("host"));
		string result=function(host);

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
	public void CallMethodIgnoreCaseTyped() {
		ScriptParser parser = new();
		TestHost host = new();

		Func<TestHost,string> function = parser.ParseDelegate<Func<TestHost,string>>("host.testmethod(\"x\", [\"y\", \"z\"])", 
		                                                                             new LambdaParameter<TestHost>("host"));
		string result=function(host);

		Assert.AreEqual("x_y,z", result);
	}

	[Test, Parallelizable]
	public void AutoCastMethodArguments() {
		ScriptParser parser = new();
		TestHost host = new();

		Func<TestHost, object, string> function = parser.ParseDelegate<Func<TestHost, object,string>>("host.testmethod(x, [\"y\", \"z\"])", 
		                                                                                              new LambdaParameter<TestHost>("host"),
		                                                                                              new LambdaParameter<object>("x"));
		string result=function(host, "x");

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
	public void ArrayIndexerTyped() {
		ScriptParser parser = new();

		Func<int[],int> function = parser.ParseDelegate<Func<int[],int>>("array[2]", 
		                                                                 new LambdaParameter<int[]>("array"));
		int result = function([7, 2, 11, 15]);

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
	public void ArithmeticBlockTyped() {
		ScriptParser parser = new();

		Func<int> function = parser.ParseDelegate<Func<int>>("(4+6)*(3-1)");
		int result = function();

		Assert.AreEqual(20, result);
	}

	[Test, Parallelizable]
	public void OperatorPrecedence() {
		ScriptParser parser = new();

		Delegate function = parser.ParseDelegate("4+6*3-1");
		object result = function.DynamicInvoke();

		Assert.AreEqual(21, result);
	}

	[Test, Parallelizable]
	public void OperatorPrecedenceTyped() {
		ScriptParser parser = new();

		Func<int> function = parser.ParseDelegate<Func<int>>("4+6*3-1");
		int result = function();

		Assert.AreEqual(21, result);
	}

	[Test, Parallelizable]
	public void ExtensionMethod() {
		ScriptParser parser = new();
		parser.Extensions.AddExtensions<TestExtensions>();

		Delegate function = parser.ParseDelegate("\"hel\".append(\"lo\")");
		object result = function.DynamicInvoke();

		Assert.AreEqual("hello", result);
	}

	[Test, Parallelizable]
	public void ExtensionMethodTyped() {
		ScriptParser parser = new();
		parser.Extensions.AddExtensions<TestExtensions>();

		Func<string> function = parser.ParseDelegate<Func<string>>("\"hel\".append(\"lo\")");
		string result = function();

		Assert.AreEqual("hello", result);
	}

	[Test, Parallelizable]
	public void ExtensionMethodOnExtensionResult() {
		ScriptParser parser = new();
		parser.Extensions.AddExtensions(typeof(Math));

		TestHost host = new();
		
		Delegate function = parser.ParseDelegate("host.float(3.0f).max(1.0f).max(host.float(7.0f))", new LambdaParameter<TestHost>("host"));
		object result = function.DynamicInvoke(host);

		Assert.AreEqual(7.0f, result);
	}

	[Test, Parallelizable]
	public void ExtensionMethodOnExtensionResultTyped() {
		ScriptParser parser = new();
		parser.Extensions.AddExtensions(typeof(Math));

		TestHost host = new();
		
		Func<TestHost, float> function = parser.ParseDelegate<Func<TestHost, float>>("host.float(3.0f).max(1.0f).max(host.float(7.0f))", 
		                                                         new LambdaParameter<TestHost>("host"));
		float result = function(host);

		Assert.AreEqual(7.0f, result);
	}

	[Test, Parallelizable]
	public void StringVariableAndInterpolation() {
		ScriptParser parser = new();
		Func<string> function = parser.ParseDelegate<Func<string>>("$variable=\"\"\n" +
		                                                         "$variable+=$\"Nr {5} lives ...\"\n" +
		                                                         "return($variable)"
		                                                        );
		string result = function();

		Assert.AreEqual("Nr 5 lives ...", result);
	}

	[Test, Parallelizable]
	public void StringAdd() {
		ScriptParser parser = new();
		Func<string> function = parser.ParseDelegate<Func<string>>("\"Hello \"+\"World\"");
		Assert.AreEqual("Hello World", function());
	}

	[Test, Parallelizable]
	public void AddDifferentTypes() {
		ScriptParser parser = new();
		Func<float> function=parser.ParseDelegate<Func<float>>("3.14f+7");
		Assert.AreEqual(10.14f, function());
	}

	[Test, Parallelizable]
	public void SwitchDefault() {
		ScriptParser parser = new();
		Func<object, int> function=parser.ParseDelegate<Func<object,int>>("switch($number) case(1) 0 case(2) 3 default 9",
		                                                            new LambdaParameter<object>("number"));
		Assert.AreEqual(9, function(78));
	}

	[Test, Parallelizable]
	public void SwitchWithoutDefault() {
		ScriptParser parser = new();
		Func<int, int> function=parser.ParseDelegate<Func<int,int>>("switch($number) case(1) 0 case(2) 3",
		                                                   new LambdaParameter<int>("number"));
		Assert.AreEqual(0, function(78));
	}
	
	[Test, Parallelizable]
	public void SwitchWithoutDefaultHitCase() {
		ScriptParser parser = new();
		Func<int, int> function=parser.ParseDelegate<Func<int,int>>("switch($number) case(1) 0 case(2) 3",
		                                                            new LambdaParameter<int>("number"));
		Assert.AreEqual(3, function(2));
	}

	[Test, Parallelizable]
	public void IfThenElse() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("if($number>5) return(7) else return(2)",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(7, function(10));
	}

	[Test, Parallelizable]
	public void IfThenElseImplicitely() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("if($number>5) 7 else 2",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(7, function(10));
	}

	[Test, Parallelizable]
	public void IfThenElseImplicitelyHitElse() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("if($number>5) 7 else 2",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(2, function(3));
	}

	[Test, Parallelizable]
	public void IfThenElseHitElse() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("if($number>5) return(7) else return(2)",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(2, function(3));
	}
	
	[Test, Parallelizable]
	public void Try() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("try { number / 0 } 9",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(9, function(3));
	}
	
	[Test, Parallelizable]
	public void TryCatch() {
		ScriptParser parser = new();
		Func<int, int> function = parser.ParseDelegate<Func<int, int>>("try { number / 0 } catch { number/3 }",
		                                                               new LambdaParameter<int>("number"));
		Assert.AreEqual(1, function(3));
	}

	[Test, Parallelizable]
	public void AutoConvertTypedLambda() {
		ScriptParser parser = new();
		Func<DayOfWeek> function = parser.ParseDelegate<Func<DayOfWeek>>("6");
		Assert.AreEqual(DayOfWeek.Saturday, function());
	}

	[Test, Parallelizable]
	public void AccessDictionaryItemUsingPropertySyntax() {
		ScriptParser parser = new();
		Func<Dictionary<string, string>, string> function = parser.ParseDelegate<Func<Dictionary<string, string>, string>>("dic.Affe",
		                                                                                                                   new LambdaParameter<Dictionary<string, string>>("dic"));
		Assert.AreEqual("Mensch", function(new() {
			{ "Affe", "Mensch" }
		}));
	}

	[Test, Parallelizable]
	public void AccessIDictionaryItemUsingPropertySyntax() {
		ScriptParser parser = new();
		Func<IDictionary<string, object>, string> function = parser.ParseDelegate<Func<IDictionary<string, object>, string>>("dic.Affe",
		                                                                                                                   new LambdaParameter<IDictionary<string, object>>("dic"));
		Assert.AreEqual("Mensch", function(new Dictionary<string, object> {
			{ "Affe", "Mensch" }
		}));
	}

	[Test, Parallelizable]
	public void AssignDictionaryItemUsingPropertySyntax() {
		ScriptParser parser = new();
		Dictionary<string, string> dic = new();
		Func<Dictionary<string, string>, string> function = parser.ParseDelegate<Func<Dictionary<string, string>, string>>("dic.Affe=\"Mensch\"",
		                                                                                                                   new LambdaParameter<Dictionary<string, string>>("dic"));

		function(dic);
		Assert.AreEqual("Mensch", dic["Affe"]);
	}

}