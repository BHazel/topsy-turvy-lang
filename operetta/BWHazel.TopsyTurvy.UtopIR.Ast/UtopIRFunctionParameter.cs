namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// A single parameter in a <see cref="UtopIRFunctionDefinition"/> <c>duty</c> header.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to one <c>term &lt;type&gt; %&lt;param-name&gt;</c> operand in the <c>duty</c> opener.
/// </para>
/// <para>
/// For example, the following Topsy Turvy function header:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="UtopIRFunctionDefinition.Parameters"/> list of:
/// </para>
/// <code>
/// new UtopIRFunctionParameter(Type: new UtopIRTermType(UtopIRType.Peer), Name: "Num1");
/// new UtopIRFunctionParameter(Type: new UtopIRTermType(UtopIRType.Peer), Name: "Num2");
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// term peer %Num1, term peer %Num2
/// </code>
/// </remarks>
/// <param name="Type">The declared type of the parameter.</param>
/// <param name="Name">The name of the parameter, without the <c>%</c> prefix.</param>
public sealed record UtopIRFunctionParameter(UtopIRTermType Type, string Name);
