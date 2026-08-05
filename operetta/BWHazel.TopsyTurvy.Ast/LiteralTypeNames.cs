namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Converts <see cref="LiteralType"/> values to their Topsy Turvy display names.
/// </summary>
public static class LiteralTypeNames
{
    /// <summary>
    /// Converts a <see cref="LiteralType"/> to a user-friendly display name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The user-friendly display name.</returns>
    public static string ToDisplayName(LiteralType type) => type switch
    {
        LiteralType.Integer => Keywords.TypeNames.Peer,
        LiteralType.Long => Keywords.TypeNames.Chancellor,
        LiteralType.Short => Keywords.TypeNames.Pirate,
        LiteralType.SignedByte => Keywords.TypeNames.SausageRoll,
        LiteralType.UnsignedInteger => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Peer}",
        LiteralType.UnsignedLong => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Chancellor}",
        LiteralType.UnsignedShort => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Pirate}",
        LiteralType.Byte => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.SausageRoll}",
        LiteralType.Double => Keywords.TypeNames.Fathom,
        LiteralType.Single => Keywords.TypeNames.Foot,
        LiteralType.String => Keywords.TypeNames.Yarn,
        LiteralType.Char => Keywords.TypeNames.Stitch,
        LiteralType.Boolean => Keywords.TypeNames.Decree,
        LiteralType.Null => Keywords.TypeNames.Naught,
        LiteralType.Array => Keywords.TypeNames.LittleListOf,
        LiteralType.Pointer => Keywords.TypeNames.GalleryPictureOf,
        _ => "unknown"
    };

    /// <summary>
    /// Converts a <see cref="LiteralType"/> to a user-friendly display name, including the element type when the type is an array.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="elementType">The array element type, when <paramref name="type"/> is <see cref="LiteralType.Array"/>.</param>
    /// <returns>The user-friendly display name.</returns>
    public static string ToDisplayName(LiteralType type, LiteralType? elementType) =>
        type == LiteralType.Array && elementType is not null
            ? $"{ToDisplayName(type)} {ToDisplayName(elementType.Value)}"
            : ToDisplayName(type);
}
