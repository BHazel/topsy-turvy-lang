namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Holds the native callback function pointers registered by the caller at session creation.
/// </summary>
/// <remarks>
/// This is a storage convenience for <see cref="NativeSession"/>: it is never exposed across the C ABI as
/// a struct parameter. <see cref="NativeExports.ToolchainExports.CreateSession"/> takes the two function pointers as
/// separate parameters.
/// </remarks>
/// <param name="outputLine">The callback invoked once per output line written by a running programme.</param>
/// <param name="resolveImport">The callback invoked to resolve a <c>PRAY ADMIT</c> import filename to source text.</param>
/// <param name="inputLine">The callback invoked to read one line of input on demand, or a null pointer if the caller did not register one.</param>
/// <param name="context">
/// A caller-supplied "userdata" pointer, for example one pointing at Swift-side state the caller wants to
/// associate with this session. This library never dereferences or interprets the value: it stores it and
/// passes it back, unchanged, as the first argument to every invocation of <paramref name="outputLine"/>,
/// <paramref name="resolveImport"/> and <paramref name="inputLine"/>, so the caller can recover which session
/// or object a callback invocation belongs to, since a C function pointer cannot itself capture that context.
/// </param>
public readonly unsafe struct NativeCallbacks(
    delegate* unmanaged<nint, byte*, byte, void> outputLine,
    delegate* unmanaged<nint, byte*, byte*> resolveImport,
    delegate* unmanaged<nint, byte*> inputLine,
    nint context)
{
    /// <summary>
    /// Gets the callback invoked once per output line written by a running programme.
    /// </summary>
    public delegate* unmanaged<nint, byte*, byte, void> OutputLine { get; } = outputLine;

    /// <summary>
    /// Gets the callback invoked to resolve a <c>PRAY ADMIT</c> import filename to source text.
    /// </summary>
    public delegate* unmanaged<nint, byte*, byte*> ResolveImport { get; } = resolveImport;

    /// <summary>
    /// Gets the callback invoked to read one line of input on demand, or a null pointer if the caller did
    /// not register one.
    /// </summary>
    public delegate* unmanaged<nint, byte*> InputLine { get; } = inputLine;

    /// <summary>
    /// Gets the caller-supplied "userdata" pointer passed back unmodified as the first argument to every callback.
    /// </summary>
    public nint Context { get; } = context;
}
