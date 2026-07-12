using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Maps between <see cref="LiteralType"/> values and their Topsy Turvy keyword equivalents for use in the visual editor.
/// </summary>
public static class VisualTypeMaps
{
    /// <summary>
    /// Maps each <see cref="LiteralType"/> to the keyword displayed in the visual editor type selectors.
    /// </summary>
    public static readonly IReadOnlyDictionary<LiteralType, string> TypeToKeyword = new Dictionary<LiteralType, string>
    {
        [LiteralType.Integer]         = Keywords.TypeNames.Peer,
        [LiteralType.Long]            = Keywords.TypeNames.Chancellor,
        [LiteralType.Short]           = Keywords.TypeNames.Pirate,
        [LiteralType.SignedByte]      = Keywords.TypeNames.SausageRoll,
        [LiteralType.UnsignedInteger] = $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Peer}",
        [LiteralType.UnsignedLong]    = $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Chancellor}",
        [LiteralType.UnsignedShort]   = $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Pirate}",
        [LiteralType.Byte]            = $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.SausageRoll}",
        [LiteralType.Double]          = Keywords.TypeNames.Fathom,
        [LiteralType.Single]          = Keywords.TypeNames.Foot,
        [LiteralType.String]          = Keywords.TypeNames.Yarn,
        [LiteralType.Char]            = Keywords.TypeNames.Stitch,
        [LiteralType.Boolean]         = Keywords.TypeNames.Decree,
        [LiteralType.Array]           = Keywords.TypeNames.LittleListOf,
        [LiteralType.Null]            = Keywords.TypeNames.Naught,
    };
}
