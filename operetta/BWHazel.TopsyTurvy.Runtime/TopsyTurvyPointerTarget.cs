using System.Collections.Generic;
using System.Threading;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents the managed location a Topsy Turvy pointer refers to.
/// </summary>
/// <remarks>
/// <para>
/// This is the runtime handle wrapped by a <see cref="TopsyTurvyValue"/> of <see cref="LiteralType"/><c>.Pointer</c>,
/// created by the <c>GALLERY PICTURE TO</c> address-of expression.  No unmanaged memory or raw address is involved:
/// a target is either a reference to a variable slot in a <see cref="TopsyTurvyEnvironment"/>, a position within a
/// shared array element list, or a character position within a string variable.  This mirrors how
/// <see cref="TopsyTurvyValue.Array"/> already wraps a shared <c>List&lt;TopsyTurvyValue&gt;</c> by reference rather
/// than copying it.
/// </para>
/// <para>
/// Three variants are supported, selected by the factory method used to create the target:
/// </para>
/// <para>
/// * <see cref="ForVariable"/>: refers directly to a named variable.  Reading and writing delegate to
///   <see cref="TopsyTurvyEnvironment.Get"/> and <see cref="TopsyTurvyEnvironment.Assign"/>.  This variant does not
///   support pointer arithmetic, as a single variable has no notion of a next element.
/// * <see cref="ForArrayElement"/>: refers to a position within a shared element list.  Reading and writing index
///   directly into the list, and pointer arithmetic produces a new target at an adjusted, bounds-checked position.
/// * <see cref="ForStringElement"/>: refers to a character position within a named string variable.  Because
///   strings are immutable CLR values rather than reference types, this variant re-reads the current string from the
///   environment on every access instead of holding a copy, so it always reflects the variable current value.
///   Writing through this variant is always rejected, as string characters cannot be reassigned individually.
/// </para>
/// </remarks>
public sealed class TopsyTurvyPointerTarget
{
    /// <summary>
    /// The handle identifier to be assigned to the next <see cref="TopsyTurvyPointerTarget"/> created.
    /// </summary>
    /// <remarks>
    /// Shared across every target in the process so that no two targets ever display the same synthetic address.
    /// </remarks>
    private static long nextHandleId;

    /// <summary>
    /// The kind of location this target refers to.
    /// </summary>
    /// <remarks>
    /// Determines which of the remaining fields are populated and how <see cref="Read"/>,
    /// <see cref="Write"/> and <see cref="WithOffset"/> behave.
    /// </remarks>
    private readonly TopsyTurvyPointerTargetKind kind;

    /// <summary>
    /// The environment the target variable is declared in.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="TopsyTurvyPointerTargetKind.Variable"/>
    /// and <see cref="TopsyTurvyPointerTargetKind.StringElement"/> targets, since both resolve the current value by
    /// name from an environment rather than holding a direct reference; <c>null</c> for
    /// <see cref="TopsyTurvyPointerTargetKind.ArrayElement"/> targets, which hold the shared list directly instead.
    /// </remarks>
    private readonly TopsyTurvyEnvironment? environment;

    /// <summary>
    /// The name of the target variable within <see cref="environment"/>.
    /// </summary>
    /// <remarks>
    /// Populated for <see cref="TopsyTurvyPointerTargetKind.Variable"/> and
    /// <see cref="TopsyTurvyPointerTargetKind.StringElement"/> targets; <c>null</c> for 
    /// <see cref="TopsyTurvyPointerTargetKind.ArrayElement"/> targets.
    /// </remarks>
    private readonly string? variableName;

    /// <summary>
    /// The shared element list this target refers into.
    /// </summary>
    /// <remarks>
    /// Populated only for <see cref="TopsyTurvyPointerTargetKind.ArrayElement"/> targets;
    /// <c>null</c> otherwise.  Held directly, rather than looked up by name, because arrays
    /// are already reference types in the runtime value model, so the list instance itself is
    /// the shared storage.
    /// </remarks>
    private readonly List<TopsyTurvyValue>? elements;

