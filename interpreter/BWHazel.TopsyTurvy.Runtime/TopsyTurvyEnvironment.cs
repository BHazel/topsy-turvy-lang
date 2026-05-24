using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Manages variable storage and scoping for a Topsy Turvy execution context.
/// </summary>
public sealed class TopsyTurvyEnvironment
{
    private readonly Dictionary<string, TopsyTurvyValue> variables = [];
    private readonly TopsyTurvyEnvironment? enclosingEnvironment;

    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyEnvironment"/> class with an optional enclosing environment.
    /// </summary>
    /// <param name="enclosing">The optional enclosing environment.</param>
    private TopsyTurvyEnvironment(TopsyTurvyEnvironment? enclosing)
    {
        this.JustSo  = TopsyTurvyValue.Null();
        this.enclosingEnvironment = enclosing;
    }

    /// <summary>
    /// Gets or sets the value of the implicit register for this environment.
    /// </summary>
    public TopsyTurvyValue JustSo { get; set; }

    /// <summary>
    /// Creates a new top-level global environment.
    /// </summary>
    public static TopsyTurvyEnvironment CreateGlobal() => new(null);

    /// <summary>
    /// Creates a nested environment whose enclosing chain includes this environment.
    /// </summary>
    /// <remarks>
    /// Variables declared in the nested environment are not visible in the enclosing
    /// environment.  Variables in the enclosing environment remain readable and assignable
    /// from the nested environment.
    /// </remarks>
    public TopsyTurvyEnvironment CreateNested() => new(this);

    /// <summary>
    /// Creates an isolated environment for a function call, with no enclosing chain.
    /// </summary>
    public static TopsyTurvyEnvironment CreateFunctionEnvironment() => new(null);

    /// <summary>
    /// Declares a new variable in this environment with the given initial value.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The initial value.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown if <paramref name="name"/> is already declared in this environment.</exception>
    public void Declare(string name, TopsyTurvyValue value)
    {
        if (this.variables.ContainsKey(name))
        {
            throw new TopsyTurvyRuntimeException($"Variable '{name}' is already declared in this scope.");
        }

        this.variables[name] = value;
    }

    /// <summary>
    /// Assigns a new value to an existing variable, walking the enclosing chain to find it.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The new value.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown if <paramref name="name"/> has not been declared in any accessible environment.</exception>
    public void Assign(string name, TopsyTurvyValue value)
    {
        if (this.variables.ContainsKey(name))
        {
            variables[name] = value;
            return;
        }

        if (this.enclosingEnvironment != null)
        {
            this.enclosingEnvironment.Assign(name, value);
            return;
        }

        throw new TopsyTurvyRuntimeException($"Variable '{name}' has not been declared.");
    }

    /// <summary>
    /// Retrieves the value of a variable, walking the enclosing chain to find it.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The current value of the variable.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown if <paramref name="name"/> has not been declared in any accessible environment.</exception>
    public TopsyTurvyValue Get(string name)
    {
        if (name == "JUST SO")
        {
            return this.JustSo;
        }

        if (this.variables.TryGetValue(name, out TopsyTurvyValue? value))
        {
            return value;
        }

        if (this.enclosingEnvironment != null)
        {
            return this.enclosingEnvironment.Get(name);
        }

        throw new TopsyTurvyRuntimeException($"Variable '{name}' has not been declared.");
    }
}
