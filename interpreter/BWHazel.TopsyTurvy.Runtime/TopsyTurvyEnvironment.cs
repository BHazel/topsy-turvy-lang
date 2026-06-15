using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Manages variable storage and scoping for a Topsy Turvy execution context.
/// </summary>
/// <remarks>
/// <para>
/// The environment is a hierarchical structure that enables variable scoping at runtime.  Each environment defines a scope for
/// variable declarations and each can have an optional enclosing environment.  The global environment is the top-level environment
/// that exists for the lifetime of a programme.  Variables can only be declared in the current environment but access and assignment
/// can be performed in the current environment or any enclosing environment.  Function environments are isolated and have no
/// enclosing environment to ensure variables declared outside of a function are not visible inside the function and variables
/// declared inside a function are not visible outside of the function.
/// </para>
/// <para>
/// The following example demonstrates creating environments and working with variables.
/// </para>
/// <code>
/// // Create the global environment and declare a variable in it.
/// TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
/// globalEnvironment.Declare("LovesickMaidens", TopsyTurvyValue.Integer(20));
/// 
/// // Create a nested environment, declare and assign a variable in it and get a variable in the enclosing environment.
/// TopsyTurvyEnvironment nestedEnvironment = globalEnvironment.CreateNested();
/// nestedEnvironment.Declare("PoemTopic", TopsyTurvyValue.String("Churn"));
/// nestedEnvironment.Assign("PoemTopic", TopsyTurvyValue.String("Hollow"));
/// TopsyTurvyValue lovesickMaidens = nestedEnvironment.Get("LovesickMaidens");
/// 
/// // Create a function environment and declare a variable in it.
/// TopsyTurvyEnvironment functionEnvironment = TopsyTurvyEnvironment.CreateFunctionEnvironment();
/// functionEnvironment.Declare("TotalLords", TopsyTurvyValue.Integer(15));
/// </code>
/// </remarks>
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
        this.JustSo = TopsyTurvyValue.Null();
        this.enclosingEnvironment = enclosing;
    }

    /// <summary>
    /// Gets or sets the value of the <c>JUST SO</c> implicit variable for this environment.
    /// </summary>
    /// <remarks>
    /// This receives the value of any expression not explicitly assigned to a named variable.
    /// </remarks>
    public TopsyTurvyValue JustSo { get; set; }

    /// <summary>
    /// Creates a new top-level global environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The global environment is the top-level environment that exists for the lifetime of a running programme and has no enclosing
    /// environment.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
    /// </code>
    /// </remarks>
    public static TopsyTurvyEnvironment CreateGlobal() => new(null);

    /// <summary>
    /// Creates a nested environment whose enclosing chain includes this environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Variables declared in the nested environment are not visible in the enclosing
    /// environment.  Variables in the enclosing environment remain readable and assignable
    /// from the nested environment.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
    /// TopsyTurvyEnvironment nestedEnvironment = globalEnvironment.CreateNested();
    /// </code>
    /// </remarks>
    public TopsyTurvyEnvironment CreateNested() => new(this);

    /// <summary>
    /// Creates an isolated environment for a function call, with no enclosing chain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Function environments are isolated from the enclosing environment chain to ensure that variables declared in a function
    /// are not visible outside of the function.  The enclosing environment is set to <c>null</c> to prevent access to variables
    /// in the calling environment.
    /// </para>
    /// <para>
    /// It should be noted that the environment created by this method is identical to a global environment, but is not
    /// the same instance as the global environment.  Both methods exist to provide clear intent when creating environments.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment functionEnvironment = TopsyTurvyEnvironment.CreateFunctionEnvironment();
    /// </code>
    /// </remarks>
    public static TopsyTurvyEnvironment CreateFunctionEnvironment() => new(null);

    /// <summary>
    /// Declares a new variable in this environment with the given initial value.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The initial value.</param>
    /// <remarks>
    /// <para>
    /// Variables can only be declared in the current environment instance.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
    /// environment.Declare("LovesickMaidens", TopsyTurvyValue.Integer(20));
    /// </code>
    /// </remarks>
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
    /// <remarks>
    /// <para>
    /// Variables can be assigned in the current environment or any enclosing environment.  If the variable is not found in the current environment,
    /// the enclosing chain is walked until the variable is found or the chain ends.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
    /// globalEnvironment.Declare("LovesickMaidens", TopsyTurvyValue.Integer(20));
    /// globalEnvironment.Assign("LovesickMaidens", TopsyTurvyValue.Integer(30));
    /// </code>
    /// </remarks>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown if <paramref name="name"/> has not been declared in any accessible environment.</exception>
    public void Assign(string name, TopsyTurvyValue value)
    {
        if (this.variables.ContainsKey(name))
        {
            this.variables[name] = value;
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
    /// <remarks>
    /// <para>
    /// Variables can be retrieved from the current environment or any enclosing environment.  If the variable is not found in the
    /// current environment, the enclosing chain is walked until the variable is found or the chain ends.  It should be noted that
    /// the <c>JUST SO</c> implicit variable is always accessible in any environment and will be returned if the name <c>JUST SO</c>
    /// is requested.  Accessing <c>JUST SO</c> will not walk the enclosing chain and is always resolved from the current environment.
    /// </para>
    /// <code>
    /// TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
    /// globalEnvironment.Declare("LovesickMaidens", TopsyTurvyValue.Integer(20));
    /// TopsyTurvyValue value = globalEnvironment.Get("LovesickMaidens");
    /// </code>
    /// </remarks>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown if <paramref name="name"/> has not been declared in any accessible environment.</exception>
    public TopsyTurvyValue Get(string name)
    {
        if (name == Keywords.SpecialNames.JustSo)
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