    /// <summary>
    /// The 0-based position within <see cref="elements"/> or the target string.
    /// </summary>
    /// <remarks>
    /// Unused (always <c>0</c>) for <see cref="TopsyTurvyPointerTargetKind.Variable"/> targets,
    /// which have no notion of a position.
    /// </remarks>
    private readonly int index;

    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyPointerTarget"/> class.
    /// </summary>
    /// <param name="kind">The kind of location this target refers to.</param>
    /// <param name="environment">The environment the target variable is declared in, when applicable.</param>
    /// <param name="variableName">The name of the target variable, when applicable.</param>
    /// <param name="elements">The shared element list the target refers into, when applicable.</param>
    /// <param name="index">The 0-based position within <paramref name="elements"/> or the target string, when applicable.</param>
    private TopsyTurvyPointerTarget(
        TopsyTurvyPointerTargetKind kind,
        TopsyTurvyEnvironment? environment,
        string? variableName,
        List<TopsyTurvyValue>? elements,
        int index)
    {
        this.kind = kind;
        this.environment = environment;
        this.variableName = variableName;
        this.elements = elements;
        this.index = index;
        this.HandleId = Interlocked.Increment(ref nextHandleId);
    }

    /// <summary>
    /// Gets the monotonic identifier assigned to this target when it was created.
    /// </summary>
    /// <remarks>
    /// Used only to produce a stable, address-shaped display string for <c>BEHOLD</c>; it has no relation to any
    /// real memory location.
    /// </remarks>
    public long HandleId { get; }

    /// <summary>
    /// Gets a value indicating whether this target supports pointer arithmetic.
    /// </summary>
    /// <remarks>
    /// Only <see cref="ForArrayElement"/> and <see cref="ForStringElement"/> targets support arithmetic; a
    /// <see cref="ForVariable"/> target has no notion of a next element.
    /// </remarks>
    public bool SupportsArithmetic => this.kind != TopsyTurvyPointerTargetKind.Variable;

    /// <summary>
    /// Creates a target referring directly to a named variable.
    /// </summary>
    /// <param name="environment">The environment the variable is declared in.</param>
    /// <param name="variableName">The name of the variable.</param>
    /// <returns>A new <see cref="TopsyTurvyPointerTarget"/> referring to the variable.</returns>
    public static TopsyTurvyPointerTarget ForVariable(TopsyTurvyEnvironment environment, string variableName) =>
        new(TopsyTurvyPointerTargetKind.Variable, environment, variableName, elements: null, index: 0);

    /// <summary>
    /// Creates a target referring to a position within a shared array element list.
    /// </summary>
    /// <param name="elements">The shared element list.</param>
    /// <param name="index">The 0-based position within <paramref name="elements"/>.</param>
    /// <returns>A new <see cref="TopsyTurvyPointerTarget"/> referring to the array position.</returns>
    public static TopsyTurvyPointerTarget ForArrayElement(List<TopsyTurvyValue> elements, int index) =>
        new(TopsyTurvyPointerTargetKind.ArrayElement, environment: null, variableName: null, elements, index);

    /// <summary>
    /// Creates a target referring to a character position within a named string variable.
    /// </summary>
    /// <param name="environment">The environment the string variable is declared in.</param>
    /// <param name="variableName">The name of the string variable.</param>
    /// <param name="index">The 0-based character position within the string.</param>
    /// <returns>A new <see cref="TopsyTurvyPointerTarget"/> referring to the character position.</returns>
    public static TopsyTurvyPointerTarget ForStringElement(TopsyTurvyEnvironment environment, string variableName, int index) =>
        new(TopsyTurvyPointerTargetKind.StringElement, environment, variableName, elements: null, index);

    /// <summary>
    /// Reads the value currently at this target.
    /// </summary>
    /// <param name="span">The source span used in error messages.</param>
    /// <returns>The current value at this target.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the target position is out of bounds.</exception>
    public TopsyTurvyValue Read(SourceSpan span) => this.kind switch
    {
        TopsyTurvyPointerTargetKind.Variable => this.environment!.Get(this.variableName!),
        TopsyTurvyPointerTargetKind.ArrayElement => this.ReadArrayElement(span),
        TopsyTurvyPointerTargetKind.StringElement => this.ReadStringElement(span),
        _ => throw new TopsyTurvyRuntimeException("Unhandled pointer target kind.", span)
    };

    /// <summary>
    /// Writes a new value through this target.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when the target position is out of bounds, or when this target refers to a string character, since
    /// string characters cannot be reassigned individually.
    /// </exception>
    public void Write(TopsyTurvyValue value, SourceSpan span)
    {
        switch (this.kind)
        {
            case TopsyTurvyPointerTargetKind.Variable:
                this.environment!.Assign(this.variableName!, value);
                return;
            case TopsyTurvyPointerTargetKind.ArrayElement:
                this.CheckArrayBounds(this.index, span);
                this.elements![this.index] = value;
                return;
            case TopsyTurvyPointerTargetKind.StringElement:
                throw new TopsyTurvyRuntimeException(
                    $"Cannot write through a pointer to '{this.variableName}': its characters cannot be reassigned individually.",
                    span);
            default:
                throw new TopsyTurvyRuntimeException("Unhandled pointer target kind.", span);
        }
    }

