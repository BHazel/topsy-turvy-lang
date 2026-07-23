using System;
using System.Runtime.InteropServices;
using BWHazel.TopsyTurvy.Embedded.NativeInterop;
using BWHazel.TopsyTurvy.StandardLibrary;

namespace BWHazel.TopsyTurvy.Embedded.NativeExports.StandardLibrary;

/// <summary>
/// Provides the native export surface for the Standard Library <see cref="Global"/> namespace, consumed
/// across the C ABI boundary.
/// </summary>
/// <remarks>
/// <para>
/// Every export is <see cref="UnmanagedCallersOnlyAttribute"/>-annotated with an explicit <c>EntryPoint</c>
/// naming the exported C symbol.
/// </para>
/// <para>
/// All exported symbols for the global namespace of the Standard Library are prefixed with <c>topsyturvy_std_</c>.
/// </para>
/// </remarks>
public static unsafe class GlobalExports
{
    /// <summary>
    /// Prints text through the session output callback via the Standard Library
    /// <see cref="Global.PreviewBehold"/> function.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="textUtf8">A null-terminated, UTF-8 encoded pointer to the text to print.</param>
    /// <param name="withCeremony">Non-zero to apply a trailing newline; zero to suppress it.</param>
    /// <returns><c>0</c> on success; <c>1</c> if the session handle is invalid or the call failed.</returns>
    /// <remarks>
    /// Routes through <see cref="BufferedNativeIO"/> rather than invoking <see cref="NativeCallbacks.OutputLine"/>
    /// directly, so this direct call and a <c>SUMMON PreviewBehold</c> reached mid-programme during
    /// <c>ToolchainExports.ExecuteProgramme</c> exercise the identical code path.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_std_preview_behold")]
    public static int PreviewBehold(nint session, byte* textUtf8, byte withCeremony)
    {
        if (!NativeExportSupport.TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return 1;
        }

        try
        {
            string text = Marshal.PtrToStringUTF8((nint)textUtf8) ?? string.Empty;
            BufferedNativeIO io = new([], nativeSession.Callbacks);
            Global.PreviewBehold(text, withCeremony != 0, io);
            return 0;
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return 1;
        }
    }

    /// <summary>
    /// Reads a line through the session input callback via the Standard Library
    /// <see cref="Global.PreviewPrayTell"/> function.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded buffer the caller must release via <c>ToolchainExports.FreeBuffer</c>,
    /// or a null pointer if the session handle is invalid or the call failed.
    /// </returns>
    /// <remarks>
    /// Routes through <see cref="BufferedNativeIO"/> rather than invoking <see cref="NativeCallbacks.InputLine"/>
    /// directly, so this direct call and a <c>SUMMON PreviewPrayTell</c> reached mid-programme during
    /// <c>ToolchainExports.ExecuteProgramme</c> exercise the identical code path.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_std_preview_pray_tell")]
    public static byte* PreviewPrayTell(nint session)
    {
        if (!NativeExportSupport.TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            BufferedNativeIO io = new([], nativeSession.Callbacks);
            return NativeExportSupport.AllocateUtf8String(Global.PreviewPrayTell(io));
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }
}
