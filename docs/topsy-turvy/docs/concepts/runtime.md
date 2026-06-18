---
sidebar_position: 5
---

# Runtime

The runtime provides the required services to run Topsy Turvy programmes and provide services to them while running.  The _Operetta Toolchain_ provides an interpreter which executes programmes by using underlying .NET types and functionality.  The _Operetta CLI_ provides direct access to the interpreter with the following command:

```sh
operetta perform <file>
```

The Visual Studio Code _Topsy Turvy_ extension also provides functionality to execute programmes using the interpreter directly.

## Implementation

The runtime is implemented in the `BWHazel.TopsyTurvy.Runtime` namespace.  It comprises several components, each of which is described in subsections below.

### Environment

The environment, implemented in the `TopsyTurvyEnvironment` class, serves two purposes during execution:

* It maintains the state of all declared values.
* It maintains scoping of values.

Every named value, both variables and function parameters, in a Topsy Turvy programme is maintained in the environment, added on declaration and updated on assignment.  The environment also retrieves values when accessed.  These values are stored as instances of the `TopsyTurvyValue` class which wraps the underlying .NET primitive type and its equivalent Topsy Turvy literal type enumeration constant.  For example, a `PEER` of value `20` would be:

Arrays are stored as a `TopsyTurvyValue` with `LiteralType.Array`, whose `RawValue` holds a `List<TopsyTurvyValue>`.  Assignment copies the list reference rather than the list contents, so two array variables assigned to each other share the same underlying `List<TopsyTurvyValue>` instance: there is no copy-on-assign.

Variables declared with the `CONSERVATIVE` modifier are tracked separately as constants.  Any attempt to mutate a constant via `IS APPOINTED` (assignment), `IS HENCEFORTH A` (in-place cast), or `PRAY TELL` (input) raises a runtime error.  Variables declared with `LIBERAL`, or with no modifier (the default), remain freely mutable.

When a `WITH THE GREATEST RESPECT` block catches a thrown value, the cursed value is always placed in the `JUST SO` implicit variable on entry to the `MODIFIED RAPTURE` block.  Optionally, a named binding may be written immediately after `MODIFIED RAPTURE` separated by a comma, for example `MODIFIED RAPTURE, Grievance`, and the interpreter will auto-declare `Grievance` in a nested scope covering the exception block.  The binding is scoped to the exception block only and is inaccessible after `THAT CONCLUDES THE MATTER.`

```cs
TopsyTurvyValue value = TopsyTurvyValue.Integer(20);
```

The `TopsyTurvyValue` class also exposes two methods used during execution:

|Method|Return Type|Description|
|-|-|-|
|`IsTruthy()`|`bool`|Determines if the current value is `true` according to specified rules (please see below).|
|`CastTo()`|`TopsyTurvyValue`|Casts a value from one Topsy Turvy type to another (please see below).|

_Truthy_ values for the Topsy Turvy types are outlined below:

|Type|Truthy Values|
|-|-|
|`PEER` (Integer)|Any non-zero number.|
|`FATHOM` (Floating-Point)|Any non-zero number.|
|`YARN` (String)|Any non-empty string.|
|`DECREE` (Boolean)|`true`|
|`NAUGHT` (Null)|Never `true`|
|`LITTLE LIST OF` (Array)|Any non-empty array.|

Casting between values is outlined below:

||To `PEER`|To `FATHOM`|To `YARN`|To `DECREE`|To `NAUGHT`|
|-|-|-|-|-|-|
|From `PEER`||Cast to `double`|Call `ToString()`|Call `IsTruthy()`|Set as `null`|
|From `FATHOM`|Cast to `int` truncating to integer||Call `ToString()`|Call `IsTruthy()`|Set as `null`|
|From `YARN`|Call `TryParse()` and throw if not an integer.|Call `TryParse()` and throw if not numeric.||Call `IsTruthy()`|Set as `null`|
|From `DECREE`|`VERITY` => `1`; `NAY` => `0`|`VERITY` => `1.0`; `NAY` => `0.0`|Topsy Turvy Literal as `string`||Set as `null`|
|From `NAUGHT`|`0`|`0.0`|Topsy Turvy Literal as `string`|`false`||


Scopes are managed by creating nested environments or function environments.  Value access is upward: nested environments can access values in enclosing environments but not vice versa.  Additionally, function environments are isolated and are therefore unable to access values outside their own environment.

### Input & Output

Input and output enables the programme to interact externally.  The runtime provides an `ITopsyTurvyIO` interface to enable reading and writing of data via `ReadLine()` and `WriteLine()` methods and a built-in `ConsoleIO` implementation to read from standard input and write to standard output.

An additional buffered IO implementation, `BufferedWebIO`, is provided in the Web Editor to simulate interactions with standard input and output.

### Control Flow Signalling

Control flow via `THAT WILL DO.` break and `ONCE MORE.` continue statements as well as `AND SO I FIND` and `MY DUTY IS PREMATURELY DISCHARGED.` function return statements use .NET exceptions as signalling of these events, implemented directly in the interpreter.  Throwing a Topsy Turvy exception via `A HIDEOUS CURSE ON` also uses an exception, in standard .NET convention, caught by the interpreter and handled using Topsy Turvy language constructs.

These are all implemented as exception types in the `BWHazel.TopsyTurvy.Runtime` namespace: `BreakSignalException`, `ContinueSignalException`, `ReturnSignalException` and `TopsyTurvyThrowException`.

### Interpreter

The interpreter is implemented in the `Interpreter` class and is an example of a tree-walking interpreter as it processes, or _walks_, the AST of the programme.  Simplistically, it is one big loop over the sequence of statements in a Topsy Turvy programme, drilling down through the AST to handle each AST type in each statement; statement handlers may call expression handlers which in turn may recursively call other expression handlers, such as when executing nested expressions.  It uses the `Parse()` method of the [Parser](./parser.md) and therefore will throw an error immediately on a parser error.  Array index bounds are validated at execution time, as is element assignment on `CONSERVATIVE` arrays, both raising a `TopsyTurvyRuntimeException` via the same constant-registry check used for scalar constants.

Execution of the interpreter can be configured by passing an `InterpreterExecutionOptions` object.  Currently supported configuration options include:

* An execution timeout as a guard against infinite loops or excessively long-running programmes.
* A source file path to resolve relative import paths.
* A custom file resolver for use in contexts such as a virtual file system.
* A list of command-line arguments, exposed inside the programme as the built-in `THE PROPS` constant array.

