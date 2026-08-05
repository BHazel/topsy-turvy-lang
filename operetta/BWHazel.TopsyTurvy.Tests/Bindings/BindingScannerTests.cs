using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// Tests for the <see cref="BindingScanner"/> class.
/// </summary>
public class BindingScannerTests
{
    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> ignores a public static method with no <c>TopsyTurvyBinding</c> attribute.
    /// </summary>
    [Fact]
    public void Scan_WithUnattributedMethod_IsIgnored()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        descriptors.ShouldNotContain(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.UnattributedFunction));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> defaults the name of an unnamed function to its CLR method name.
    /// </summary>
    [Fact]
    public void Scan_WithNoFunctionName_DefaultsNameToClrMethodName()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.ValueFunction));
        descriptor.Name.ShouldBe(nameof(TestValidBindingClass.ValueFunction));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> uses the <c>FunctionName</c> of the <c>TopsyTurvyBinding</c> attribute when set.
    /// </summary>
    [Fact]
    public void Scan_WithFunctionName_UsesFunctionName()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));
        descriptor.Name.ShouldBe("TestVoidFunction");
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> renames a bound parameter using its <c>TopsyTurvyBoundParameter</c> attribute.
    /// </summary>
    [Fact]
    public void Scan_WithBoundParameterAttribute_RenamesParameter()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));
        descriptor.Parameters.ShouldHaveSingleItem();
        descriptor.Parameters[0].Name.ShouldBe("Text");
        descriptor.Parameters[0].Type.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> excludes a trailing host-injected parameter from the language-level signature.
    /// </summary>
    [Fact]
    public void Scan_WithTrailingHostInjectedParameter_ExcludesItFromParameters()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));
        descriptor.HostInjectedParameterCount.ShouldBe(1);
        descriptor.Parameters.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> infers a <c>null</c> return type for a void function.
    /// </summary>
    [Fact]
    public void Scan_WithVoidFunction_InfersNullReturnType()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));
        descriptor.ReturnType.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> infers the mapped return type for a value-returning function.
    /// </summary>
    [Fact]
    public void Scan_WithValueReturningFunction_InfersReturnType()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.ValueFunction));
        descriptor.ReturnType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when the binding class itself is not public.
    /// </summary>
    [Fact]
    public void Scan_WithNonPublicBindingClass_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestNonPublicBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a bound method is an instance method.
    /// </summary>
    [Fact]
    public void Scan_WithInstanceMethod_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestInstanceMethodBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a bound method is not public.
    /// </summary>
    [Fact]
    public void Scan_WithPrivateMethod_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestPrivateMethodBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a host-injected parameter precedes a bound parameter.
    /// </summary>
    [Fact]
    public void Scan_WithHostInjectedParameterBeforeBoundParameter_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestHostInjectedBeforeBoundBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a bound method has a parameter of an unmapped CLR type.
    /// </summary>
    [Fact]
    public void Scan_WithUnmappedParameterType_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestUnmappedParameterTypeBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a bound method returns an unmapped CLR type.
    /// </summary>
    [Fact]
    public void Scan_WithUnmappedReturnType_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestUnmappedReturnTypeBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when the Topsy Turvy name of a function is not a valid identifier.
    /// </summary>
    [Fact]
    public void Scan_WithInvalidIdentifierName_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestInvalidIdentifierNameBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when the renamed identifier of a bound parameter is invalid.
    /// </summary>
    [Fact]
    public void Scan_WithInvalidParameterName_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestInvalidParameterNameBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> throws when a namespace path segment is not a valid identifier.
    /// </summary>
    [Fact]
    public void Scan_WithInvalidNamespaceSegment_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingScanner.Scan(typeof(TestInvalidNamespaceSegmentBindingClass)));
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> joins multiple namespace path segments with a dot.
    /// </summary>
    [Fact]
    public void Scan_WithMultiSegmentNamespace_JoinsSegmentsWithDot()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.NamespacedFunction));
        descriptor.Namespace.ShouldBe("Test.Namespace");
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> treats an empty namespace segment array as the global namespace.
    /// </summary>
    [Fact]
    public void Scan_WithNoNamespace_ResolvesToGlobalNamespace()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));
        descriptor.Namespace.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> maps an array-typed parameter to <see cref="LiteralType.Array"/>
    /// with the correct element type.
    /// </summary>
    [Fact]
    public void Scan_WithArrayParameter_MapsToArrayWithElementType()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestArrayBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestArrayBindingClass.Count));
        descriptor.Parameters.ShouldHaveSingleItem();
        descriptor.Parameters[0].Type.ShouldBe(LiteralType.Array);
        descriptor.Parameters[0].ArrayElementType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> maps an array-typed return value to <see cref="LiteralType.Array"/>
    /// with the correct element type.
    /// </summary>
    [Fact]
    public void Scan_WithArrayReturnType_MapsToArrayWithElementType()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestArrayBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestArrayBindingClass.Range));
        descriptor.ReturnType.ShouldBe(LiteralType.Array);
        descriptor.ReturnElementType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that <see cref="BindingScanner.Scan"/> leaves <see cref="BoundParameter.ArrayElementType"/> and
    /// <see cref="BoundFunctionDescriptor.ReturnElementType"/> <c>null</c> for a non-array parameter and return type.
    /// </summary>
    [Fact]
    public void Scan_WithNonArrayParameterAndReturnType_LeavesElementTypeNull()
    {
        IReadOnlyList<BoundFunctionDescriptor> descriptors = BindingScanner.Scan(typeof(TestValidBindingClass));

        BoundFunctionDescriptor descriptor = descriptors.Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.ValueFunction));
        descriptor.ReturnElementType.ShouldBeNull();
    }
}
