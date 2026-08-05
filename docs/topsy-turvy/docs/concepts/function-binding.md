---
sidebar_position: 6
---

# Function Binding

Function binding is the process by which a Topsy Turvy programme can call functions implemented in .NET exposed to Topsy Turvy using the standard `SUMMON` call syntax.  This underpins how the Standard Library works in its current implementation and how developers can write functions in .NET and make them callable from Topsy Turvy.

Rather than being an individual step in the toolchain, function binding is required at several stages, each of which consults the same catalogue of bound functions:

* The [Type Checker](./type-checker.md) requires a function signature to check a call against.
* The [Runtime](./runtime.md) needs to invoke the function.
* [Analysis](./analysis.md) requires a function description to display in hover text and completions.

## Implementation

:::warning
The following known limitations affect the current implementation:

* Only one host-injected parameter type on a single bound function is currently supported.
* Nested array (array of arrays) are not supported as a bound parameter or return type.
* Admitting an external library is not supported during a `cadenza` REPL session.
:::

Function Binding is implemented across three projects, each with a distinct responsibility:

|Project|Role|Description|
|-|-|-|
|`BWHazel.TopsyTurvy.Sdk.Interop`|Types to expose functions.|The attributes that mark a method as callable from Topsy Turvy and the host services such a method may request.  Has no dependencies on other toolchain components.|
|`BWHazel.TopsyTurvy.Bindings`|The function binding discovery engine.|Scans a class or an assembly for attributed methods, builds a description of each one and combines them into a catalogue that the Type Checker, Runtime and Analysis all consult.|
|`BWHazel.TopsyTurvy.StandardLibrary`|The Standard Library|A set of classes whose methods are marked with the `Sdk.Interop` attributes, in the same way any external library would be.|

### Marking a Function as Callable

A method becomes callable from Topsy Turvy by marking it with the `[TopsyTurvyFunction]` attribute from `Sdk.Interop` and each parameter with the `[TopsyTurvyParameter]` attribute.  The method must be `public static` on a `public` class, since the toolchain calls it by reflection rather than through an instance.

```csharp
[TopsyTurvyFunction(Name = "PreviewBehold", IsPreview = true, KeywordAnalogue = "BEHOLD")]
public static void PreviewBehold(
    [TopsyTurvyParameter("Text")] string text,
    [TopsyTurvyParameter("WithCeremony")] bool withCeremony,
    ITopsyTurvyIO io) =>
    io.WriteLine(text, suppressNewline: !withCeremony);
```

The example above, taken from the preview Standard Library `Global` class, is a function that can be called from Topsy Turvy as:

```topsy
SUMMON PreviewBehold WITH Text AS "Hello" AND WithCeremony AS VERITY IF YOU PLEASE.
```

`TopsyTurvyFunctionAttribute` supports the following properties, all optional:

|Property|Type|Description|
|-|-|-|
|`Name`|`string?`|The name used to call the function from Topsy Turvy.  Defaults to the CLR method name if not set.|
|`Namespace`|`string[]?`|The ordered namespace path segments the function is declared under, or `null` for the global namespace. This is the same shape as `NamespaceDeclarationNode.Path`, for example `["Accounts", "Payroll"]` for a function declared under `TOWN Accounts WITH DISTRICT Payroll`.|
|`IsPreview`|`bool`|Marks the function as a preview function that may change without warning. Used only for documentation metadata.|
|`KeywordAnalogue`|`string?`|The name of the language keyword this function is the library counterpart of, if any, for example `BEHOLD` for `PreviewBehold`. Used only for documentation metadata.|

A parameter can be given a different, Topsy Turvy-visible name with `[TopsyTurvyParameter("Name")]`, as shown for `Text` and `WithCeremony` above. Without this attribute the CLR parameter name is used verbatim. The specification convention is Pascal-cased parameter names, while C# convention is camel case, so a bound parameter almost always carries this attribute.

### Host-Injected Parameters

A bound method may request a service supplied by the host running the programme, rather than or in addition to a value coming from Topsy Turvy as a parameter, by adding a trailing parameter of a recognised host-injected type.  At present the following of these host-injected types are supported:

