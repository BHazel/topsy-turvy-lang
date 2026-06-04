namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines Topsy Turvy language keyword strings.
/// </summary>
/// <remarks>
/// These constants are generally used wherever a keyword string is compared, looked up, or passed programmatically,
/// but not necessarily everywhere.  In some situations string literlas may be used directly for readability.
/// </remarks>
public static class Keywords
{
    /// <summary>
    /// Boolean and null literal keywords.
    /// </summary>
    public static class Literals
    {
        /// <summary>Boolean <c>true</c> literal.</summary>
        public const string Verity = "VERITY";

        /// <summary>Boolean <c>false</c> literal.</summary>
        public const string Nay = "NAY";

        /// <summary>Null literal and null type name.</summary>
        public const string Naught = "NAUGHT";
    }

    /// <summary>
    /// Type keywords for declarations and casts.
    /// </summary>
    public static class TypeNames
    {
        /// <summary>Integer type (<c>PEER</c>).</summary>
        public const string Peer = "PEER";

        /// <summary>Floating-point type (<c>FATHOM</c>).</summary>
        public const string Fathom = "FATHOM";

        /// <summary>String type (<c>YARN</c>).</summary>
        public const string Yarn = "YARN";

        /// <summary>Boolean type (<c>DECREE</c>).</summary>
        public const string Decree = "DECREE";

        /// <summary>Null type (<c>NAUGHT</c>).</summary>
        /// <remarks>
        /// The same token as the null literal, <see cref="Literals.Naught"/>.
        /// </remarks>
        public const string Naught = Literals.Naught;
    }

    /// <summary>
    /// Special built-in keywords.
    /// </summary>
    public static class SpecialNames
    {
        /// <summary>The implicit variable.</summary>
        public const string JustSo = "JUST SO";
    }
}
