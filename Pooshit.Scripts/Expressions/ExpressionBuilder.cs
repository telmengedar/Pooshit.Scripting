using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Extensions;
using Pooshit.Scripting.Extern;
using Pooshit.Scripting.Operations.Assign;
using Pooshit.Scripting.Operations.Comparision;
using Pooshit.Scripting.Operations.Logic;
using Pooshit.Scripting.Operations.Unary;
using Pooshit.Scripting.Operations.Values;
using Pooshit.Scripting.Parser;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Expressions;

/// <summary>
/// builds delegate expressions from scripts
/// </summary>
public class ExpressionBuilder {
	readonly Type[] typeOrder = [
		typeof(object),
		typeof(bool),
		typeof(char),
		typeof(byte),
		typeof(sbyte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(decimal),
		typeof(string)
	];

	readonly MethodInfo converter;

	readonly IExtensionProvider extensions;
	
	/// <summary>
	/// creates a new <see cref="ExpressionBuilder"/>
	/// </summary>
	/// <param name="extensions">available extension methods</param>
	public ExpressionBuilder(IExtensionProvider extensions) {
		this.extensions = extensions;
		converter = typeof(Converter).GetMethods(BindingFlags.Public | BindingFlags.Static)
		                             .First(m => m.Name == "Convert" && m.IsGenericMethodDefinition);
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, IDictionary<string, object> variables) {
		return BuildExpression(script, 
		                       variables.Select(v => new LambdaParameter(v.Key, v.Value.GetType())).ToArray());
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name)).ToArray();
		List<ParameterExpression> variablesXp = [..parameters];
		return Expression.Lambda(BuildMainBlock(script.Body, variablesXp, typeof(object)), parameters);
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public Expression<T> BuildExpression<T>(IScript script, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name)).ToArray();
		List<ParameterExpression> variablesXp = [..parameters];

		Type returnType = typeof(object);
		if (typeof(T).IsGenericType && typeof(T).Name.StartsWith("Func"))
			returnType = typeof(T).GetGenericArguments().Last();
		
		Expression body = BuildMainBlock(script.Body, variablesXp, returnType);
		if (body.Type != returnType)
			body = Convert(body, returnType);
		return Expression.Lambda<T>(body, parameters);
	}

	Expression BuildMainBlock(IScriptToken token, List<ParameterExpression> variables, Type returnType) {
		if (token is StatementBlock block) {
			returnType ??= typeof(object);
			Labels labels = new() {
				ReturnType = returnType
			};

			List<ParameterExpression> newVariables = [..variables];
			Expression[] statements = block.Children.Select(c => Build(c, newVariables, labels)).ToArray();
			Type blockType = statements.LastOrDefault()?.Type ?? typeof(void);
			
			newVariables=[..newVariables.Except(variables)];
			if (labels.ReturnLabel != null)
				return Expression.Block(blockType, newVariables, [..statements, labels.ReturnLabel]);
			return Expression.Block(blockType, newVariables, statements);
		}
		
		throw new NotSupportedException($"Unsupported expression type {returnType} as main block");
	}
	
	Expression Build(IScriptToken token, List<ParameterExpression> variables, Labels labels, Type typeHint=default) {
		if (token == null)
			return null;
		
		if (token is StatementBlock block) {
			List<ParameterExpression> newVariables = [..variables];
			Expression[] statements = block.Children.Select(c => Build(c, newVariables, labels)).ToArray();
			newVariables=[..newVariables.Except(variables)];
			return Expression.Block(newVariables, statements);

		}

		if (token is Addition addition)
			return BinOpString(Build(addition.Lhs, variables, labels),
			                   Build(addition.Rhs, variables, labels),
			                   Expression.Add,
			                   false
			                  );
		if(token is Subtraction subtraction)
			return Expression.Subtract(Build(subtraction.Lhs, variables, labels), 
			                           Build(subtraction.Rhs, variables, labels));
		if(token is Multiplication multiply)
			return Expression.Multiply(Build(multiply.Lhs, variables, labels), 
			                           Build(multiply.Rhs, variables, labels));
		if(token is Division division)
			return Expression.Divide(Build(division.Lhs, variables, labels), 
			                         Build(division.Rhs, variables, labels));
		if(token is Modulo modulo)
			return Expression.Modulo(Build(modulo.Lhs, variables, labels), 
			                         Build(modulo.Rhs, variables, labels));
		if(token is ShiftLeft shiftLeft)
			return Expression.LeftShift(Build(shiftLeft.Lhs, variables, labels), 
			                            Build(shiftLeft.Rhs, variables, labels));
		if(token is ShiftRight shiftRight)
			return Expression.RightShift(Build(shiftRight.Lhs, variables, labels), 
			                             Build(shiftRight.Rhs, variables, labels));
		if(token is BitwiseAnd bitwiseAnd)
			return Expression.And(Build(bitwiseAnd.Lhs, variables, labels), 
			                      Build(bitwiseAnd.Rhs, variables, labels));
		if(token is BitwiseOr bitwiseOr)
			return Expression.Or(Build(bitwiseOr.Lhs, variables, labels), 
			                     Build(bitwiseOr.Rhs, variables, labels));
		if(token is BitwiseXor bitwiseXor)
			return Expression.ExclusiveOr(Build(bitwiseXor.Lhs, variables, labels), 
			                              Build(bitwiseXor.Rhs, variables, labels));
		if (token is AddAssign addAssign)
			return BinOpString(Build(addAssign.Lhs, variables, labels),
			                   Build(addAssign.Rhs, variables, labels),
			                   Expression.AddAssign,
			                   true
			                  );
		if(token is SubAssign subAssign)
			return Expression.SubtractAssign(Build(subAssign.Lhs, variables, labels), 
			                                 Build(subAssign.Rhs, variables, labels));
		if(token is MulAssign mulAssign)
			return Expression.MultiplyAssign(Build(mulAssign.Lhs, variables, labels), 
			                                 Build(mulAssign.Rhs, variables, labels));
		if(token is DivAssign divAssign)
			return Expression.DivideAssign(Build(divAssign.Lhs, variables, labels), 
			                               Build(divAssign.Rhs, variables, labels));
		if(token is ModAssign modAssign)
			return Expression.ModuloAssign(Build(modAssign.Lhs, variables, labels), 
			                               Build(modAssign.Rhs, variables, labels));
		if(token is AndAssign andAssign)
			return Expression.AndAssign(Build(andAssign.Lhs, variables, labels), 
			                            Build(andAssign.Rhs, variables, labels));
		if(token is OrAssign orAssign)
			return Expression.OrAssign(Build(orAssign.Lhs, variables, labels), 
			                           Build(orAssign.Rhs, variables, labels));
		if(token is XorAssign xorAssign)
			return Expression.ExclusiveOrAssign(Build(xorAssign.Lhs, variables, labels), 
			                                    Build(xorAssign.Rhs, variables, labels));
		if(token is ShlAssign shlAssign)
			return Expression.LeftShiftAssign(Build(shlAssign.Lhs, variables, labels), 
			                                  Build(shlAssign.Rhs, variables, labels));
		if(token is ShrAssign shrAssign)
			return Expression.RightShiftAssign(Build(shrAssign.Lhs, variables, labels), 
			                                   Build(shrAssign.Rhs, variables, labels));
		if (token is Assignment assignment) {
			Expression rhs = Build(assignment.Rhs, variables, labels);
			return Expression.Assign(Build(assignment.Lhs, variables, labels, rhs.Type),
			                         rhs);
		}

		if(token is Equal equal)
			return Expression.Equal(Build(equal.Lhs, variables, labels), 
			                        Build(equal.Rhs, variables, labels));
		if(token is NotEqual notEqual)
			return Expression.NotEqual(Build(notEqual.Lhs, variables, labels), 
			                           Build(notEqual.Rhs, variables, labels));
		if(token is Greater greater)
			return Expression.GreaterThan(Build(greater.Lhs, variables, labels), 
			                              Build(greater.Rhs, variables, labels));
		if(token is GreaterOrEqual greaterOrEqual)
			return Expression.GreaterThanOrEqual(Build(greaterOrEqual.Lhs, variables, labels), 
			                                     Build(greaterOrEqual.Rhs, variables, labels));
		if(token is Less less)
			return Expression.LessThan(Build(less.Lhs, variables, labels), 
			                           Build(less.Rhs, variables, labels));
		if(token is LessOrEqual lessOrEqual)
			return Expression.LessThanOrEqual(Build(lessOrEqual.Lhs, variables, labels), 
			                                  Build(lessOrEqual.Rhs, variables, labels));
		if(token is LogicAnd logicAnd)
			return Expression.AndAlso(Build(logicAnd.Lhs, variables, labels), 
			                          Build(logicAnd.Rhs, variables, labels));
		if(token is LogicOr logicOr)
			return Expression.OrElse(Build(logicOr.Lhs, variables, labels), 
			                         Build(logicOr.Rhs, variables, labels));
		if(token is Complement complement)
			return Expression.OnesComplement(Build(complement.Operand, variables, labels));
		if (token is Increment increment) {
			if(increment.IsPostToken)
				return Expression.PostIncrementAssign(Build(increment.Operand, variables, labels));
			return Expression.PreIncrementAssign(Build(increment.Operand, variables, labels));
		}
		if (token is Decrement decrement) {
			if(decrement.IsPostToken)
				return Expression.PostDecrementAssign(Build(decrement.Operand, variables, labels));
			return Expression.PreDecrementAssign(Build(decrement.Operand, variables, labels));
		}
		if(token is Negate negate)
			return Expression.Negate(Build(negate.Operand, variables, labels));
		if(token is Not not)
			return Expression.Not(Build(not.Operand, variables, labels));

		if(token is ScriptValue value)
			return Expression.Constant(value.Value);

		if (token is ScriptVariable variable) {
			ParameterExpression parameter = variables.FirstOrDefault(v => v.Name == variable.Name);
			if (parameter != null)
				return parameter;
			
			ParameterExpression declaration = Expression.Variable(typeHint ?? typeof(object), variable.Name);
			variables.Add(declaration);
			return declaration;
		}

		if (token is ScriptIndexer indexer) {
			Expression indexHost = Build(indexer.Host, variables, labels);
			if (indexHost.Type.IsArray)
				return Expression.ArrayIndex(indexHost, indexer.Parameters.Select(p => Build(p, variables, labels)).ToArray());
			return Expression.Property(indexHost, "Item", indexer.Parameters.Select(p => Build(p, variables, labels)).ToArray());
		}

		if (token is ScriptMember member) {
			Expression host = Build(member.Host, variables, labels);
			if (typeof(IDictionary).IsAssignableFrom(host.Type) || 
			    host.Type.IsGenericType && host.Type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
			    host.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
				return Expression.Property(host, "Item", Expression.Constant(member.Member));
			return Expression.Property(host, member.Member);
		}

		if (token is ScriptMethod method)
			return BuildMethod(method, variables, labels);
		if (token is ScriptArray array) {
			Expression[] arrayValues = array.Values.Select(p => Build(p, variables, labels)).ToArray();
			return Expression.NewArrayInit(DetermineArrayType(arrayValues), arrayValues);
		}

		if (token is ArithmeticBlock arithmeticBlock)
			return Build(arithmeticBlock.InnerBlock, variables, labels);

		if (token is StringInterpolation stringInterpolation)
			return StringConcat(stringInterpolation.Children.Select(c => StringConvert(Build(c, variables, labels))).ToArray());

		if (token is Return returnToken) {
			labels.ReturnLabel ??= Expression.Label(Expression.Label(labels.ReturnType),
			                                        Expression.Default(labels.ReturnType));
			Expression returnValue = Build(returnToken.Value, variables, labels);
			return Expression.Return(labels.ReturnLabel.Target,
			                         Convert(returnValue, labels.ReturnType),
			                         labels.ReturnType);
		}

		if (token is If ifToken) {
			Expression condition = Convert(Build(ifToken.Parameters.Single(), variables, labels), typeof(bool));
			Expression trueBranch = Build(ifToken.Body, variables, labels);
			if (ifToken.Else != null) {
				Expression falseBranch = Build(ifToken.Else.Body, variables, labels);
				if (falseBranch.Type == trueBranch.Type)
					return Expression.Condition(condition, trueBranch, falseBranch, trueBranch.Type);
				return Expression.Condition(condition, Convert(trueBranch, typeof(object)), Convert(falseBranch, typeof(object)), typeof(object));
			}

			return Expression.Condition(condition,
			                            trueBranch,
			                            Expression.Default(trueBranch.Type),
			                            trueBranch.Type);
		}
		
		if (token is Switch switchToken) {
			Expression condition = Build(switchToken.Parameters.Single(), variables, labels);
			SwitchCase[] cases = switchToken.Cases.Select(c => Expression.SwitchCase(
			                                                                         Build(c.Body, variables, labels),
			                                                                         c.Parameters.Select(p => Convert(Build(p, variables, labels), condition.Type)).ToArray()
			                                                                        )).ToArray();
			Expression defaultBody = Build(switchToken.Default?.Body, variables, labels);
			if (defaultBody == null && cases.Length > 0 && cases[0].Body.Type != typeof(void))
				defaultBody = Expression.Default(cases[0].Body.Type);

			return Expression.Switch(condition,
			                         defaultBody,
			                         cases);
		}

		if (token is Try tryToken) {
			Expression tryBlock = Build(tryToken.Body, variables, labels);
			CatchBlock catchBlock = Expression.Catch(typeof(Exception), Build(tryToken.Catch?.Body, variables, labels) ?? Expression.Default(tryBlock.Type));
			return Expression.TryCatch(tryBlock, catchBlock);
		}

		if (token is TypeToken type)
			return Expression.Constant(type.Type);

		if (token is While whileToken) {
			LabelTarget loopEnd = Expression.Label("label" + labels.LabelCounter++);
			Expression condition = Expression.IfThen(Expression.Not(Convert(Build(whileToken.Parameters.First(), variables, labels), typeof(bool))),
			                                         Expression.Break(loopEnd));
			Expression body = Build(whileToken.Body, variables, labels);
			return Expression.Loop(Expression.Block(condition, body), loopEnd);
		}

		if (token is For forToken) {
			LabelTarget loopEnd = Expression.Label("label" + labels.LabelCounter++);
			Expression init = Build(forToken.Parameters.First(), variables, labels);
			Expression condition = Expression.IfThen(Expression.Not(Convert(Build(forToken.Parameters.Skip(1).First(), variables, labels), typeof(bool))),
			                                         Expression.Break(loopEnd));
			return Expression.Block(init,
			                        Expression.Loop(Expression.Block(condition,
			                                                         Build(forToken.Body, variables, labels),
			                                                         Build(forToken.Parameters.Skip(2).First(), variables, labels)),
			                                        loopEnd));
		}

		if (token is Foreach foreachToken) {
			Expression collection = Build(foreachToken.Collection, variables, labels);
			return ForEach(collection,
			               Build(foreachToken.Iterator, variables, labels, collection.Type.GetElementType()) as ParameterExpression,
			               Build(foreachToken.Body, variables, labels),
			               labels);
		}
		
		throw new NotSupportedException(token.GetType().Name);
	}

	static Expression ForEach(Expression collection, ParameterExpression loopVar, Expression loopContent, Labels labels)
	{
		Type elementType = loopVar.Type;
		Type enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
		Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

		ParameterExpression enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
		MethodCallExpression getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
		BinaryExpression enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

		// The MoveNext method's actually on IEnumerator, not IEnumerator<T>
		MethodCallExpression moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

		LabelTarget breakLabel = Expression.Label("label" + labels.LabelCounter++);

		return Expression.Block([enumeratorVar],
		                        enumeratorAssign,
		                        Expression.Loop(
		                                        Expression.IfThenElse(
		                                                              Expression.Equal(moveNextCall, Expression.Constant(true)),
		                                                              Expression.Block([loopVar],
		                                                                               Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
		                                                                               loopContent
		                                                                              ),
		                                                              Expression.Break(breakLabel)
		                                                             ),
		                                        breakLabel)
		                       );
	}
	Expression Convert(Expression expression, Type type) {
		if (expression.Type == type || type.IsAssignableFrom(expression.Type) && type != typeof(object) && !type.IsNullable())
			return expression;

		return Expression.Call(null, converter.MakeGenericMethod(type), Expression.Convert(expression, typeof(object)), Expression.Constant(true));
	}

	Expression StringConcat(params Expression[] expressions) {
		return Expression.Call(typeof(string).GetMethod("Concat", [typeof(string[])]),
		                       Expression.NewArrayInit(typeof(string), expressions));
	}
	
	Expression StringConvert(Expression expression) {
		if (expression.Type == typeof(string))
			return expression;

		return Expression.Call(converter.MakeGenericMethod(typeof(string)),
		                       Expression.Convert(expression, typeof(object)),
		                       Expression.Constant(true));
	}

	Expression BinOpString(Expression lhs, Expression rhs, Func<Expression, Expression, Expression> op, bool assign) {
		if (lhs.Type == typeof(string) || rhs.Type == typeof(string)) {
			if (lhs.Type != typeof(string)) {
				if (assign)
					throw new InvalidOperationException($"Add Assign not possible for {lhs.Type} and {rhs.Type}");
				lhs = StringConvert(lhs);
			}

			if (rhs.Type != typeof(string))
				rhs = StringConvert(rhs);

			if (assign)
				return Expression.Assign(lhs, StringConcat(lhs, rhs));
			return StringConcat(lhs, rhs);
		}

		return ArithmeticBinOp(lhs, rhs, op);
	}
	
	Expression ArithmeticBinOp(Expression lhs, Expression rhs, Func<Expression, Expression, Expression> op) {
		if (lhs.Type == rhs.Type)
			return op(lhs, rhs);

		int lhsIndex = Array.IndexOf(typeOrder, lhs.Type);
		int rhsIndex = Array.IndexOf(typeOrder, rhs.Type);
		if (lhsIndex <= 0 || rhsIndex <= 0)
			// just try anyways, there might be a custom operator
			return op(lhs, rhs);

		Type targetType = typeOrder[Math.Max(lhsIndex, rhsIndex)];
		if (lhs.Type != targetType)
			lhs = Expression.Call(converter.MakeGenericMethod(targetType), Expression.Convert(lhs, typeof(object)), Expression.Constant(true));
		if (rhs.Type != targetType)
			rhs = Expression.Call(converter.MakeGenericMethod(targetType), Expression.Convert(rhs, typeof(object)), Expression.Constant(true));

		return op(lhs, rhs);
	}
	
	int GetMatchScore(MethodInfo method, Expression[] parameters) {
		ParameterInfo[] methodParameters = method.GetParameters();
		int score = 0;
		for (int i = 0; i < parameters.Length; ++i) {
			int parameterIndex = Array.IndexOf(typeOrder, parameters[i].Type);
			int methodParameterIndex = Array.IndexOf(typeOrder, methodParameters[i].ParameterType);
			if (parameterIndex == -1 || methodParameterIndex == -1)
				score += 500;
			else score += Math.Abs(parameterIndex - methodParameterIndex);
		}

		score <<= 1;
		score += methodParameters.Length - parameters.Length;
		return score;
	}

	IEnumerable<MethodInfo> GetMatchingMethods(IEnumerable<MethodInfo> methods, string name, Expression[] scriptParameters, Type[] genericParameters, bool isExtension) {
		foreach (MethodInfo method in methods) {
			if (string.Compare(name, method.Name, StringComparison.OrdinalIgnoreCase) != 0)
				continue;
			
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Count(p => !p.IsOptional) + (isExtension ? -1 : 0) != scriptParameters.Length)
				continue;

			if (method.IsGenericMethodDefinition) {
				if (method.GetGenericArguments().Length != genericParameters.Length)
					continue;
			}
			else if (genericParameters.Length > 0)
				continue;
			
			yield return method;
		}
	}
	
	Expression BuildMethod(ScriptMethod method, List<ParameterExpression> variables, Labels labels) {
		Expression host = Build(method.Host, variables, labels);
		Expression[] scriptParameters = method.Parameters.Select(p => Build(p, variables, labels))
		                                      .ToArray();
		Type[] genericScriptParameters = method.GenericParameters?.Cast<TypeToken>().Select(p => p.Type).ToArray() ?? [];
		MethodInfo methodInfo = GetMatchingMethods(host.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance), 
		                                           method.MethodName,
		                                           scriptParameters,
		                                           genericScriptParameters,
		                                           false)
		                        .OrderBy(m => GetMatchScore(m, scriptParameters))
		                        .FirstOrDefault();

		if (methodInfo == null) {
			methodInfo = GetMatchingMethods(extensions.GetExtensions(method.MethodName),
			                                method.MethodName,
			                                scriptParameters,
			                                genericScriptParameters,
			                                true)
			             .OrderBy(m => GetMatchScore(m, scriptParameters))
			             .FirstOrDefault();
			scriptParameters = [host, ..scriptParameters];
		}

		if (methodInfo == null)
			throw new NotSupportedException($"Unable to find matching method '{method.MethodName}' on type '{host.Type.FullName}'.");

		if (methodInfo.IsGenericMethodDefinition)
			methodInfo = methodInfo.MakeGenericMethod(genericScriptParameters);
		
		return Expression.Call(methodInfo.IsStatic?null:host,
		                       methodInfo,
		                       GenerateParameters(scriptParameters, methodInfo.GetParameters()).ToArray());
	}
	
	IEnumerable<Expression> GenerateParameters(Expression[] sourceParameters, ParameterInfo[] targetParameters) {
		for (int i = 0; i < targetParameters.Length; ++i) {
			if (i >= sourceParameters.Length)
				yield return Expression.Constant(targetParameters[i].DefaultValue, targetParameters[i].ParameterType);
			else yield return Convert(sourceParameters[i], targetParameters[i].ParameterType);
		}
	}

	Type DetermineArrayType(Expression[] arrayValues) {
		if(arrayValues.Length == 0)
			return typeof(object);

		Type firstType = arrayValues[0].Type;
		if (arrayValues.Skip(1).All(v => v.Type == firstType))
			return firstType;
		return typeof(object);
	}
}