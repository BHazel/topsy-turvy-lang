using System.Collections.Generic;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// The variables of one environment level within a paused <see cref="StackFrame"/>, labelled for display.
/// </summary>
/// <param name="Label">The display label for this scope level.</param>
/// <param name="Variables">The variables declared directly in this scope level.</param>
/// <remarks>
/// The full variable picture of a frame is one or more <see cref="VariableScope"/>s, built by walking
/// <see cref="TopsyTurvyEnvironment.Enclosing"/> from the environment of a <see cref="StackFrame"/> until it
/// reaches <c>null</c>. Because a function environment has no enclosing chain, a frame paused inside a function has
/// a single <c>"Locals"</c> scope and no <c>"Globals"</c> scope, correctly reflecting that <c>PRINCIPALS</c>
/// globals are not visible inside a function body.
/// </remarks>
public sealed record VariableScope(string Label, IReadOnlyDictionary<string, TopsyTurvyValue> Variables);
