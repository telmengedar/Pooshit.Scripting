using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Pooshit.Scripting.Control;
using Pooshit.Scripting.Operations.Assign;
using Pooshit.Scripting.Operations.Comparision;
using Pooshit.Scripting.Operations.Logic;
using Pooshit.Scripting.Operations.Unary;
using Pooshit.Scripting.Operations.Values;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Expressions;

public class ExpressionBuilder {

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, IDictionary<string, object> variables) {
		return BuildExpression(script, variables.Select(v => new LambdaParameter(v.Key, v.Value.GetType())).ToArray());
	}

	/// <summary>
	/// builds an expression from a script
	/// </summary>
	/// <param name="script">script from which to build expression</param>
	/// <param name="variables">variables from which to build parameters</param>
	/// <returns>build lambda expression</returns>
	public LambdaExpression BuildExpression(IScript script, params LambdaParameter[] variables) {
		ParameterExpression[] parameters = variables.Select(v => Expression.Parameter(v.Type, v.Name))
		                                            .ToArray();
		return Expression.Lambda(Build(script.Body, parameters), parameters);
	}

	Expression Build(IScriptToken token, ParameterExpression[] variables) {
		if (token is StatementBlock block)
			return Expression.Block(block.Children.Select(c=>Build(c, variables)));
		if(token is Addition addition)
			return Expression.Add(Build(addition.Lhs, variables), Build(addition.Rhs, variables));
		if(token is Subtraction subtraction)
			return Expression.Subtract(Build(subtraction.Lhs, variables), Build(subtraction.Rhs, variables));
		if(token is Multiplication multiply)
			return Expression.Multiply(Build(multiply.Lhs, variables), Build(multiply.Rhs, variables));
		if(token is Division division)
			return Expression.Divide(Build(division.Lhs, variables), Build(division.Rhs, variables));
		if(token is Modulo modulo)
			return Expression.Modulo(Build(modulo.Lhs, variables), Build(modulo.Rhs, variables));
		if(token is ShiftLeft shiftLeft)
			return Expression.LeftShift(Build(shiftLeft.Lhs, variables), Build(shiftLeft.Rhs, variables));
		if(token is ShiftRight shiftRight)
			return Expression.RightShift(Build(shiftRight.Lhs, variables), Build(shiftRight.Rhs, variables));
		if(token is BitwiseAnd bitwiseAnd)
			return Expression.And(Build(bitwiseAnd.Lhs, variables), Build(bitwiseAnd.Rhs, variables));
		if(token is BitwiseOr bitwiseOr)
			return Expression.Or(Build(bitwiseOr.Lhs, variables), Build(bitwiseOr.Rhs, variables));
		if(token is BitwiseXor bitwiseXor)
			return Expression.ExclusiveOr(Build(bitwiseXor.Lhs, variables), Build(bitwiseXor.Rhs, variables));
		if(token is AddAssign addAssign)
			return Expression.AddAssign(Build(addAssign.Lhs, variables), Build(addAssign.Rhs, variables));
		if(token is SubAssign subAssign)
			return Expression.SubtractAssign(Build(subAssign.Lhs, variables), Build(subAssign.Rhs, variables));
		if(token is MulAssign mulAssign)
			return Expression.MultiplyAssign(Build(mulAssign.Lhs, variables), Build(mulAssign.Rhs, variables));
		if(token is DivAssign divAssign)
			return Expression.DivideAssign(Build(divAssign.Lhs, variables), Build(divAssign.Rhs, variables));
		if(token is ModAssign modAssign)
			return Expression.ModuloAssign(Build(modAssign.Lhs, variables), Build(modAssign.Rhs, variables));
		if(token is AndAssign andAssign)
			return Expression.AndAssign(Build(andAssign.Lhs, variables), Build(andAssign.Rhs, variables));
		if(token is OrAssign orAssign)
			return Expression.OrAssign(Build(orAssign.Lhs, variables), Build(orAssign.Rhs, variables));
		if(token is XorAssign xorAssign)
			return Expression.ExclusiveOrAssign(Build(xorAssign.Lhs, variables), Build(xorAssign.Rhs, variables));
		if(token is ShlAssign shlAssign)
			return Expression.LeftShiftAssign(Build(shlAssign.Lhs, variables), Build(shlAssign.Rhs, variables));
		if(token is ShrAssign shrAssign)
			return Expression.RightShiftAssign(Build(shrAssign.Lhs, variables), Build(shrAssign.Rhs, variables));
		if(token is Assignment assignment)
			return Expression.Assign(Build(assignment.Lhs, variables), Build(assignment.Rhs, variables));
		if(token is Equal equal)
			return Expression.Equal(Build(equal.Lhs, variables), Build(equal.Rhs, variables));
		if(token is NotEqual notEqual)
			return Expression.NotEqual(Build(notEqual.Lhs, variables), Build(notEqual.Rhs, variables));
		if(token is Greater greater)
			return Expression.GreaterThan(Build(greater.Lhs, variables), Build(greater.Rhs, variables));
		if(token is GreaterOrEqual greaterOrEqual)
			return Expression.GreaterThanOrEqual(Build(greaterOrEqual.Lhs, variables), Build(greaterOrEqual.Rhs, variables));
		if(token is Less less)
			return Expression.LessThan(Build(less.Lhs, variables), Build(less.Rhs, variables));
		if(token is LessOrEqual lessOrEqual)
			return Expression.LessThanOrEqual(Build(lessOrEqual.Lhs, variables), Build(lessOrEqual.Rhs, variables));
		if(token is LogicAnd logicAnd)
			return Expression.AndAlso(Build(logicAnd.Lhs, variables), Build(logicAnd.Rhs, variables));
		if(token is LogicOr logicOr)
			return Expression.OrElse(Build(logicOr.Lhs, variables), Build(logicOr.Rhs, variables));
		if(token is Complement complement)
			return Expression.OnesComplement(Build(complement.Operand, variables));
		if (token is Increment increment) {
			if(increment.IsPostToken)
				return Expression.PostIncrementAssign(Build(increment.Operand, variables));
			return Expression.PreIncrementAssign(Build(increment.Operand, variables));
		}
		if (token is Decrement decrement) {
			if(decrement.IsPostToken)
				return Expression.PostDecrementAssign(Build(decrement.Operand, variables));
			return Expression.PreDecrementAssign(Build(decrement.Operand, variables));
		}
		if(token is Negate negate)
			return Expression.Negate(Build(negate.Operand, variables));
		if(token is Not not)
			return Expression.Not(Build(not.Operand, variables));

		if(token is ScriptValue value)
			return Expression.Constant(value.Value);

		if (token is ScriptVariable variable)
			return variables.First(v => v.Name == variable.Name);

		if (token is ScriptIndexer indexer) {
			Expression indexHost = Build(indexer.Host, variables);
			if (indexHost.Type.IsArray)
				return Expression.ArrayIndex(indexHost, indexer.Parameters.Select(p => Build(p, variables)).ToArray());
			return Expression.Property(indexHost, "Item", indexer.Parameters.Select(p => Build(p, variables)).ToArray());
		}

		if(token is ScriptMember member)
			return Expression.Property(Build(member.Host, variables), member.Member);
		if(token is ScriptMethod method)
			return Expression.Call(Build(method.Host, variables), method.MethodName, null, method.Parameters.Select(p=>Build(p, variables)).ToArray());
		if (token is ScriptArray array) {
			Expression[] arrayValues = array.Values.Select(p => Build(p, variables)).ToArray();
			return Expression.NewArrayInit(DetermineArrayType(arrayValues), arrayValues);
		}

		if (token is ArithmeticBlock arithmeticBlock)
			return Build(arithmeticBlock.InnerBlock, variables);
			
		throw new NotSupportedException(token.GetType().Name);
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