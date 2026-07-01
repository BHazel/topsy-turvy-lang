namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines UtopIR keyword and mnemonic string constants.
/// </summary>
/// <remarks>
/// These constants ensure consistent string representation across all UtopIR toolchain
/// components.
/// </remarks>
public static class UtopIRKeywords
{
    /// <summary>
    /// Boolean literal keywords for the <c>decree</c> type.
    /// </summary>
    public static class Literals
    {
        /// <summary>Boolean <c>true</c> literal.</summary>
        public const string Verity = "verity";

        /// <summary>Boolean <c>false</c> literal.</summary>
        public const string Nay = "nay";
    }

    /// <summary>
    /// UtopIR type keyword strings.
    /// </summary>
    public static class TypeNames
    {
        /// <summary>64-bit signed integer.</summary>
        public const string Chancellor = "chancellor";

        /// <summary>32-bit signed integer.</summary>
        public const string Peer = "peer";

        /// <summary>16-bit signed integer.</summary>
        public const string Pirate = "pirate";

        /// <summary>8-bit signed integer.</summary>
        public const string SausageRoll = "sausageroll";

        /// <summary>64-bit unsigned integer.</summary>
        public const string StandingChancellor = "standingchancellor";

        /// <summary>32-bit unsigned integer.</summary>
        public const string StandingPeer = "standingpeer";

        /// <summary>16-bit unsigned integer.</summary>
        public const string StandingPirate = "standingpirate";

        /// <summary>8-bit unsigned integer.</summary>
        public const string StandingSausageRoll = "standingsausageroll";

        /// <summary>64-bit floating-point.</summary>
        public const string Fathom = "fathom";

        /// <summary>32-bit floating-point.</summary>
        public const string Foot = "foot";

        /// <summary>Boolean type.</summary>
        public const string Decree = "decree";

        /// <summary>Character type.</summary>
        public const string Stitch = "stitch";

        /// <summary>String type.</summary>
        public const string Yarn = "yarn";
    }

    /// <summary>
    /// UtopIR instruction mnemonic strings.
    /// </summary>
    public static class Instructions
    {
        /// <summary>The <c>welcome</c> instruction.</summary>
        public const string Welcome = "welcome";

        /// <summary>The <c>appoint</c> instruction.</summary>
        public const string Appoint = "appoint";

        /// <summary>The <c>prentice</c> instruction.</summary>
        public const string Prentice = "prentice";

        /// <summary>The <c>leave</c> instruction.</summary>
        public const string Leave = "leave";

        /// <summary>The <c>were</c> instruction.</summary>
        public const string Were = "were";

        /// <summary>The <c>find</c> instruction.</summary>
        public const string Find = "find";

        /// <summary>The <c>sum</c> instruction.</summary>
        public const string Sum = "sum";

        /// <summary>The <c>diff</c> instruction.</summary>
        public const string Diff = "diff";

        /// <summary>The <c>prod</c> instruction.</summary>
        public const string Prod = "prod";

        /// <summary>The <c>quot</c> instruction.</summary>
        public const string Quot = "quot";

        /// <summary>The <c>rem</c> instruction.</summary>
        public const string Rem = "rem";

        /// <summary>The <c>max</c> instruction.</summary>
        public const string Max = "max";

        /// <summary>The <c>min</c> instruction.</summary>
        public const string Min = "min";
    }
}
