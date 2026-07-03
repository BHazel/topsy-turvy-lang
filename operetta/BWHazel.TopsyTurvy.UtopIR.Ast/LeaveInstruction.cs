namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Pops the top value from the current stack frame and assigns it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>leave</c> instruction with the format <c>£&lt;name&gt; = leave</c>.
/// Like <see cref="PrenticeInstruction"/>, there is no direct equivalent in Topsy Turvy.
/// </para>
/// <para>
/// The following example pops a value from the stack into the virtual register <c>£LovesickMaidens</c>:
/// </para>
/// <code>
/// new LeaveInstruction(Target: new UtopIRVariable("LovesickMaidens"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £LovesickMaidens = leave
/// </code>
/// </remarks>
/// <param name="Target">The virtual register to receive the value popped from the stack.</param>
public sealed record LeaveInstruction(UtopIRVariable Target) : UtopIRInstruction;
