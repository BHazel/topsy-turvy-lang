---
sidebar_position: 6
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

```cs
TopsyTurvyValue value = TopsyTurvyValue.Integer(20);
```

When a variable is declared without an initial value, the environment initialises it to the type-specific default: integer types to `0`, floating-point types to `0.0`, `DECREE` to `NAY`, `YARN` to an empty string and `STITCH` to the null character.  This default is supplied at the point of declaration in the interpreter rather than deferred to first access.

#### Arrays

Arrays are stored as a `TopsyTurvyValue` with `LiteralType.Array`, whose `RawValue` holds a `List<TopsyTurvyValue>`.  Assignment copies the list reference rather than the list contents, so two array variables assigned to each other share the same underlying `List<TopsyTurvyValue>` instance: there is no copy-on-assign.

#### Pointers

Pointers are stored as a `TopsyTurvyValue` with `LiteralType.Pointer`, whose `RawValue` holds a `TopsyTurvyPointerTarget`.  This is a managed handle rather than a raw memory address.  `GALLERY PICTURE TO <variable>` creates one of three target variants depending on the pointed-at variable current type:

* A **variable** target for a scalar which resolves reads and writes through the environment by name.
* An **array element** target to hold the shared `List<TopsyTurvyValue>` directly at a 0-based position.
* A **string element** target which re-reads the current string from the environment on every access rather than holding a copy, since strings are immutable CLR values rather than reference types.

Pointing at an array or `YARN` decays the pointer to its first element or character respectively, enabling subsequent pointer arithmetic.  An unassigned pointer, declared without a `BEING` clause, is represented as `NAUGHT` rather than a pointer value with a `null` target.  Dereferencing or writing through it is a runtime error.

#### Constants

Variables declared with the `CONSERVATIVE` modifier are tracked separately as constants.  Any attempt to mutate a constant via `IS APPOINTED` (assignment) or `PRAY TELL` (input) raises a runtime error.  Variables declared with `LIBERAL`, or with no modifier (the default), remain freely mutable.

#### Exception Handling

When a `WITH THE GREATEST RESPECT` block catches a thrown value, a named binding must be written immediately after `MODIFIED RAPTURE` separated by a comma, for example `MODIFIED RAPTURE, Grievance`.  The interpreter auto-declares the binding as a `YARN` variable in a nested scope covering the exception block and is inaccessible after `THAT CONCLUDES THE MATTER.`


#### Truthiness & Casting

The `TopsyTurvyValue` class also exposes two methods used during execution:

|Method|Return Type|Description|
|-|-|-|
|`IsTruthy()`|`bool`|Determines if the current value is `true` according to specified rules (please see below).|
|`CastTo()`|`TopsyTurvyValue`|Casts a value from one Topsy Turvy type to another (please see below).|

`IsTruthy()` is used internally by `CastTo()` when converting any type to `DECREE`.  It is not used to evaluate conditional expressions: boolean contexts (conditions, logical operators, guard clauses) require a `DECREE` value and any other type is a type error.  The truthiness mapping used during casts is:

|Type|Truthy Values|
|-|-|
|All Integer Types|Any non-zero number.|
|All Floating-Point Types|Any non-zero number.|
|`YARN` (String)|Any non-empty string.|
|`STITCH` (Character)|Any character except the null character (`\0`).|
|`DECREE` (Boolean)|`VERITY`|
|`NAUGHT` (Null)|Never truthy.|
|`LITTLE LIST OF` (Array)|Any non-empty array.|
|`A GALLERY PICTURE OF` (Pointer)|Always truthy: an unassigned pointer is represented by `NAUGHT`, not a pointer value.|

Casting between values is outlined below:

