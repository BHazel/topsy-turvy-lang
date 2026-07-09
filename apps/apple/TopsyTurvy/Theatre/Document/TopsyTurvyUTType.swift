import UniformTypeIdentifiers

/// Extends `UTType` to add a unique type identifier (UTI) for Topsy Turvy `.topsy` files.
extension UTType {
    /// The exported UTI for Topsy Turvy `.topsy` source files.
    ///
    /// As this is not a system-registered type it must also be registered in `Info.plist` in the
    /// `UTExportedTypeDeclarations` array, conforming to `public.plain-text`.
    static var topsySource: UTType {
        UTType(exportedAs: "uk.bwhazel.topsy")
    }
}
