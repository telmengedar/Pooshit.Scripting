using System;

namespace Pooshit.Scripting.Expressions;

/// <summary>
/// parameter for building lambda expressions from script
/// </summary>
public class LambdaParameter {
		
	/// <summary>
	/// creates a new <see cref="LambdaParameter"/>
	/// </summary>
	/// <param name="name">name of parameter</param>
	/// <param name="type">parameter type</param>
	public LambdaParameter(string name, Type type) {
		Name = name;
		Type = type;
	}

	/// <summary>
	/// name of parameter
	/// </summary>
	public string Name { get; }
		
	/// <summary>
	/// parameter type
	/// </summary>
	public Type Type { get; }
}

/// <summary>
/// parameter for building lambda expressions from script
/// </summary>
public class LambdaParameter<T> : LambdaParameter {
		
	/// <summary>
	/// creates a new <see cref="LambdaParameter{T}"/>
	/// </summary>
	/// <param name="name">name of parameter</param>
	public LambdaParameter(string name) : base(name, typeof(T)) { }
}