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

	public ExpressionBuilder() {
		converter = typeof(Converter).GetMethods(BindingFlags.Public | BindingFlags.Static)
		                             .First(m => m.Name == "Convert" && m.IsGenericMethodDefinition);
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="extensions">available extensions</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, IExtensionProvider extensions, IDictionary<string, object> variables) {
		return BuildExpression(script, extensions, variables.Select(v => new LambdaParameter(v.Key, v.Value.GetType())).ToArray());
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="extensions">available extensions</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, IExtensionProvider extensions, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name)).ToArray();
		List<ParameterExpression> variablesXp = [..parameters];
		return Expression.Lambda(Build(script.Body, extensions, variablesXp, [], null, true), parameters);
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="extensions">available extensions</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public Expression<T> BuildExpression<T>(IScript script, IExtensionProvider extensions, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name)).ToArray();
		List<ParameterExpression> variablesXp = [..parameters];

		Expression body = Build(script.Body, extensions, variablesXp, [], null, true);
		if (typeof(T).IsGenericType && typeof(T).Name.StartsWith("Func")) {
			Type returnType = typeof(T).GetGenericArguments().Last();
			if (body.Type != returnType)
				body = Convert(body, returnType);
		}
		return Expression.Lambda<T>(body, parameters);
	}
	
	Expression Build(IScriptToken token, IExtensionProvider extensions, List<ParameterExpression> variables, List<LabelExpression> labels, Type typeHint=default, bool firstBlock=false) {
		if (token == null)
			return null;
		
		if (token is StatementBlock block) {
			List<ParameterExpression> newVariables = [..variables];
			Expression[] blockExpressions = block.Children.Select(c => Build(c, extensions, newVariables, labels)).ToArray();

			newVariables = [..newVariables.Except(variables)];
			if(firstBlock)
				return Expression.Block(newVariables, blockExpressions.Concat(labels));
			return Expression.Block(newVariables, blockExpressions);
		}

		if (token is Addition addition)
			return BinOpString(Build(addition.Lhs, extensions, variables, labels),
			                   Build(addition.Rhs, extensions, variables, labels),
			                   Expression.Add,
			                   false
			                  );
		if(token is Subtraction subtraction)
			return Expression.Subtract(Build(subtraction.Lhs, extensions, variables, labels), 
			                           Build(subtraction.Rhs, extensions, variables, labels));
		if(token is Multiplication multiply)
			return Expression.Multiply(Build(multiply.Lhs, extensions, variables, labels), 
			                           Build(multiply.Rhs, extensions, variables, labels));
		if(token is Division division)
			return Expression.Divide(Build(division.Lhs, extensions, variables, labels), 
			                         Build(division.Rhs, extensions, variables, labels));
		if(token is Modulo modulo)
			return Expression.Modulo(Build(modulo.Lhs, extensions, variables, labels), 
			                         Build(modulo.Rhs, extensions, variables, labels));
		if(token is ShiftLeft shiftLeft)
			return Expression.LeftShift(Build(shiftLeft.Lhs, extensions, variables, labels), 
			                            Build(shiftLeft.Rhs, extensions, variables, labels));
		if(token is ShiftRight shiftRight)
			return Expression.RightShift(Build(shiftRight.Lhs, extensions, variables, labels), 
			                             Build(shiftRight.Rhs, extensions, variables, labels));
		if(token is BitwiseAnd bitwiseAnd)
			return Expression.And(Build(bitwiseAnd.Lhs, extensions, variables, labels), 
			                      Build(bitwiseAnd.Rhs, extensions, variables, labels));
		if(token is BitwiseOr bitwiseOr)
			return Expression.Or(Build(bitwiseOr.Lhs, extensions, variables, labels), 
			                     Build(bitwiseOr.Rhs, extensions, variables, labels));
		if(token is BitwiseXor bitwiseXor)
			return Expression.ExclusiveOr(Build(bitwiseXor.Lhs, extensions, variables, labels), 
			                              Build(bitwiseXor.Rhs, extensions, variables, labels));
		if (token is AddAssign addAssign)
			return BinOpString(Build(addAssign.Lhs, extensions, variables, labels),
			                   Build(addAssign.Rhs, extensions, variables, labels),
			                   Expression.AddAssign,
			                   true
			                  );
		if(token is SubAssign subAssign)
			return Expression.SubtractAssign(Build(subAssign.Lhs, extensions, variables, labels), 
			                                 Build(subAssign.Rhs, extensions, variables, labels));
		if(token is MulAssign mulAssign)
			return Expression.MultiplyAssign(Build(mulAssign.Lhs, extensions, variables, labels), 
			                                 Build(mulAssign.Rhs, extensions, variables, labels));
		if(token is DivAssign divAssign)
			return Expression.DivideAssign(Build(divAssign.Lhs, extensions, variables, labels), 
			                               Build(divAssign.Rhs, extensions, variables, labels));
		if(token is ModAssign modAssign)
			return Expression.ModuloAssign(Build(modAssign.Lhs, extensions, variables, labels), 
			                               Build(modAssign.Rhs, extensions, variables, labels));
		if(token is AndAssign andAssign)
			return Expression.AndAssign(Build(andAssign.Lhs, extensions, variables, labels), 
			                            Build(andAssign.Rhs, extensions, variables, labels));
		if(token is OrAssign orAssign)
			return Expression.OrAssign(Build(orAssign.Lhs, extensions, variables, labels), 
			                           Build(orAssign.Rhs, extensions, variables, labels));
		if(token is XorAssign xorAssign)
			return Expression.ExclusiveOrAssign(Build(xorAssign.Lhs, extensions, variables, labels), 
			                                    Build(xorAssign.Rhs, extensions, variables, labels));
		if(token is ShlAssign shlAssign)
			return Expression.LeftShiftAssign(Build(shlAssign.Lhs, extensions, variables, labels), 
			                                  Build(shlAssign.Rhs, extensions, variables, labels));
		if(token is ShrAssign shrAssign)
			return Expression.RightShiftAssign(Build(shrAssign.Lhs, extensions, variables, labels), 
			                                   Build(shrAssign.Rhs, extensions, variables, labels));
		if (token is Assignment assignment) {
			Expression rhs = Build(assignment.Rhs, extensions, variables, labels);
			return Expression.Assign(Build(assignment.Lhs, extensions, variables, labels, rhs.Type),
			                         rhs);
		}

		if(token is Equal equal)
			return Expression.Equal(Build(equal.Lhs, extensions, variables, labels), 
			                        Build(equal.Rhs, extensions, variables, labels));
		if(token is NotEqual notEqual)
			return Expression.NotEqual(Build(notEqual.Lhs, extensions, variables, labels), 
			                           Build(notEqual.Rhs, extensions, variables, labels));
		if(token is Greater greater)
			return Expression.GreaterThan(Build(greater.Lhs, extensions, variables, labels), 
			                              Build(greater.Rhs, extensions, variables, labels));
		if(token is GreaterOrEqual greaterOrEqual)
			return Expression.GreaterThanOrEqual(Build(greaterOrEqual.Lhs, extensions, variables, labels), 
			                                     Build(greaterOrEqual.Rhs, extensions, variables, labels));
		if(token is Less less)
			return Expression.LessThan(Build(less.Lhs, extensions, variables, labels), 
			                           Build(less.Rhs, extensions, variables, labels));
		if(token is LessOrEqual lessOrEqual)
			return Expression.LessThanOrEqual(Build(lessOrEqual.Lhs, extensions, variables, labels), 
			                                  Build(lessOrEqual.Rhs, extensions, variables, labels));
		if(token is LogicAnd logicAnd)
			return Expression.AndAlso(Build(logicAnd.Lhs, extensions, variables, labels), 
			                          Build(logicAnd.Rhs, extensions, variables, labels));
		if(token is LogicOr logicOr)
			return Expression.OrElse(Build(logicOr.Lhs, extensions, variables, labels), 
			                         Build(logicOr.Rhs, extensions, variables, labels));
		if(token is Complement complement)
			return Expression.OnesComplement(Build(complement.Operand, extensions, variables, labels));
		if (token is Increment increment) {
			if(increment.IsPostToken)
				return Expression.PostIncrementAssign(Build(increment.Operand, extensions, variables, labels));
			return Expression.PreIncrementAssign(Build(increment.Operand, extensions, variables, labels));
		}
		if (token is Decrement decrement) {
			if(decrement.IsPostToken)
				return Expression.PostDecrementAssign(Build(decrement.Operand, extensions, variables, labels));
			return Expression.PreDecrementAssign(Build(decrement.Operand, extensions, variables, labels));
		}
		if(token is Negate negate)
			return Expression.Negate(Build(negate.Operand, extensions, variables, labels));
		if(token is Not not)
			return Expression.Not(Build(not.Operand, extensions, variables, labels));

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
			Expression indexHost = Build(indexer.Host, extensions, variables, labels);
			if (indexHost.Type.IsArray)
				return Expression.ArrayIndex(indexHost, indexer.Parameters.Select(p => Build(p, extensions, variables, labels)).ToArray());
			return Expression.Property(indexHost, "Item", indexer.Parameters.Select(p => Build(p, extensions, variables, labels)).ToArray());
		}

		if (token is ScriptMember member) {
			Expression host = Build(member.Host, extensions, variables, labels);
			if (typeof(IDictionary).IsAssignableFrom(host.Type) || 
			    host.Type.IsGenericType && host.Type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
			    host.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
				return Expression.Property(host, "Item", Expression.Constant(member.Member));
			return Expression.Property(host, member.Member);
		}

		if (token is ScriptMethod method)
			return BuildMethod(method, extensions, variables, labels);
		if (token is ScriptArray array) {
			Expression[] arrayValues = array.Values.Select(p => Build(p, extensions, variables, labels)).ToArray();
			return Expression.NewArrayInit(DetermineArrayType(arrayValues), arrayValues);
		}

		if (token is ArithmeticBlock arithmeticBlock)
			return Build(arithmeticBlock.InnerBlock, extensions, variables, labels);

		if (token is StringInterpolation stringInterpolation)
			return StringConcat(stringInterpolation.Children.Select(c => StringConvert(Build(c, extensions, variables, labels))).ToArray());

		if (token is Return returnToken) {
			Expression returnValue = Build(returnToken.Value, extensions, variables, labels);
			LabelExpression returnLabel = labels.FirstOrDefault();
			if (returnLabel == null) {
				returnLabel = Expression.Label(Expression.Label(returnValue.Type),
				                               Expression.Default(returnValue.Type));
				labels.Add(returnLabel);
			}

			return Expression.Return(returnLabel.Target, returnValue, returnValue.Type);
		}

		if (token is If ifToken) {
			Expression condition = Convert(Build(ifToken.Parameters.Single(), extensions, variables, labels), typeof(bool));
			Expression trueBranch = Build(ifToken.Body, extensions, variables, labels);
			if (ifToken.Else != null) {
				Expression falseBranch = Build(ifToken.Else.Body, extensions, variables, labels);
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
			Expression condition = Build(switchToken.Parameters.Single(), extensions, variables, labels);
			SwitchCase[] cases = switchToken.Cases.Select(c => Expression.SwitchCase(
			                                                                         Build(c.Body, extensions, variables, labels),
			                                                                         c.Parameters.Select(p => Convert(Build(p, extensions, variables, labels), condition.Type)).ToArray()
			                                                                        )).ToArray();
			Expression defaultBody = Build(switchToken.Default?.Body, extensions, variables, labels);
			if (defaultBody == null && cases.Length > 0 && cases[0].Body.Type != typeof(void))
				defaultBody = Expression.Default(cases[0].Body.Type);

			return Expression.Switch(condition,
			                         defaultBody,
			                         cases);
		}

		if (token is Try tryToken) {
			Expression tryBlock = Build(tryToken.Body, extensions, variables, labels);
			CatchBlock catchBlock = Expression.Catch(typeof(Exception), Build(tryToken.Catch?.Body, extensions, variables, labels) ?? Expression.Default(tryBlock.Type));
			return Expression.TryCatch(tryBlock, catchBlock);
		}

		if (token is TypeToken type)
			return Expression.Constant(type.Type);
		
		throw new NotSupportedException(token.GetType().Name);
	}

	Expression Convert(Expression expression, Type type) {
		if (expression.Type == type || type.IsAssignableFrom(expression.Type) && type!=typeof(object) && !type.IsNullable())
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
	
	Expression BuildMethod(ScriptMethod method, IExtensionProvider extensions, List<ParameterExpression> variables, List<LabelExpression> labels) {
		Expression host = Build(method.Host, extensions, variables, labels);
		Expression[] scriptParameters = method.Parameters.Select(p => Build(p, extensions, variables, labels))
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