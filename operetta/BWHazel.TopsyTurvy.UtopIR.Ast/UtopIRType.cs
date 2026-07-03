namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines the supported variable types in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value maps directly to a Topsy Turvy type and a corresponding UtopIR type keyword.
/// The supported types in UtopIR are:
/// * <see cref="UtopIRType"/>.<c>Chancellor</c>: <c>chancellor</c> (64-bit signed integer)
/// * <see cref="UtopIRType"/>.<c>StandingChancellor</c>: <c>standingchancellor</c> (64-bit unsigned integer)
/// * <see cref="UtopIRType"/>.<c>Peer</c>: <c>peer</c> (32-bit signed integer)
/// * <see cref="UtopIRType"/>.<c>StandingPeer</c>: <c>standingpeer</c> (32-bit unsigned integer)
/// * <see cref="UtopIRType"/>.<c>Pirate</c>: <c>pirate</c> (16-bit signed integer)
/// * <see cref="UtopIRType"/>.<c>StandingPirate</c>: <c>standingpirate</c> (16-bit unsigned integer)
/// * <see cref="UtopIRType"/>.<c>SausageRoll</c>: <c>sausageroll</c> (8-bit signed integer)
/// * <see cref="UtopIRType"/>.<c>StandingSausageRoll</c>: <c>standingsausageroll</c> (8-bit unsigned integer)
/// * <see cref="UtopIRType"/>.<c>Fathom</c>: <c>fathom</c> (64-bit floating-point number)
/// * <see cref="UtopIRType"/>.<c>Foot</c>: <c>foot</c> (32-bit floating-point number)
/// * <see cref="UtopIRType"/>.<c>Decree</c>: <c>decree</c> (boolean value)
/// * <see cref="UtopIRType"/>.<c>Stitch</c>: <c>stitch</c> (single character)
/// * <see cref="UtopIRType"/>.<c>Yarn</c>: <c>yarn</c> (string of characters)
/// </para>
/// </remarks>
public enum UtopIRType
{
    /// <summary>A 64-bit signed integer (Topsy Turvy <c>CHANCELLOR</c>, UtopIR <c>chancellor</c>).</summary>
    Chancellor,

    /// <summary>A 32-bit signed integer (Topsy Turvy <c>PEER</c>, UtopIR <c>peer</c>).</summary>
    Peer,

    /// <summary>A 16-bit signed integer (Topsy Turvy <c>PIRATE</c>, UtopIR <c>pirate</c>).</summary>
    Pirate,

    /// <summary>An 8-bit signed integer (Topsy Turvy <c>SAUSAGE-ROLL</c>, UtopIR <c>sausageroll</c>).</summary>
    SausageRoll,

    /// <summary>A 64-bit unsigned integer (Topsy Turvy <c>STANDING CHANCELLOR</c>, UtopIR <c>standingchancellor</c>).</summary>
    StandingChancellor,

    /// <summary>A 32-bit unsigned integer (Topsy Turvy <c>STANDING PEER</c>, UtopIR <c>standingpeer</c>).</summary>
    StandingPeer,

    /// <summary>A 16-bit unsigned integer (Topsy Turvy <c>STANDING PIRATE</c>, UtopIR <c>standingpirate</c>).</summary>
    StandingPirate,

    /// <summary>An 8-bit unsigned integer (Topsy Turvy <c>STANDING SAUSAGE-ROLL</c>, UtopIR <c>standingsausageroll</c>).</summary>
    StandingSausageRoll,

    /// <summary>A 64-bit floating-point number (Topsy Turvy <c>FATHOM</c>, UtopIR <c>fathom</c>).</summary>
    Fathom,

    /// <summary>A 32-bit floating-point number (Topsy Turvy <c>FOOT</c>, UtopIR <c>foot</c>).</summary>
    Foot,

    /// <summary>A boolean value (Topsy Turvy <c>DECREE</c>, UtopIR <c>decree</c>).</summary>
    Decree,

    /// <summary>A single character (Topsy Turvy <c>STITCH</c>, UtopIR <c>stitch</c>).</summary>
    Stitch,

    /// <summary>A string of characters (Topsy Turvy <c>YARN</c>, UtopIR <c>yarn</c>).</summary>
    Yarn,
}
