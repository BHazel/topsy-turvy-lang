namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// A single UtopIR parse diagnostic.
/// </summary>
/// <remarks>
/// Deliberately minimal and independent of the Topsy Turvy diagnostic types to maintain a clean
/// separation.  The <seealso cref="Line"/> and <see cref="Column"/> properties are index 1-based.
/// </remarks>
/// <param name="Message">A human-readable description of the parse failure.</param>
/// <param name="Line">The 1-based line number the failure occurred at.</param>
/// <param name="Column">The 1-based column number the failure occurred at.</param>
public sealed record UtopIRDiagnostic(string Message, int Line, int Column);
