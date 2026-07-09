import Combine
import SwiftUI
import UIKit

/// Tracks whether the software keyboard is currently visible, via `UIResponder` show/hide notifications.
///
/// `CodeEditorView` wraps its own `UITextView`, so there is no SwiftUI `@FocusState` binding available to
/// detect editing directly; observing the keyboard notifications works regardless of which view holds first
/// responder.
@MainActor
final class KeyboardObserver: ObservableObject {
    /// A value indicating whether the keyboard is visible, updating the view on modification.
    @Published private(set) var isKeyboardVisible = false

    /// The subscriptions to the keyboard show/hide notifications.
    private var cancellables = Set<AnyCancellable>()

    /// On initialisation configures responders for keyboard show and hide notifications.
    init() {
        NotificationCenter.default.publisher(for: UIResponder.keyboardWillShowNotification)
            .sink {
                [weak self] _ in self?.isKeyboardVisible = true
            }
            .store(in: &cancellables)

        NotificationCenter.default.publisher(for: UIResponder.keyboardWillHideNotification)
            .sink {
                [weak self] _ in self?.isKeyboardVisible = false
            }
            .store(in: &cancellables)
    }

    /// Resigns whichever view currently holds first responder, in this case the internal `UITextView` of the editor,
    /// which dismisses the keyboard without needing a direct reference to that view.
    func dismiss() {
        UIApplication.shared.sendAction(#selector(UIResponder.resignFirstResponder), to: nil, from: nil, for: nil)
    }
}
