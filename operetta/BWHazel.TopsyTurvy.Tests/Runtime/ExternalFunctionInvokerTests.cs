using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Tests for the <see cref="ExternalFunctionInvoker"/> class.
/// </summary>
public class ExternalFunctionInvokerTests
{
    private readonly BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestExternalFunctionBindingClass));

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Find"/> returns the descriptor for a known name.
    /// </summary>
    [Fact]
    public void Find_WithKnownName_ReturnsDescriptor()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);

        invoker.Find("TestWiden").ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Find"/> returns <c>null</c> for an unknown name.
    /// </summary>
    [Fact]
    public void Find_WithUnknownName_ReturnsNull()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);

        invoker.Find("NoSuchFunction").ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Invoke"/> converts a narrower argument to a wider parameter type.
    /// </summary>
    [Fact]
    public void Invoke_WithWideningArgument_ConvertsAndReturnsWrappedValue()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);
        BoundFunctionDescriptor descriptor = invoker.Find("TestWiden")!;
        TestIO io = new([], new Queue<string>());

        TopsyTurvyValue? result = invoker.Invoke(descriptor, [TopsyTurvyValue.Integer(5)], io);

        result.ShouldNotBeNull();
        result!.LiteralType.ShouldBe(LiteralType.Long);
        result.RawValue.ShouldBe(5L);
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Invoke"/> throws when an argument is not the parameter type and is
    /// not a widening conversion to it.
    /// </summary>
    [Fact]
    public void Invoke_WithNarrowingArgumentMismatch_Throws()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);
        BoundFunctionDescriptor descriptor = invoker.Find("TestNarrow")!;
        TestIO io = new([], new Queue<string>());

        Should.Throw<TopsyTurvyRuntimeException>(() => invoker.Invoke(descriptor, [TopsyTurvyValue.Long(5L)], io));
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Invoke"/> throws when an argument type is not numeric-compatible
    /// with the parameter type at all.
    /// </summary>
    [Fact]
    public void Invoke_WithIncompatibleArgumentType_Throws()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);
        BoundFunctionDescriptor descriptor = invoker.Find("TestNarrow")!;
        TestIO io = new([], new Queue<string>());

        Should.Throw<TopsyTurvyRuntimeException>(() => invoker.Invoke(descriptor, [TopsyTurvyValue.String("nope")], io));
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Invoke"/> wraps an exception thrown by the invoked function in a
    /// <see cref="TopsyTurvyRuntimeException"/> naming the function.
    /// </summary>
    [Fact]
    public void Invoke_WhenFunctionThrows_WrapsInRuntimeExceptionNamingFunction()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);
        BoundFunctionDescriptor descriptor = invoker.Find("TestThrows")!;
        TestIO io = new([], new Queue<string>());

        TopsyTurvyRuntimeException exception = Should.Throw<TopsyTurvyRuntimeException>(() => invoker.Invoke(descriptor, [], io));

        exception.Message.ShouldContain("TestThrows");
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionInvoker.Invoke"/> returns <c>null</c> for a void function.
    /// </summary>
    [Fact]
    public void Invoke_WithVoidFunction_ReturnsNull()
    {
        ExternalFunctionInvoker invoker = new(this.catalogue);
        BoundFunctionDescriptor descriptor = invoker.Find("TestWrite")!;
        TestIO io = new([], new Queue<string>());

        TopsyTurvyValue? result = invoker.Invoke(descriptor, [TopsyTurvyValue.String("Hello")], io);

        result.ShouldBeNull();
    }
}
