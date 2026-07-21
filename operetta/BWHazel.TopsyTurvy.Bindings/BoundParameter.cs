using System;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Describes one Topsy Turvy-visible parameter of a bound function.
/// </summary>
/// <remarks>
/// A CLR method signature and a Topsy Turvy function signature are not the same shape: CLR parameter names are
/// camel case and may carry host-injected service types the language never sees, while Topsy Turvy expects Pascal-cased,
/// <see cref="LiteralType"/>-typed parameters. <see cref="BindingScanner"/> produces one <see cref="BoundParameter"/>
/// per language-visible parameter so every later consumer (the type checker, the interpreter, hover text) reads a
/// signature already in the shape the language expects, rather than re-deriving it from raw reflection metadata
/// each time.
/// </remarks>
/// <param name="Name">The Topsy Turvy-visible parameter name.</param>
/// <param name="Type">The inferred Topsy Turvy type.</param>
/// <param name="ClrType">The CLR parameter type, retained for marshalling.</param>
public sealed record BoundParameter(string Name, LiteralType Type, Type ClrType);
