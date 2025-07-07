using System;
using System.Linq.Expressions;

namespace Pooshit.Scripting.Expressions;

/// <summary>
/// labels used in expression trees
/// </summary>
public class Labels {

	/// <summary>
	/// type of expected return value
	/// </summary>
	public Type ReturnType { get; set; }
	
	/// <summary>
	/// label used for return statements
	/// </summary>
	public LabelExpression ReturnLabel { get; set; }

	/// <summary>
	/// counts of custom labels
	/// </summary>
	public int LabelCounter { get; set; }
}