    /// <summary>
    /// Creates a new target at this target position shifted by the given offset.
    /// </summary>
    /// <param name="offset">The signed number of elements to move forward, negative to move backward.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <returns>A new <see cref="TopsyTurvyPointerTarget"/> at the shifted position.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when this target does not support arithmetic, or when the shifted position is out of bounds.
    /// </exception>
    public TopsyTurvyPointerTarget WithOffset(long offset, SourceSpan span)
    {
        switch (this.kind)
        {
            case TopsyTurvyPointerTargetKind.Variable:
                throw new TopsyTurvyRuntimeException(
                    "Pointer arithmetic requires a pointer to an array or YARN element: this pointer refers to a single variable.",
                    span);
            case TopsyTurvyPointerTargetKind.ArrayElement:
                long newArrayIndex = this.index + offset;
                this.CheckArrayBounds(newArrayIndex, span);
                return ForArrayElement(this.elements!, (int)newArrayIndex);
            case TopsyTurvyPointerTargetKind.StringElement:
                long newStringIndex = this.index + offset;
                this.CheckStringBounds(newStringIndex, span);
                return ForStringElement(this.environment!, this.variableName!, (int)newStringIndex);
            default:
                throw new TopsyTurvyRuntimeException("Unhandled pointer target kind.", span);
        }
    }

    /// <summary>
    /// Returns the synthetic, address-shaped display string for this target.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="HandleId"/>; the result has no relation to any real memory address.
    /// </remarks>
    /// <returns>A hexadecimal string in the form <c>0xNNNNNNNN</c>.</returns>
    public override string ToString() => "0x" + this.HandleId.ToString("X8");

    /// <summary>
    /// Reads the current element at this target position within the shared array element list.
    /// </summary>
    /// <param name="span">The source span used in error messages.</param>
    /// <returns>The element at this target position.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the target position is out of bounds.</exception>
    private TopsyTurvyValue ReadArrayElement(SourceSpan span)
    {
        this.CheckArrayBounds(this.index, span);
        return this.elements![this.index];
    }

    /// <summary>
    /// Reads the current character at this target position within the target string variable current value.
    /// </summary>
    /// <param name="span">The source span used in error messages.</param>
    /// <returns>The character at this target position, as a <c>STITCH</c> value.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the target position is out of bounds.</exception>
    private TopsyTurvyValue ReadStringElement(SourceSpan span)
    {
        string current = (string)this.environment!.Get(this.variableName!).RawValue!;
        this.CheckStringBounds(this.index, current.Length, span);
        return TopsyTurvyValue.Char(current[this.index]);
    }

    /// <summary>
    /// Validates that a 0-based position falls within the bounds of the target array element list.
    /// </summary>
    /// <param name="candidateIndex">The 0-based position to validate.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the position is out of bounds.</exception>
    private void CheckArrayBounds(long candidateIndex, SourceSpan span)
    {
        if (candidateIndex < 0 || candidateIndex >= this.elements!.Count)
        {
            throw new TopsyTurvyRuntimeException(
                $"Pointer is out of bounds: resulting index {candidateIndex + 1} for an array of length {this.elements!.Count}.",
                span);
        }
    }

    /// <summary>
    /// Validates that a 0-based position falls within the bounds of the target string current value.
    /// </summary>
    /// <param name="candidateIndex">The 0-based position to validate.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the position is out of bounds.</exception>
    private void CheckStringBounds(long candidateIndex, SourceSpan span)
    {
        string current = (string)this.environment!.Get(this.variableName!).RawValue!;
        this.CheckStringBounds(candidateIndex, current.Length, span);
    }

    /// <summary>
    /// Validates that a 0-based position falls within the bounds of a string of the given length.
    /// </summary>
    /// <param name="candidateIndex">The 0-based position to validate.</param>
    /// <param name="length">The length of the target string current value.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the position is out of bounds.</exception>
    private void CheckStringBounds(long candidateIndex, int length, SourceSpan span)
    {
        if (candidateIndex < 0 || candidateIndex >= length)
        {
            throw new TopsyTurvyRuntimeException(
                $"Pointer is out of bounds: resulting index {candidateIndex + 1} for a YARN of length {length}.",
                span);
        }
    }
}
