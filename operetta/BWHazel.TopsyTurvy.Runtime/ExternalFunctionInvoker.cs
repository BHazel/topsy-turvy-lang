using System;
using System.Collections.Generic;
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
            : WrapReturnValue(result, descriptor.ReturnType.Value);
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
        if (argument.LiteralType == parameter.Type)
        {
            return argument.RawValue!;
        }

        if (!IsNumericType(argument.LiteralType) || !IsNumericType(parameter.Type) || !IsWidening(parameter.Type, argument.LiteralType))
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{functionName}' expects parameter '{parameter.Name}' to be {LiteralTypeNames.ToDisplayName(parameter.Type)}, got {LiteralTypeNames.ToDisplayName(argument.LiteralType)}.");
        }

        return argument.CastTo(parameter.Type).RawValue!;
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
    /// <returns>The wrapped value.</returns>
    private static TopsyTurvyValue WrapReturnValue(object? clrValue, LiteralType returnType) => returnType switch
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
        _ => throw new TopsyTurvyRuntimeException($"Function returned an unsupported type: {returnType}.")
    };
}
