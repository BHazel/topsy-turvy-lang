namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Defines constants for the kind of output the <see cref="CilEmitter"/> produces:
/// </summary>
/// <remarks>
/// * <see cref="CilOutputKind"/><c>.Library</c>: Outputs a library with no CLR entry point.  Suitable for loading via
/// reflecton and invoking directly.
/// * <see cref="CilOutputKind"/><c>.Executable</c>: Outputs an executable with the <c>Opera.Main</c> method registered as
/// the CLR entry point.  An additional <c>&lt;name&gt;.runtimeconfig.json</c> file is written alongside the output so it can
/// be launched as a framework-dependent app.
/// * <see cref="CilOutputKind"/><c>.IlSourceOnly</c>: Emits no assembly and <see cref="CilEmitOptions.OutputPath"/> is
/// ignored.  Only the CIL text is produced and saved to <see cref="CilEmitResult.IlSource"/>.
/// </remarks>
public enum CilOutputKind
{
    /// <summary>A library assembly with no CLR entry point set.</summary>
    Library,

    /// <summary>An executable assembly with the <c>Opera.Main</c> method registered as the CLR entry point.</summary>
    Executable,

    /// <summary>No assembly is written and only the CIL text is produced.</summary>
    IlSourceOnly,
}
