import SwiftUI

/// Works around a confirmed Apple platform bug (FB20116555): `@FocusedValue`/`.focusedSceneValue` never
/// activates for `DocumentGroup`-based apps on iOS/iPadOS (it works on macOS, which this app doesn't
/// target — `TOURING_THEATRE_PLAN.md` §13.6) — `Commands` always saw `nil`, regardless of which document
/// window was key, so the menu bar and hardware-keyboard shortcuts stayed permanently disabled. Whichever
/// document/workspace scene is currently active publishes its actions here directly, tracked via
/// `\.scenePhase`; `TheatreCommands` reads straight from this singleton instead of `@FocusedValue`.
///
/// A per-publisher token guards against one scene's `onDisappear` clearing a value another scene has since
/// published (e.g. switching files within a workspace tears down and rebuilds `WorkspaceEditorHostView`) —
/// a clear only takes effect if the token still matches the most recent publish.
@Observable
@MainActor
final class TheatreActiveScene {
    static let shared = TheatreActiveScene()

    private init() {}

    private(set) var documentActions: TheatreDocumentActions?
    private(set) var folderActions: TheatreFolderActions?

    private var documentActionsToken: UUID?
    private var folderActionsToken: UUID?

    func publishDocumentActions(_ actions: TheatreDocumentActions, token: UUID) {
        documentActionsToken = token
        documentActions = actions
    }

    func clearDocumentActions(token: UUID) {
        guard documentActionsToken == token else { return }
        documentActionsToken = nil
        documentActions = nil
    }

    func publishFolderActions(_ actions: TheatreFolderActions, token: UUID) {
        folderActionsToken = token
        folderActions = actions
    }

    func clearFolderActions(token: UUID) {
        guard folderActionsToken == token else { return }
        folderActionsToken = nil
        folderActions = nil
    }
}
