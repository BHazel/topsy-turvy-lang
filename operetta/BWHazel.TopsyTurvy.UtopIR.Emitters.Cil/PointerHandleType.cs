using System;
using System.Reflection.Emit;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Holds the reflection handles for the <c>PointerHandle</c> value type that <see cref="CilEmitter"/>
/// defines in every emitted assembly, so IL that constructs or reads a UtopIR pointer can reference
/// its <see cref="Type"/>, <see cref="Container"/> and <see cref="Index"/> fields.
/// </summary>
/// <remarks>
/// A UtopIR pointer is represented at runtime as this managed handle, not a real CLR pointer or
/// byref. <see cref="Container"/> holds the CLR array or string being pointed into, and
/// <see cref="Index"/> is the 0-based position within it. There is no separate field recording
/// whether a handle refers into a <c>yarn</c> or an array: code that needs to know tests the runtime
/// type of <see cref="Container"/> directly with <c>isinst</c>. This distinction matters because a
/// <c>yarn</c> is backed by an immutable string and so cannot be written through, while an array can.
/// </remarks>
/// <param name="Type">The finalised <c>PointerHandle</c> CLR type.</param>
/// <param name="Container">The <c>Container</c> field.</param>
/// <param name="Index">The <c>Index</c> field.</param>
internal sealed record PointerHandleType(Type Type, FieldBuilder Container, FieldBuilder Index);
