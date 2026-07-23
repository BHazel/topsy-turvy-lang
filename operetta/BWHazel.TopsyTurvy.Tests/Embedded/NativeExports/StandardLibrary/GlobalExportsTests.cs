using System.Runtime.InteropServices;
using BWHazel.TopsyTurvy.Embedded.NativeExports.StandardLibrary;

namespace BWHazel.TopsyTurvy.Tests.Embedded.NativeExports.StandardLibrary;

/// <summary>
/// Tests for the <see cref="GlobalExports"/> class.
/// </summary>
/// <remarks>
/// Every export is <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>-annotated, so
/// the C# compiler forbids calling them directly even from in-process test code.  A function
/// pointer must be obtained via <c>&amp;GlobalExports.Method</c> and invoked through that, exactly as a
/// native caller would resolve the exported symbol.  All pointer marshalling is isolated in the private
/// static helper methods below, so the tests themselves only ever deal in plain managed types.
/// </remarks>
public class GlobalExportsTests
{
    /// <summary>
    /// Tests that <see cref="GlobalExports.PreviewBehold"/> invokes the registered output callback directly,
    /// without a running programme.
    /// </summary>
    [Fact]
    public void PreviewBehold_WhenCalledDirectly_InvokesOutputCallback()
    {
        TestCallbackCapture.Reset();
        nint session = NativeExportsTestSupport.CreateSession();

        try
        {
            int status = PreviewBehold(session, "Hello from preview", withCeremony: true);

            status.ShouldBe(0);
            TestCallbackCapture.OutputLines.ShouldContain("Hello from preview");
        }
        finally
        {
            NativeExportsTestSupport.DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="GlobalExports.PreviewBehold"/> given <c>with_ceremony = 0</c> suppresses the
    /// trailing newline, appending to the previous output line rather than starting a new one.
    /// </summary>
    [Fact]
    public void PreviewBehold_WithoutCeremony_SuppressesTrailingNewline()
    {
        TestCallbackCapture.Reset();
        nint session = NativeExportsTestSupport.CreateSession();

        try
        {
            PreviewBehold(session, "first", withCeremony: false);
            PreviewBehold(session, "second", withCeremony: false);

            TestCallbackCapture.OutputLines.ShouldContain("firstsecond");
        }
        finally
        {
            NativeExportsTestSupport.DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="GlobalExports.PreviewBehold"/> given an invalid session handle returns <c>1</c>.
    /// </summary>
    [Fact]
    public void PreviewBehold_WithInvalidSession_ReturnsOne()
    {
        int status = PreviewBehold(0, "text", withCeremony: true);

        status.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <see cref="GlobalExports.PreviewPrayTell"/> invokes the registered input callback directly,
    /// without a running programme.
    /// </summary>
    [Fact]
    public void PreviewPrayTell_WhenCalledDirectly_ReadsFromInputCallback()
    {
        TestCallbackCapture.Reset();
        TestCallbackCapture.SetInputLineResult("a line typed by the user");
        nint session = NativeExportsTestSupport.CreateSession();

        try
        {
            string? line = PreviewPrayTell(session);

            line.ShouldBe("a line typed by the user");
        }
        finally
        {
            NativeExportsTestSupport.DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="GlobalExports.PreviewPrayTell"/> given an invalid session handle returns <c>null</c>.
    /// </summary>
    [Fact]
    public void PreviewPrayTell_WithInvalidSession_ReturnsNull()
    {
        string? line = PreviewPrayTell(0);

        line.ShouldBeNull();
    }

    /// <summary>
    /// Calls <see cref="GlobalExports.PreviewBehold"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="text">The text to print.</param>
    /// <param name="withCeremony">Whether a trailing newline is applied.</param>
    /// <returns>The status code returned by <see cref="GlobalExports.PreviewBehold"/>.</returns>
    private static unsafe int PreviewBehold(nint session, string text, bool withCeremony)
    {
        delegate* unmanaged<nint, byte*, byte, int> previewBehold = &GlobalExports.PreviewBehold;

        nint textPointer = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            return previewBehold(session, (byte*)textPointer, withCeremony
                ? (byte)1
                : (byte)0);
        }
        finally
        {
            Marshal.FreeCoTaskMem(textPointer);
        }
    }

    /// <summary>
    /// Calls <see cref="GlobalExports.PreviewPrayTell"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <returns>The line read, or <c>null</c> if the call failed.</returns>
    private static unsafe string? PreviewPrayTell(nint session)
    {
        delegate* unmanaged<nint, byte*> previewPrayTell = &GlobalExports.PreviewPrayTell;

        byte* resultPointer = previewPrayTell(session);
        if (resultPointer is null)
        {
            return null;
        }

        string line = Marshal.PtrToStringUTF8((nint)resultPointer) ?? string.Empty;
        NativeExportsTestSupport.FreeBuffer(resultPointer);
        return line;
    }
}
