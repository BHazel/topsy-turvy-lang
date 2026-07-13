import SwiftUI

/// Holds the actions published by whichever document or folder view is currently active for the menu
/// bar and hardware-keyboard shortcuts to read.
///
/// This is required as `TheatreCommands` cannot use the built-in SwiftUI `@FocusedValue`/`.focusedSceneValue`
/// as they do not propagate values in `DocumentGroup`-based apps which is only supported on macOS at present.
/// Single-file or folder editors post actions here to be read by `TheatreCommands`.
///
/// A view publishes on appear and whenever its `\.scenePhase` becomes `.active` then clears its entry on
/// disappear.  Each publisher passes its own `UUID` token with every call.  A clear only takes effect if that
/// token still matches the most recently published one so one view teardown cannot clear a value a
/// different view has since published.  This matters because, for example, switching files within a
/// folder tears down and rebuilds the previous file `EditorHostView`.
@Observable
@MainActor
final class TheatreActiveScene {
    /// The shared instance of `TheatreActiveScene`.
    static let shared = TheatreActiveScene()

    /// Creates a `TheatreActiveScene`.
    private init() {}

    /// The active document actions.
    ///
    /// Read directly by `TheatreCommands` (menu bar).
    private(set) var documentActions: TheatreDocumentActions?
    
    /// The active document view folder opening actions, or `nil` if none are published.
    ///
    /// The folder browser view does not publish this as it does not offer the ability to open a folder.
    private(set) var folderActions: TheatreFolderActions?

    /// The token most recently passed to publish document actions.
    ///
    /// Used to validate a subsequent calls to clear.
    private var documentActionsToken: UUID?

    /// The token most recently passed to publish folder actions.
    ///
    /// Used to validate a subsequent calls to clear.
    private var folderActionsToken: UUID?

    /// Publishes the provided actions as the active view document actions.
    ///
    /// - Parameters:
    ///   - actions: The document actions to publish.
    ///   - token: The publishing view own token, recorded so a later clear call from the same view can be validated.
    func publishDocumentActions(_ actions: TheatreDocumentActions, token: UUID) {
        self.documentActionsToken = token
        self.documentActions = actions
    }

    /// Clears document actions but only if the token still matches the token of the most recent
    /// call to publish document actions.
    ///
    /// If different, a different view has since published, and this call is a no-op.
    ///
    /// - Parameters:
    ///   - token: The token the caller published with.
    func clearDocumentActions(token: UUID) {
        guard self.documentActionsToken == token else {
            return
        }
        
        self.documentActionsToken = nil
        self.documentActions = nil
    }

    /// Publishes the provided actions as the active view folder actions.
    ///
    /// - Parameters:
    ///   - actions: The folder actions to publish.
    ///   - token: The publishing view own token, recorded so a later clear call from the same window can be validated.
    func publishFolderActions(_ actions: TheatreFolderActions, token: UUID) {
        self.folderActionsToken = token
        self.folderActions = actions
    }

    /// Clears folder actions but only if the token still matches the token of the most recent
    /// call to publish folder actions.
    ///
    /// If different, a different view has since published, and this call is a no-op.
    ///
    /// - Parameters:
    ///   - token: The token the caller published with.
    func clearFolderActions(token: UUID) {
        guard self.folderActionsToken == token else {
            return
        }
        
        self.folderActionsToken = nil
        self.folderActions = nil
    }
}
