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

        /// <summary>Null literal, assignable to a pointer, array or <c>yarn</c> variable.</summary>
        public const string Naught = "naught";
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

        /// <summary>The <c>sum.f</c> instruction.</summary>
        public const string SumFloat = "sum.f";

        /// <summary>The <c>diff.f</c> instruction.</summary>
        public const string DiffFloat = "diff.f";

        /// <summary>The <c>prod.f</c> instruction.</summary>
        public const string ProdFloat = "prod.f";

        /// <summary>The <c>quot.f</c> instruction.</summary>
        public const string QuotFloat = "quot.f";

        /// <summary>The <c>rem.f</c> instruction.</summary>
        public const string RemFloat = "rem.f";

        /// <summary>The <c>max.f</c> instruction.</summary>
        public const string MaxFloat = "max.f";

        /// <summary>The <c>min.f</c> instruction.</summary>
        public const string MinFloat = "min.f";

        /// <summary>The <c>chord</c> instruction.</summary>
        public const string Chord = "chord";

        /// <summary>The <c>harmony</c> instruction.</summary>
        public const string Harmony = "harmony";

        /// <summary>The <c>discord</c> instruction.</summary>
        public const string Discord = "discord";

        /// <summary>The <c>inv</c> instruction.</summary>
        public const string Inv = "inv";

        /// <summary>The <c>transup</c> instruction.</summary>
        public const string TransUp = "transup";

        /// <summary>The <c>transdown</c> instruction.</summary>
        public const string TransDown = "transdown";

        /// <summary>The <c>alike</c> instruction.</summary>
        public const string Alike = "alike";

        /// <summary>The <c>unlike</c> instruction.</summary>
        public const string Unlike = "unlike";

        /// <summary>The <c>preadam</c> instruction.</summary>
        public const string PreAdam = "preadam";

        /// <summary>The <c>lowerdeg</c> instruction.</summary>
        public const string LowerDeg = "lowerdeg";

        /// <summary>The <c>alike.f</c> instruction.</summary>
        public const string AlikeFloat = "alike.f";

        /// <summary>The <c>unlike.f</c> instruction.</summary>
        public const string UnlikeFloat = "unlike.f";

        /// <summary>The <c>preadam.f</c> instruction.</summary>
        public const string PreAdamFloat = "preadam.f";

        /// <summary>The <c>lowerdeg.f</c> instruction.</summary>
        public const string LowerDegFloat = "lowerdeg.f";

        /// <summary>The <c>both</c> instruction.</summary>
        public const string Both = "both";

        /// <summary>The <c>either</c> instruction.</summary>
        public const string Either = "either";

        /// <summary>The <c>hardly</c> instruction.</summary>
        public const string Hardly = "hardly";

        /// <summary>The <c>sail</c> instruction.</summary>
        public const string Sail = "sail";

        /// <summary>The <c>sailalike</c> instruction.</summary>
        public const string SailAlike = "sailalike";

        /// <summary>The <c>sailunlike</c> instruction.</summary>
        public const string SailUnlike = "sailunlike";

        /// <summary>The <c>victim.yarn</c> instruction.</summary>
        public const string VictimYarn = "victim.yarn";

        /// <summary>The <c>welcome.list</c> instruction.</summary>
        public const string WelcomeList = "welcome.list";

        /// <summary>The <c>appoint.victim</c> instruction.</summary>
        public const string AppointVictim = "appoint.victim";

        /// <summary>The <c>victim.list</c> instruction.</summary>
        public const string VictimList = "victim.list";

        /// <summary>The <c>welcome.gallerypic</c> instruction.</summary>
        public const string WelcomeGallerypic = "welcome.gallerypic";

        /// <summary>The <c>pictureto</c> instruction.</summary>
        public const string PictureTo = "pictureto";

        /// <summary>The <c>viewfrom</c> instruction.</summary>
        public const string ViewFrom = "viewfrom";

        /// <summary>The <c>viewto</c> instruction.</summary>
        public const string ViewTo = "viewto";

        /// <summary>The <c>sum.g</c> instruction.</summary>
        public const string SumPointer = "sum.g";

        /// <summary>The <c>diff.g</c> instruction.</summary>
        public const string DiffPointer = "diff.g";
    }
}