||To Integer|To Floating-Point|To `YARN`|To `STITCH`|To `DECREE`|To `NAUGHT`|
|-|-|-|-|-|-|-|
|From Integer||Widening or narrowing numeric conversion.|Call `ToString()`.|Cast code point to `char`.|Call `IsTruthy()`.|Set as `null`.|
|From Floating-Point|Truncating numeric conversion.||Call `ToString()`.|Truncate to integer then cast to `char`.|Call `IsTruthy()`.|Set as `null`.|
|From `YARN`|Call `TryParse()` and throw if not an integer.|Call `TryParse()` and throw if not numeric.||First character; throws if empty string.|Call `IsTruthy()`.|Set as `null`.|
|From `STITCH`|Unicode code point.|Unicode code point.|Single-character string.||Call `IsTruthy()`.|Set as `null`.|
|From `DECREE`|`VERITY` => `1`; `NAY` => `0`|`VERITY` => `1.0`; `NAY` => `0.0`|Topsy Turvy Literal as `string`.|`VERITY` => `\x01`; `NAY` => `\0`.||Set as `null`.|
|From `NAUGHT`|`0`|`0.0`|Topsy Turvy Literal as `string`.|`\0` (null character).|`false`||


Scopes are managed by creating nested environments or function environments.  Value access is upward: nested environments can access values in enclosing environments but not vice versa.  Additionally, function environments are isolated and are therefore unable to access values outside their own environment.

### Input & Output

Input and output enables the programme to interact externally.  The runtime provides an `ITopsyTurvyIO` interface to enable reading and writing of data via `ReadLine()` and `WriteLine()` methods and a built-in `ConsoleIO` implementation to read from standard input and write to standard output.

An additional buffered IO implementation, `BufferedWebIO`, is provided in the Web Editor to simulate interactions with standard input and output.

### Control Flow Signalling

Control flow via `THAT WILL DO.` break and `ONCE MORE.` continue statements as well as `AND SO I FIND` and `MY DUTY IS PREMATURELY DISCHARGED.` function return statements as well as the `AND SO I FIND` programme return statement use .NET exceptions as signalling of these events, implemented directly in the interpreter.  Throwing a Topsy Turvy exception via `A HIDEOUS CURSE ON` also uses an exception, in standard .NET convention, caught by the interpreter and handled using Topsy Turvy language constructs.

These are all implemented as exception types in the `BWHazel.TopsyTurvy.Runtime` namespace: `BreakSignalException`, `ContinueSignalException`, `ProgrammeReturnSignalException`, `ReturnSignalException` and `TopsyTurvyThrowException`.

### Interpreter

The interpreter is implemented in the `Interpreter` class and is an example of a tree-walking interpreter as it processes, or _walks_, the AST of the programme.  Simplistically, it is one big loop over the sequence of statements in a Topsy Turvy programme, drilling down through the AST to handle each AST type in each statement; statement handlers may call expression handlers which in turn may recursively call other expression handlers, such as when executing nested expressions.  It uses the `Parse()` method of the [Parser](./parser.md) and therefore will throw an error immediately on a parser error.  Array index bounds are validated at execution time, as is element assignment on `CONSERVATIVE` arrays, both raising a `TopsyTurvyRuntimeException` via the same constant-registry check used for scalar constants.

Pointer arithmetic (`SUM OF` and `DIFFERENCE OF` on a pointer operand) is bounds-checked in the same way, at the point of the arithmetic operation itself rather than lazily on a later dereference.  Moving a pointer before the first element or beyond the last, or performing arithmetic on a pointer that refers to a plain variable rather than an array or `YARN` element, is a runtime error.

Execution of the interpreter can be configured by passing an `InterpreterExecutionOptions` object.  Currently supported configuration options include:

* An execution timeout as a guard against infinite loops or excessively long-running programmes.
* A source file path to resolve relative import paths.
* A custom file resolver for use in contexts such as a virtual file system.
* A list of command-line arguments, exposed inside the programme as the built-in `THE PROPS` constant array.

Every `IT IS MY DUTY TO PERFORM` function, whether declared directly or contributed by a `PRAY ADMIT`-imported file, is registered in a single lookup table keyed by its name.  When the declaring file declares a namespace with `TOWN`, the registered key is namespace-qualified rather than bare.  Resolving a `SUMMON` call by a bare name tries, in order:

* The calling code namespace.
* Each namespace opened with `PRAY RECOGNISE`.
* Then the global, non-namespaced, table.

This is mirrored by the same tiered resolution the [Type Checker](./type-checker.md) during static type-checking.  A bare name matching more than one open namespace is a runtime error requiring a fully-qualified name to disambiguate.  A fully-qualified call, using either the `WITH DISTRICT` ... `WITH DUTY` long form or the `*` short form, is looked up directly by its qualified key and does not go through this tiered resolution.

