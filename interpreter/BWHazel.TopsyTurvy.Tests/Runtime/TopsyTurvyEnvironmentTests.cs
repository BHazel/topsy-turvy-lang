using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Tests for the <see cref="TopsyTurvyEnvironment"/> class.
/// </summary>
public class TopsyTurvyEnvironmentTests
{
    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.JustSo"/> property is initialised to null.
    /// </summary>
    [Fact]
    public void JustSo_AfterCreation_IsNull()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();

        Assert.Equal(TopsyTurvyValue.Null().TopsyTurvyType, environment.JustSo.TopsyTurvyType);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.JustSo"/> property can be written and re-read.
    /// </summary>
    [Fact]
    public void JustSo_WhenSet_ReturnsNewValue()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        TopsyTurvyValue expectedValue = TopsyTurvyValue.Integer(99);

        environment.JustSo = expectedValue;

        Assert.Same(expectedValue, environment.JustSo);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Declare"/> method stores a variable retrievable by name.
    /// </summary>
    [Fact]
    public void Declare_WithNewName_VariableIsRetrievable()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        environment.Declare(name: "x", value: value);

        Assert.Same(value, environment.Get("x"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Declare"/> method throws when the same name is declared twice.
    /// </summary>
    [Fact]
    public void Declare_WithDuplicateName_ThrowsRuntimeException()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        environment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));

        Assert.Throws<TopsyTurvyRuntimeException>(() => environment.Declare(name: "x", value: TopsyTurvyValue.Integer(2)));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Assign"/> method updates the value of an existing variable.
    /// </summary>
    [Fact]
    public void Assign_ToExistingVariable_UpdatesValue()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        environment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));

        environment.Assign(name: "x", value: TopsyTurvyValue.Integer(99));

        Assert.Equal(99, environment.Get("x").RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Assign"/> method walks the enclosing chain to update a parent variable.
    /// </summary>
    [Fact]
    public void Assign_ToEnclosingScopeVariable_UpdatesEnclosingValue()
    {
        TopsyTurvyEnvironment enclosingEnvironment = TopsyTurvyEnvironment.CreateGlobal();
        enclosingEnvironment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));
        TopsyTurvyEnvironment nestedEnvironment = enclosingEnvironment.CreateNested();

        nestedEnvironment.Assign(name: "x", value: TopsyTurvyValue.Integer(99));

        Assert.Equal(99, enclosingEnvironment.Get("x").RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Assign"/> method throws when the variable has not been declared.
    /// </summary>
    [Fact]
    public void Assign_ToUndeclaredVariable_ThrowsRuntimeException()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();

        Assert.Throws<TopsyTurvyRuntimeException>(() => environment.Assign(name: "undeclared", value: TopsyTurvyValue.Integer(1)));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Assign"/> method throws when the variable is not in the function environment chain.
    /// </summary>
    [Fact]
    public void Assign_InFunctionEnvironmentToUndeclaredVariable_ThrowsRuntimeException()
    {
        TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
        globalEnvironment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));
        TopsyTurvyEnvironment functionEnvironment = TopsyTurvyEnvironment.CreateFunctionEnvironment();

        Assert.Throws<TopsyTurvyRuntimeException>(() => functionEnvironment.Assign(name: "x", value: TopsyTurvyValue.Integer(99)));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Get"/> method returns the current value of a declared variable.
    /// </summary>
    [Fact]
    public void Get_DeclaredVariable_ReturnsValue()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");
        environment.Declare(name: "greeting", value: value);

        TopsyTurvyValue result = environment.Get("greeting");

        Assert.Same(value, result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Get"/> method walks the enclosing chain to find a variable in the enclosing scope.
    /// </summary>
    [Fact]
    public void Get_EnclosingScopeVariable_ReturnsEnclosingValue()
    {
        TopsyTurvyEnvironment enclosingEnvironment = TopsyTurvyEnvironment.CreateGlobal();
        TopsyTurvyValue value = TopsyTurvyValue.Integer(7);
        enclosingEnvironment.Declare(name: "x", value: value);
        TopsyTurvyEnvironment nestedEnvironment = enclosingEnvironment.CreateNested();

        TopsyTurvyValue result = nestedEnvironment.Get("x");

        Assert.Same(value, result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Get"/> method returns the JustSo register for the special name JUST SO.
    /// </summary>
    [Fact]
    public void Get_WithJustSoName_ReturnsJustSoRegister()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();
        TopsyTurvyValue value = TopsyTurvyValue.Integer(55);
        environment.JustSo = value;

        TopsyTurvyValue result = environment.Get("JUST SO");

        Assert.Same(value, result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Get"/> method throws when the variable has not been declared.
    /// </summary>
    [Fact]
    public void Get_UndeclaredVariable_ThrowsRuntimeException()
    {
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();

        Assert.Throws<TopsyTurvyRuntimeException>(() => environment.Get("undeclared"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.Get"/> method throws when a variable in the enclosing scope is requested from a function environment.
    /// </summary>
    [Fact]
    public void Get_EnclosingScopeVariableFromFunctionEnvironment_ThrowsRuntimeException()
    {
        TopsyTurvyEnvironment globalEnvironment = TopsyTurvyEnvironment.CreateGlobal();
        globalEnvironment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));
        TopsyTurvyEnvironment functionEnvironment = TopsyTurvyEnvironment.CreateFunctionEnvironment();

        Assert.Throws<TopsyTurvyRuntimeException>(() => functionEnvironment.Get("x"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.CreateNested"/> method creates a nested environment that can shadow a variable in the enclosing environment.
    /// </summary>
    [Fact]
    public void CreateNested_WithSameNameDeclared_ShadowsEnclosingValue()
    {
        TopsyTurvyEnvironment enclosingEnvironment = TopsyTurvyEnvironment.CreateGlobal();
        enclosingEnvironment.Declare(name: "x", value: TopsyTurvyValue.Integer(1));
        TopsyTurvyEnvironment nestedEnvironment = enclosingEnvironment.CreateNested();
        nestedEnvironment.Declare(name: "x", value: TopsyTurvyValue.Integer(99));

        Assert.Equal(99, nestedEnvironment.Get("x").RawValue);
        Assert.Equal(1, enclosingEnvironment.Get("x").RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyEnvironment.CreateFunctionEnvironment"/> method creates an isolated environment that can declare and retrieve its own variables.
    /// </summary>
    [Fact]
    public void CreateFunctionEnvironment_Always_CanDeclareAndRetrieveOwnVariables()
    {
        TopsyTurvyEnvironment functionEnvironment = TopsyTurvyEnvironment.CreateFunctionEnvironment();

        functionEnvironment.Declare(name: "param", value: TopsyTurvyValue.Integer(7));

        Assert.Equal(7, functionEnvironment.Get("param").RawValue);
        Assert.Throws<TopsyTurvyRuntimeException>(() => functionEnvironment.Get("nonexistent"));
    }
}
