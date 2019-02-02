# NightlyCode.Scripting

[NuGet](https://www.nuget.org/packages/NightlyCode.Scripting)

Provides a parser for scripts which execute controlled scripts on a C# backend.

## Environment

To use the script engine effectively it is necessary to add types to instantiate or function providers of which to call methods. The script engine can be used without doing this but only a limited functionality is available then like computing expressions.

### Global Variables
The most simple way to extend functionality of the script engine is to provide instances with some functionality using global variables. These variables are read-only and every instance method can be called on that variable using it's keyword.

```
// HttpProvider is just an example for an thinkable instance and not available in library
IScriptParser parser=new ScriptParser(new Variable("http", new HttpProvider()))
IScript script=parser.Parse("http.get(\"http://www.google.de/\")");
script.Execute();
```

### Add Types
Added types can be instantiated using the **new** keyword and all instance methods and properties of this type are then available to be called or used in expressions.

```
IScriptParser parser=new ScriptParser();
parser.Types.AddType<DateTime>();
IScript script=parser.Parse("new datetime(2012,9,4)");
script.Execute();
```

### Extensions
Existing types can be extended with methods. For this a class containing static methods taking an instance of the type to extend as first parameter. Such a method can then be called like it would be an instance method of the extended type. Unlike c# extension methods, the class does not need to be static and the extension parameter does not need to be marked with a **this** keyword.

```
IScriptParser parser=new ScriptParser();
parser.Extensions.AddExtensions<StringExtensions>();
IScript script=parser.Parse("\"test\".beautify()");
script.Execute();
```

## Language

### Expressions

An expression is a single token or a series of tokens which result in a value. A token can be a literal, operator, method call, variable or basically anything which combined in a meaningful way results in a value.

### Operators

|Operator|Name|Description|
|-|-|-|
|~|Complement|Flips every bit of a numerical value|
|!|Not|Negates a boolean value|
|=|Assignment|Assigns the value of the right hand side expression to the left hand side token|
|==|Equals|Determines whether the left hand side is equal to the right hand side|
|!= or <>|Not Equals|Determines whether the left hand side is not equal to the right hand side|
|<|Less Than|Determines whether the left hand side value is less than the right hand side value|
|<=|Less or Equal|Determines whether the left hand side value is less than or equal to the right hand side value|
|>|Greater Than|Determines whether the left hand side value is greater than the right hand side value|
|>=|Greater or Equal|Determines whether the left hand side value is greater than or equal to the right hand side value|
|~~|Matches|Determines whether the left hand side value matches the regex pattern on the right hand side|
|!~|Not Matches|Determines whether the left hand side value does not match the regex pattern of the right hand side|
|+|Add|Adds left hand side and right hand side|
|-|Subtract|Subtracts the right hand side value from the left hand side|
|*|Multiply|Multiplies the left hand side value by the right hand side value|
|/|Division|Divides the left hand side value by the right hand side value|
|%|Modulo|Computes the remainder when dividing the left hand side value by the right hand side value|
|&|Bitwise And|Applies a bitwise and between left hand side and right hand side|
|\||Bitwise Or|Applies a bitwise or between left hand side and right hand side|
|^|Bitwise Xor|Applies a bitwise xor between left hand side and right hand side|
|<<|Shift Left|Shifts left the bits of the left hand side value by the number of steps specified by the right hand side value|
|>>|Shift Right|Shifts right the bits of the left hand side value by the number of steps specified by the right hand side value|
|<<<|Rol Left|Rolls left the bits of the left hand side value by the number of steps specified by the right hand side value|
|>>>|Rol Right|Rolls right the bits of the left hand side value by the number of steps specified by the right hand side value|
|&&|Logical And|Computes the logical and between the left hand side boolean and the right hand side boolean|
|\|\||Logical Or|Computes the logical or between the left hand side boolean and the right hand side boolean|
|^^|Logical Xor|Computes the logical xor between the left hand side boolean and the right hand side boolean|
|+=|Add Assign|Adds the right hand side to the left hand side storing the result in the left hand side|
|-=|Sub Assign|Subtracts the right hand side from the left hand side storing the result in the left hand side|
|*=|Mul Assign|Multiplicates the right hand side with the left hand side storing the result in the left hand side|
|/=|Div Assign|Divides the left hand side by the right hand side storing the result in the left hand side|
|%=|Mod Assign|Computes the remainder when dividing the left hand side by the right hand side storing the result in the left hand side|
|<<=|Shift Left Assign|Shifts left the left hand side by the number of steps indicated by the right hand side storing the result in the left hand side|
|>>=|Shift Right Assign|Shifts right the left hand side by the number of steps indicated by the right hand side storing the result in the left hand side|
|&=|And Assign|Applies a bitwise and between left hand side and right hand side storing the result in the left hand side|
|\|=|Or Assign|Applies a bitwise or between left hand side and right hand side storing the result in the left hand side|
|^=|Xor Assign|Applies a bitwise xor between left hand side and right hand side storing the result in the left hand side|
|++|Increment|Increments the token this operator is attached to by 1|
|--|Decrement|Decrements the token this operator is attached to by 1|

Note: Shift and Rol operations are executed in a bitwise and not in an arithmetic fashion. This differs in the way C# handles these operations.

### Control Flow Statements

#### If / Else

*If* allows to execute a statement body based on a condition and optionally allows to execute a statement body when the condition is not met.

**Parameters**

|Index|Type|Description|
|-|-|-|
|0|boolean|Condition based on which the corresponding body is executed|

```
$condition=true
if($condition)
	statement.execute()
else
	otherstatement.execute()
```

#### While

*While* executes a body until a condition evaluated to *false*

**Parameters**

|Index|Type|Description|
|-|-|-|
|0|boolean|Condition checked before every loop. The loop body is executed until this condition evaluates to *false*|

```
$condition=true
$counter=0
while($condition)
{
	if($counter++>5)
		$condition=false
}
```

#### For

*For* is like a *while* statement. It only adds an initializing statement and an statement which is executed after every loop.

**Parameters**

|Index|Type|Description|
|-|-|-|
|0|statement|Statement executed before the first loop run. This should usually be an variable initializer but can theoretically be any statement|
|1|boolean|Condition checked before every loop. The loop body is executed until this condition evaluates to *false*|
|2|statement|Statement executed after every loop run. This is usually used to increment or decrement a loop variable but can theoretically be any statement|

```
$array=[1,2,3,4,5,6,7]
$sum=0
for($i,$i<$array.length,++$i)
	$sum+=$array[$i]
```

#### Foreach

*Foreach* is used to iterate over items of a collection. Every item is assigned to a variable for which a loop body is executed.

**Parameters**

|Index|Type|Description|
|-|-|-|
|0|variable|Name of a variable which is used to contain the current item of the iteration|
|1|enumeration|An enumeration over which to iterate|

```
$sum=0
foreach($item,[1,2,3,4,5])
	$sum+=$item
```

#### Switch

*Switch* is used to execute one of multiple branches based on the value of an expression. Following the *switch* there needs to be at least one *case* or *default* statement for the *switch* to make sense. Unlike other in major languages cases in this scripting language can contain multiple expressions which would react similar to multiple case labels with fall through behavior in c#. Case bodies also need no break statement since there is no fallthrough behavior in this language. A break would take effect on a possible enclosing loop.

**Parameters**

|Index|Type|Description|
|-|-|-|
|0|object|expression which is compared to the branches|

```
$result=0
switch(rng.next(10))
	case(1,5,8)
		$result=1
	case(6)
		$result=2
	default*
		$result=0
```

#### Return

*Return* ends the execution of the script and returns a value. The expression following the return is evaluated to the returned value.

```
for($i=0,$i<5,++$i)
	if(rng.next(100)<3)
		return true
return false
```

#### Break

*Breaks* ends execution of enclosing loops. *Break* can optionally be called with a parameter specifying the loop depth to be *broken* out of.

```
while(true)
{
	if(rng.next(5)>3)
		break
}
```

```
for($i=0,$i<100,++$i)
{
	for($k=0,$k<5,++$k)
	{
		if(rng.next(5)>3)
			break(2)
	}
}
```

#### Continue

*Continue* ends execution of the current loop body and starts the loop again. Like *break* it can optionally called with a parameter specifying the loop to be *continued*.

```
for($i=0,$i<100,++$i)
{
	for($k=0,$k<5,++$k)
	{
		if(rng.next(5)>3)
			continue(2)
	}
}
```