import Combine
import SwiftUI
import UIKit

/// Tracks whether the software keyboard is currently visible, via `UIResponder`'s show/hide notifications.
///
/// `CodeEditorView` wraps its own `UITextView`, not a stock SwiftUI `TextField`/`TextEditor`, so there's no
/// `@FocusState` binding available to detect editing directly — observing the keyboard notifications works
/// regardless of which view currently holds first responder. For the same reason, dismissing the keyboard
/// can't target a specific SwiftUI focus binding either; `dismiss()` uses the standard "resign whichever
/// responder is currently first" trick instead.
@MainActor
final class KeyboardObserver: ObservableObject {
    @Published private(set) var isKeyboardVisible = false

    private var cancellables = Set<AnyCancellable>()

    init() {
        NotificationCenter.default.publisher(for: UIResponder.keyboardWillShowNotification)
            .sink { [weak self] _ in self?.isKeyboardVisible = true }
            .store(in: &cancellables)

        NotificationCenter.default.publisher(for: UIResponder.keyboardWillHideNotification)
            .sink { [weak self] _ in self?.isKeyboardVisible = false }
            .store(in: &cancellables)
    }

    /// Resigns whichever view currently holds first responder (the editor's internal `UITextView`), which
    /// dismisses the keyboard without needing a direct reference to that view.
    func dismiss() {
        UIApplication.shared.sendAction(#selector(UIResponder.resignFirstResponder), to: nil, from: nil, for: nil)
    }
}
