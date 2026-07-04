import UniformTypeIdentifiers

extension UTType {
    /// The exported UTI for `.topsy` source files, conforming to `public.plain-text`.
    ///
    /// Declared in `Info.plist` under `UTExportedTypeDeclarations`/`CFBundleDocumentTypes`, since `.topsy` is
    /// not a system-registered type.
    static var topsySource: UTType {
        UTType(exportedAs: "com.bwhazel.operetta.topsy")
    }
}
