using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Pooshit.Scripting.Control;
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
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name))
		                                            .ToArray();
		return Expression.Lambda(Build(script.Body, extensions, parameters), parameters);
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="extensions">available extensions</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public Expression<T> BuildExpression<T>(IScript script, IExtensionProvider extensions, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name))
		                                            .ToArray();
		return Expression.Lambda<T>(Build(script.Body, extensions, parameters), parameters);
	}

	Expression Build(IScriptToken token, IExtensionProvider extensions, ParameterExpression[] variables) {
		if (token is StatementBlock block)
			return Expression.Block(block.Children.Select(c=>Build(c, extensions, variables)));
		if(token is Addition addition)
			return Expression.Add(Build(addition.Lhs, extensions, variables), Build(addition.Rhs, extensions, variables));
		if(token is Subtraction subtraction)
			return Expression.Subtract(Build(subtraction.Lhs, extensions, variables), Build(subtraction.Rhs, extensions, variables));
		if(token is Multiplication multiply)
			return Expression.Multiply(Build(multiply.Lhs, extensions, variables), Build(multiply.Rhs, extensions, variables));
		if(token is Division division)
			return Expression.Divide(Build(division.Lhs, extensions, variables), Build(division.Rhs, extensions, variables));
		if(token is Modulo modulo)
			return Expression.Modulo(Build(modulo.Lhs, extensions, variables), Build(modulo.Rhs, extensions, variables));
		if(token is ShiftLeft shiftLeft)
			return Expression.LeftShift(Build(shiftLeft.Lhs, extensions, variables), Build(shiftLeft.Rhs, extensions, variables));
		if(token is ShiftRight shiftRight)
			return Expression.RightShift(Build(shiftRight.Lhs, extensions, variables), Build(shiftRight.Rhs, extensions, variables));
		if(token is BitwiseAnd bitwiseAnd)
			return Expression.And(Build(bitwiseAnd.Lhs, extensions, variables), Build(bitwiseAnd.Rhs, extensions, variables));
		if(token is BitwiseOr bitwiseOr)
			return Expression.Or(Build(bitwiseOr.Lhs, extensions, variables), Build(bitwiseOr.Rhs, extensions, variables));
		if(token is BitwiseXor bitwiseXor)
			return Expression.ExclusiveOr(Build(bitwiseXor.Lhs, extensions, variables), Build(bitwiseXor.Rhs, extensions, variables));
		if(token is AddAssign addAssign)
			return Expression.AddAssign(Build(addAssign.Lhs, extensions, variables), Build(addAssign.Rhs, extensions, variables));
		if(token is SubAssign subAssign)
			return Expression.SubtractAssign(Build(subAssign.Lhs, extensions, variables), Build(subAssign.Rhs, extensions, variables));
		if(token is MulAssign mulAssign)
			return Expression.MultiplyAssign(Build(mulAssign.Lhs, extensions, variables), Build(mulAssign.Rhs, extensions, variables));
		if(token is DivAssign divAssign)
			return Expression.DivideAssign(Build(divAssign.Lhs, extensions, variables), Build(divAssign.Rhs, extensions, variables));
		if(token is ModAssign modAssign)
			return Expression.ModuloAssign(Build(modAssign.Lhs, extensions, variables), Build(modAssign.Rhs, extensions, variables));
		if(token is AndAssign andAssign)
			return Expression.AndAssign(Build(andAssign.Lhs, extensions, variables), Build(andAssign.Rhs, extensions, variables));
		if(token is OrAssign orAssign)
			return Expression.OrAssign(Build(orAssign.Lhs, extensions, variables), Build(orAssign.Rhs, extensions, variables));
		if(token is XorAssign xorAssign)
			return Expression.ExclusiveOrAssign(Build(xorAssign.Lhs, extensions, variables), Build(xorAssign.Rhs, extensions, variables));
		if(token is ShlAssign shlAssign)
			return Expression.LeftShiftAssign(Build(shlAssign.Lhs, extensions, variables), Build(shlAssign.Rhs, extensions, variables));
		if(token is ShrAssign shrAssign)
			return Expression.RightShiftAssign(Build(shrAssign.Lhs, extensions, variables), Build(shrAssign.Rhs, extensions, variables));
		if(token is Assignment assignment)
			return Expression.Assign(Build(assignment.Lhs, extensions, variables), Build(assignment.Rhs, extensions, variables));
		if(token is Equal equal)
			return Expression.Equal(Build(equal.Lhs, extensions, variables), Build(equal.Rhs, extensions, variables));
		if(token is NotEqual notEqual)
			return Expression.NotEqual(Build(notEqual.Lhs, extensions, variables), Build(notEqual.Rhs, extensions, variables));
		if(token is Greater greater)
			return Expression.GreaterThan(Build(greater.Lhs, extensions, variables), Build(greater.Rhs, extensions, variables));
		if(token is GreaterOrEqual greaterOrEqual)
			return Expression.GreaterThanOrEqual(Build(greaterOrEqual.Lhs, extensions, variables), Build(greaterOrEqual.Rhs, extensions, variables));
		if(token is Less less)
			return Expression.LessThan(Build(less.Lhs, extensions, variables), Build(less.Rhs, extensions, variables));
		if(token is LessOrEqual lessOrEqual)
			return Expression.LessThanOrEqual(Build(lessOrEqual.Lhs, extensions, variables), Build(lessOrEqual.Rhs, extensions, variables));
		if(token is LogicAnd logicAnd)
			return Expression.AndAlso(Build(logicAnd.Lhs, extensions, variables), Build(logicAnd.Rhs, extensions, variables));
		if(token is LogicOr logicOr)
			return Expression.OrElse(Build(logicOr.Lhs, extensions, variables), Build(logicOr.Rhs, extensions, variables));
		if(token is Complement complement)
			return Expression.OnesComplement(Build(complement.Operand, extensions, variables));
		if (token is Increment increment) {
			if(increment.IsPostToken)
				return Expression.PostIncrementAssign(Build(increment.Operand, extensions, variables));
			return Expression.PreIncrementAssign(Build(increment.Operand, extensions, variables));
		}
		if (token is Decrement decrement) {
			if(decrement.IsPostToken)
				return Expression.PostDecrementAssign(Build(decrement.Operand, extensions, variables));
			return Expression.PreDecrementAssign(Build(decrement.Operand, extensions, variables));
		}
		if(token is Negate negate)
			return Expression.Negate(Build(negate.Operand, extensions, variables));
		if(token is Not not)
			return Expression.Not(Build(not.Operand, extensions, variables));

		if(token is ScriptValue value)
			return Expression.Constant(value.Value);

		if (token is ScriptVariable variable)
			return variables.First(v => v.Name == variable.Name);

		if (token is ScriptIndexer indexer) {
			Expression indexHost = Build(indexer.Host, extensions, variables);
			if (indexHost.Type.IsArray)
				return Expression.ArrayIndex(indexHost, indexer.Parameters.Select(p => Build(p, extensions, variables)).ToArray());
			return Expression.Property(indexHost, "Item", indexer.Parameters.Select(p => Build(p, extensions, variables)).ToArray());
		}

		if(token is ScriptMember member)
			return Expression.Property(Build(member.Host, extensions, variables), member.Member);
		if (token is ScriptMethod method)
			return BuildMethod(method, extensions, variables);
		if (token is ScriptArray array) {
			Expression[] arrayValues = array.Values.Select(p => Build(p, extensions, variables)).ToArray();
			return Expression.NewArrayInit(DetermineArrayType(arrayValues), arrayValues);
		}

		if (token is ArithmeticBlock arithmeticBlock)
			return Build(arithmeticBlock.InnerBlock, extensions, variables);
			
		throw new NotSupportedException(token.GetType().Name);
	}

	bool ParametersMatching(ParameterInfo[] parameters, Expression[] expressions) {
		if (parameters.Length - 1 != expressions.Length)
			return false;

		for (int i = 0; i < expressions.Length; ++i)
			if (!parameters[i + 1].ParameterType.IsAssignableFrom(expressions[i].Type))
				return false;
		return true;
	}

	Expression BuildMethod(ScriptMethod method, IExtensionProvider extensions, ParameterExpression[] variables) {
		Expression host = Build(method.Host, extensions, variables);
		Expression[] parameters = method.Parameters.Select(p => Build(p, extensions, variables)).ToArray();
		
		if (host.Type.GetMethod(method.MethodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null)
			return Expression.Call(Build(method.Host, extensions, variables), method.MethodName, null, parameters);

		MethodInfo[] methods = extensions.GetExtensions(host.Type)
		                                 .Where(m => string.Compare(m.Name, method.MethodName, StringComparison.OrdinalIgnoreCase) == 0 && ParametersMatching(m.GetParameters(), parameters))
		                                 .ToArray();

		if (methods.Length == 0)
			throw new NotSupportedException($"Unable to find matching method '{method.MethodName}' on type '{host.Type.FullName}'.");

		return Expression.Call(null, methods[0], [host, ..method.Parameters.Select(p => Build(p, extensions, variables))]);
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