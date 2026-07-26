using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Invokes catalogue-bound external functions on behalf of the interpreter.
/// </summary>
/// <remarks>
/// This is the only place in the toolchain that marshals a value between the <see cref="TopsyTurvyValue"/> world the interpreter
/// works in and the CLR world a bound method runs in, and the only place that calls <see cref="MethodInfo"/>.Invoke
/// on a bound method. An argument is converted to its parameter type using the same widening rules ordinary Topsy Turvy assignment
/// already follows, for example passing a <c>PEER</c> where a <c>CHANCELLOR</c> is expected; a mismatch that is not a widening
/// conversion is a runtime error. The one host-injected service currently supported is <see cref="ITopsyTurvyIO"/>: a trailing
/// parameter of that type is filled in automatically with the value supplied to <see cref="Invoke"/>, since Topsy Turvy code
/// never supplies one.
/// </remarks>
/// <param name="catalogue">The catalogue of external functions available to invoke.</param>
public sealed class ExternalFunctionInvoker(BindingCatalogue catalogue)
{
    private static readonly LiteralType[] NumericWideningOrder =
    [
        LiteralType.Double,
        LiteralType.Single,
        LiteralType.UnsignedLong,
        LiteralType.Long,
        LiteralType.UnsignedInteger,
        LiteralType.Integer,
        LiteralType.UnsignedShort,
        LiteralType.Short,
        LiteralType.Byte,
        LiteralType.SignedByte,
    ];

    /// <summary>
    /// Attempts to resolve the given name to a bound function descriptor.
    /// </summary>
    /// <param name="qualifiedName">The bare or dot-joined qualified name to resolve.</param>
    /// <returns>The matching descriptor, or <c>null</c> if none exists in the catalogue.</returns>
    public BoundFunctionDescriptor? Find(string qualifiedName) => catalogue.Find(qualifiedName);

    /// <summary>
    /// Invokes the described external function with the given evaluated arguments.
    /// </summary>
    /// <param name="descriptor">The function to invoke.</param>
    /// <param name="arguments">The evaluated argument values, in call order, excluding any host-injected slot.</param>
    /// <param name="io">The input/output implementation of the interpreter, supplied to every trailing host-injected parameter.</param>
    /// <returns>The returned value, or <c>null</c> for a void function.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when an argument cannot be widened to its parameter type, or when the invoked function itself throws.</exception>
    public TopsyTurvyValue? Invoke(BoundFunctionDescriptor descriptor, IReadOnlyList<TopsyTurvyValue> arguments, ITopsyTurvyIO io)
    {
        object?[] clrArguments = new object?[descriptor.Parameters.Count + descriptor.HostInjectedParameterCount];
        for (int i = 0; i < descriptor.Parameters.Count; i++)
        {
            clrArguments[i] = ConvertArgument(arguments[i], descriptor.Parameters[i], descriptor.Name);
        }

        if (descriptor.HostInjectedParameterCount > 1)
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{descriptor.Name}' declares {descriptor.HostInjectedParameterCount} host-injected parameters; only one host-injected service, {nameof(ITopsyTurvyIO)}, is currently supported.");
        }

        if (descriptor.HostInjectedParameterCount == 1)
        {
            clrArguments[descriptor.Parameters.Count] = io;
        }

        object? result;
        try
        {
            result = descriptor.Method.Invoke(null, clrArguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is TopsyTurvyThrowException or TopsyTurvyRuntimeException)
        {
            throw ex.InnerException;
        }
        catch (Exception ex)
        {
            string reason = ex is TargetInvocationException { InnerException: Exception inner }
                ? inner.Message
                : ex.Message;

            throw new TopsyTurvyRuntimeException($"Function '{descriptor.Name}' threw an unhandled exception: {reason}");
        }

        return descriptor.ReturnType is null
            ? null
            : WrapReturnValue(result, descriptor.ReturnType.Value, descriptor.ReturnElementType);
    }