|Type|Description|
|-|-|
|`ITopsyTurvyIO`|The interface for reading and writing input and output, as described on the [Runtime](./runtime.md#input--output) page.|

A host-injected parameter is invisible to Topsy Turvy code:

* It does not appear in the call syntax.
* It is not counted in the argument list.
* It is supplied automatically by the interpreter at the point of invocation.

Only trailing parameters may be host-injected.  A bound parameter appearing after a host-injected one is rejected when the function is scanned as described below.  Currently only one host-injected parameter is supported per function, even though a future host-injected type could in principle appear alongside another.

### Discovery

A class or assembly is turned into a set of function descriptions by the `BindingScanner` class, which offers two entry points:

|Method|Use|
|-|-|
|`Scan(Type)`|Scans a single class known at compile time, such as a Standard Library class.|
|`ScanAssembly(Assembly)`|Scans every type in an assembly loaded at runtime, such as an admitted external library.|

Scanning enforces a number of rules on every attributed method, failing immediately with a clear error at scan time rather than allowing a malformed binding to surface as a confusing failure later:

* The declaring class must be `public`.
* The method must be `public static`.
* The Topsy Turvy-visible name must be a valid identifier.
* Any host-injected parameter must come after every bound parameter, never before.
* Every bound parameter type, and the return type if the method does not return `void`, must be a type recognised by the type map described below.
* Every namespace segment, if any, must be a valid identifier.

### The Type Map

The bound parameters and return type of a method are ordinary CLR types, such as `int` or `string`, but the Type Checker, Runtime and Analysis all work in terms of Topsy Turvy `LiteralType` values. `ClrTypeMap` is the single place this conversion happens, mapping each of the CLR numeric, string, character and boolean types onto its corresponding `LiteralType`, and a one-dimensional array of any of these onto an array of the corresponding element type. A CLR type with no entry in this map, or a nested array, cannot be used as a bound parameter or return type, and scanning fails.

### Function Descriptions

The result of scanning a function is a `BoundFunctionDescriptor`, recording its:

* Name.
* Namespace.
* Preview Status.
* Keyword Analogue (if applicaable).
* Number of host-injected parameters.
* List of `BoundParameter` values, each carrying a Topsy Turvy-visible name, `LiteralType`, and the underlying CLR type used for marshalling.

This is the one shape every consumer, including the Runtime when invoking a function, the Type Checker when seeding a signature, and Analysis when building hover text, expects.

### The Catalogue

`BindingCatalogue` collects a set of `BoundFunctionDescriptor` values, indexed case-insensitively by fully-qualified name (including the full namespace path and function name), and offers several ways to build one:

|Member|Description|
|-|-|
|`Default`|The Standard Library only, built once and reused everywhere it is needed.|
|`Empty`|A catalogue with no functions at all, for a host that offers none.|
|`FromDescriptors(descriptors)`|Builds a catalogue directly from an already-scanned list of descriptors.|
|`Create(bindingClasses)`|Scans several classes at once and builds a catalogue from the result.|
|`Merge(catalogues)`|Combines several catalogues into one, for example the Standard Library plus one or more admitted external libraries.|
|`Find(name)` / `FindAll(name)`|Looks a function up by its namespace-qualified name. `Find` expects exactly one match and is an error if more than one overload shares that name; `FindAll` returns every overload so the caller can pick the right one by argument type.|

Two functions with the same namespace-qualified name and identical parameter types cannot coexist in the same catalogue: building or merging a catalogue with such a collision fails immediately. Two functions sharing a name but with different parameter types are permitted and are treated as an overload set, resolved by argument type at the point of call.

### Documentation

The XML documentation comments for a .NET method are mapped onto the same `DocumentationComment` type used for hand-written Topsy Turvy documentation comments, described on the [Analysis](./analysis.md#documentation-comments) page. This enables a Standard Library or external library function to display a proper hover tooltip in an editor, with its preview status and keyword analogue folded into the tooltip text alongside the summary.

Writing documentation for a bound function is optional: an assembly with no accompanying XML documentation file at all simply displays an empty tooltip for a function. If a documentation file is present, however, every bound function in that assembly is expected to have a matching entry in it, so a specific function missing from an otherwise-documented file is treated as an error rather than silently producing an incomplete tooltip.

## The Shadowing Rule

A bare, unqualified `SUMMON` name is resolved against a series of tiers, tried in order until one produces a match:

1. Within the namespace of the calling code.
2. Each namespace opened with `PRAY RECOGNISE`.
3. The global, non-namespaced, table.

A call using a fully-qualified name, using either the `WITH DISTRICT` ... `WITH DUTY` long form or the `*` short form, is looked up directly by its qualified name and does not pass through this tiered resolution.

If a Topsy Turvy programme declares a function with the same namespace-qualified name and parameter types as a Standard Library or admitted function, the Topsy Turvy declaration always wins. This is checked independently everywhere a bound function might otherwise be reached, so that the Type Checker, the Runtime and Analysis all agree with each other about which function a given call actually reaches.

* The [Type Checker](./type-checker.md) only records a signature for a bound function if no Topsy Turvy function of the same qualified name and parameter types has already been recorded.
* The [Runtime](./runtime.md) checks each tier above for a Topsy Turvy function first. Only if none exists at that tier does it check for a bound function there instead. A fully-qualified call applies the same priority at its single qualified name.
* [Analysis](./analysis.md) does not add a bound function to the symbol table under a name already taken by a declared symbol.

A function that is not shadowed behaves identically wherever it is bound from: the same descriptor, the same type information, and the same documentation, whether it came from the Standard Library or an admitted external library.

## External Libraries

:::warning
The Web Editor does not support admitted external libraries due to application security.  Passing it a non-empty external catalogue raises an error rather than silently ignoring it.
:::

Beyond the Standard Library, developers may write their own .NET assemblies containing `[TopsyTurvyFunction]`-attributed methods and expose them to a Topsy Turvy programme by passing `--admit <path.dll>` on the command line (alias `--include`). The option can be repeated to admit more than one library at once.

```sh
operetta perform my-programme.topsy --admit MyLibrary.dll
```

This is supported on every _Operetta CLI_ command that runs or checks a programme: `perform`, `rehearse`, `sorcerer`, `incantation` and `cadenza`. For the REPL specifically, `--admit` is a startup-only flag: there is no in-session command to admit a library part-way through a session, since the interpreter session state is deliberately immutable once started.

Loading, scanning and merging an admitted library is handled in one place, `ExternalLibraryLoader`, so every command follows the same sequence: each path is resolved and checked for existence, the assembly is loaded, it is scanned the same way the Standard Library is scanned and the result is merged with `BindingCatalogue.Default`. Any failure along the way, such as a missing file, a load failure, a malformed binding or a name collision with an existing function, stops the whole operation and reports a single, plain error rather than proceeding with a partially loaded catalogue.
