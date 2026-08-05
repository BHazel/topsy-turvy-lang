using System;

namespace BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

/// <summary>
/// Overrides the Topsy Turvy-visible name of a bound function parameter.
/// </summary>
/// <remarks>
/// Without this attribute the CLR parameter name is used verbatim.  The specification
/// convention is Pascal-cased parameter names, for example <c>Text</c>, while C# convention
/// is camel case, so bound parameters normally carry this attribute.
/// </remarks>
/// <param name="parameterName">The parameter name as shown to Topsy Turvy code and tooling.</param>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class TopsyTurvyParameterAttribute(string parameterName) : Attribute
{
    /// <summary>
    /// Gets the parameter name as shown to Topsy Turvy code and tooling.
    /// </summary>
    public string ParameterName { get; } = parameterName;
}