    /// <summary>
    /// Converts one evaluated argument to the CLR value its bound parameter expects.
    /// </summary>
    /// <param name="argument">The evaluated argument.</param>
    /// <param name="parameter">The parameter it is being passed to.</param>
    /// <param name="functionName">The name of the function being invoked, for the error message.</param>
    /// <returns>The CLR value ready to pass to <see cref="MethodInfo"/>.Invoke.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the argument type is not the parameter type and is not a widening conversion to it.</exception>
    private static object ConvertArgument(TopsyTurvyValue argument, BoundParameter parameter, string functionName)
    {
        if (parameter.Type == LiteralType.Array)
        {
            return ConvertArrayArgument(argument, parameter, functionName);
        }

        return ConvertValue(
            argument,
            parameter.Type,
            $"Function '{functionName}' expects parameter '{parameter.Name}' to be {LiteralTypeNames.ToDisplayName(parameter.Type)}, got {LiteralTypeNames.ToDisplayName(argument.LiteralType)}.");
    }

    /// <summary>
    /// Converts an evaluated array argument to a real CLR array matching the bound parameter element type.
    /// </summary>
    /// <param name="argument">The evaluated array argument.</param>
    /// <param name="parameter">The array parameter it is being passed to.</param>
    /// <param name="functionName">The name of the function being invoked, for the error message.</param>
    /// <returns>A CLR array of the <paramref name="parameter"/> element CLR type.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the argument is not an array, or an element cannot be widened to the declared element type.</exception>
    /// <remarks>
    /// A Topsy Turvy array value is boxed as <c>List&lt;TopsyTurvyValue&gt;</c> in the interpreter, unlike a
    /// UtopIR-compiled array, which is already a real CLR array by the time it reaches a bound method. This is the
    /// one place that difference is bridged.
    /// </remarks>
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "The element type always comes from the ClrTypeMap fixed, closed set of scalar CLR types, never an arbitrary caller-supplied type, so Array.CreateInstance here is safe in practice; a genuine Native AOT publish of this path is not exercised today since no Standard Library function has an array parameter yet, and Embedded is explicitly out of scope for external library support.")]
    private static object ConvertArrayArgument(TopsyTurvyValue argument, BoundParameter parameter, string functionName)
    {
        if (argument.LiteralType != LiteralType.Array)
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{functionName}' expects parameter '{parameter.Name}' to be {LiteralTypeNames.ToDisplayName(parameter.Type)}, got {LiteralTypeNames.ToDisplayName(argument.LiteralType)}.");
        }

        List<TopsyTurvyValue> elements = (List<TopsyTurvyValue>)argument.RawValue!;
        Type elementClrType = parameter.ClrType.GetElementType()!;
        LiteralType elementType = parameter.ArrayElementType!.Value;
        Array clrArray = Array.CreateInstance(elementClrType, elements.Count);

        for (int i = 0; i < elements.Count; i++)
        {
            object elementValue = ConvertValue(
                elements[i],
                elementType,
                $"Function '{functionName}' expects element {i} of parameter '{parameter.Name}' to be {LiteralTypeNames.ToDisplayName(elementType)}, got {LiteralTypeNames.ToDisplayName(elements[i].LiteralType)}.");

            clrArray.SetValue(elementValue, i);
        }

        return clrArray;
    }

    /// <summary>
    /// Converts a single value to a CLR value of the given target type, applying the same widening rules as
    /// ordinary Topsy Turvy assignment.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The type to convert it to.</param>
    /// <param name="errorMessage">The message to use if the conversion is not exact or widening.</param>
    /// <returns>The converted CLR value.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the value type is not the target type and is not a widening conversion to it.</exception>
    private static object ConvertValue(TopsyTurvyValue value, LiteralType targetType, string errorMessage)
    {
        if (value.LiteralType == targetType)
        {
            return value.RawValue!;
        }

        if (!IsNumericType(value.LiteralType) || !IsNumericType(targetType) || !IsWidening(targetType, value.LiteralType))
        {
            throw new TopsyTurvyRuntimeException(errorMessage);
        }

        return value.CastTo(targetType).RawValue!;
    }

    /// <summary>
    /// Determines whether a value of <paramref name="valueType"/> widens to <paramref name="declaredType"/>, using the same
    /// numeric widening order as ordinary Topsy Turvy assignment.
    /// </summary>
    /// <param name="declaredType">The declared parameter type.</param>
    /// <param name="valueType">The type of the value being passed.</param>
    /// <returns><c>true</c> if the conversion is widening or exact, otherwise <c>false</c>.</returns>
    private static bool IsWidening(LiteralType declaredType, LiteralType valueType) =>
        IndexOfNumericWidening(declaredType) <= IndexOfNumericWidening(valueType);

    /// <summary>
    /// Determines whether a <see cref="LiteralType"/> is one of the numeric types eligible for widening.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is numeric, otherwise <c>false</c>.</returns>
    private static bool IsNumericType(LiteralType type) => IndexOfNumericWidening(type) != int.MaxValue;

    /// <summary>
    /// Returns the index of a numeric type in the widening order, or <see cref="int.MaxValue"/> if the type is not numeric.
    /// </summary>
    /// <param name="type">The type to look up.</param>
    /// <returns>The index of the type in the widening order.</returns>
    private static int IndexOfNumericWidening(LiteralType type)
    {
        for (int i = 0; i < NumericWideningOrder.Length; i++)
        {
            if (NumericWideningOrder[i] == type)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    /// <summary>
    /// Wraps a CLR return value back into a <see cref="TopsyTurvyValue"/> of the given type.
    /// </summary>
    /// <param name="clrValue">The raw CLR return value.</param>
    /// <param name="returnType">The Topsy Turvy type to wrap it as.</param>
    /// <param name="arrayElementType">The element type when <paramref name="returnType"/> is <see cref="LiteralType.Array"/>, otherwise <c>null</c>.</param>
    /// <returns>The wrapped value.</returns>
    private static TopsyTurvyValue WrapReturnValue(object? clrValue, LiteralType returnType, LiteralType? arrayElementType) => returnType switch
    {
        LiteralType.Integer => TopsyTurvyValue.Integer((int)clrValue!),
        LiteralType.Long => TopsyTurvyValue.Long((long)clrValue!),
        LiteralType.Short => TopsyTurvyValue.Short((short)clrValue!),
        LiteralType.SignedByte => TopsyTurvyValue.SignedByte((sbyte)clrValue!),
        LiteralType.UnsignedInteger => TopsyTurvyValue.UnsignedInteger((uint)clrValue!),
        LiteralType.UnsignedLong => TopsyTurvyValue.UnsignedLong((ulong)clrValue!),
        LiteralType.UnsignedShort => TopsyTurvyValue.UnsignedShort((ushort)clrValue!),
        LiteralType.Byte => TopsyTurvyValue.Byte((byte)clrValue!),
        LiteralType.Double => TopsyTurvyValue.Double((double)clrValue!),
        LiteralType.Single => TopsyTurvyValue.Single((float)clrValue!),
        LiteralType.String => TopsyTurvyValue.String((string)clrValue!),
        LiteralType.Char => TopsyTurvyValue.Char((char)clrValue!),
        LiteralType.Boolean => TopsyTurvyValue.Boolean((bool)clrValue!),
        LiteralType.Array => WrapArrayReturnValue((Array)clrValue!, arrayElementType!.Value),
        _ => throw new TopsyTurvyRuntimeException($"Function returned an unsupported type: {returnType}.")
    };

    /// <summary>
    /// Wraps a CLR array return value back into a Topsy Turvy array value, element by element.
    /// </summary>
    /// <param name="clrArray">The CLR array returned by the bound method.</param>
    /// <param name="elementType">The Topsy Turvy type of each element.</param>
    /// <returns>The wrapped array value.</returns>
    private static TopsyTurvyValue WrapArrayReturnValue(Array clrArray, LiteralType elementType)
    {
        List<TopsyTurvyValue> elements = new(clrArray.Length);
        foreach (object? element in clrArray)
        {
            elements.Add(WrapReturnValue(element, elementType, null));
        }

        return TopsyTurvyValue.Array(elements);
    }
}
