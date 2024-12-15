using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Pooshit.Scripting.Expressions;

namespace Pooshit.Scripting.Extensions;

/// <summary>
/// extensions used to convert scripts to expressions
/// </summary>
public static class ExpressionExtensions {

	/// <summary>
	/// converts a script to a compiled lambda expression
	/// </summary>
	/// <param name="script">script to convert</param>
	/// <param name="parameters">parameters of script</param>
	/// <returns>compiled lambda expression</returns>
	public static Delegate ToDelegate(this IScript script, IDictionary<string, object> parameters) {
		LambdaExpression expression = new ExpressionBuilder().BuildExpression(script, parameters);
		return expression.Compile();
	}
		
	/// <summary>
	/// converts a script to a compiled lambda expression
	/// </summary>
	/// <param name="script">script to convert</param>
	/// <param name="parameters">parameters of script</param>
	/// <returns>compiled lambda expression</returns>
	public static Delegate ToDelegate(this IScript script, params LambdaParameter[] parameters) {
		LambdaExpression expression = new ExpressionBuilder().BuildExpression(script, parameters);
		return expression.Compile();
	}

}