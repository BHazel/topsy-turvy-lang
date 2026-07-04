import Foundation
import TopsyTurvyToolchain

/// Holds the callback closures for one native session, recovered from the opaque `context` pointer passed to
/// `topsyturvy_session_create`.
///
/// `@convention(c)` function pointers can only be formed from top-level functions or literal closures, never
/// from methods with captured instance state — so the two callback targets below are free functions that
/// recover their owning session's box via `Unmanaged`, rather than instance methods on `TopsyTurvySession`
/// itself. This also allows more than one session (e.g. multiple open documents) to register distinct
/// callback targets safely, since each box is routed to independently via its own context pointer.
final class SessionCallbackBox {
    /// Invoked for each output line the running programme writes; `suppressNewline` means "append to the
    /// last line" rather than "start a new line".
    var onOutputLine: ((_ text: String, _ suppressNewline: Bool) -> Void)?

    /// Resolves a `PRAY ADMIT` import filename to its source text, or `nil` if it cannot be resolved.
    var onResolveImport: ((_ filename: String) -> String?)?

    /// The most recently returned `resolve_import` buffer.
    ///
    /// The header documents that the native side copies the returned string immediately and never frees or
    /// retains the pointer — ownership stays with the caller (Swift) throughout. Since the native side reads
    /// the buffer synchronously before this callback returns, it is safe to free the *previous* call's buffer
    /// at the start of the *next* invocation (or at session teardown); freeing within the same invocation that
    /// returns it would be a use-after-free from the native side's perspective.
    var pendingImportBuffer: UnsafeMutablePointer<CChar>?

    /// Frees `pendingImportBuffer` if one is outstanding.
    func freePendingImportBuffer() {
        if let pendingImportBuffer {
            free(pendingImportBuffer)
        }
        pendingImportBuffer = nil
    }
}

/// The `output_line` callback target registered with `topsyturvy_session_create`.
/// - Parameter context: The `Unmanaged<SessionCallbackBox>` opaque pointer for the owning session.
/// - Parameter utf8: The UTF-8 output line.
/// - Parameter suppressNewline: Non-zero when the trailing newline should be omitted.
func sessionOutputLineCallback(context: UnsafeMutableRawPointer?, utf8: UnsafePointer<UInt8>?, suppressNewline: UInt8) {
    guard let context, let utf8 else { return }

    let box = Unmanaged<SessionCallbackBox>.fromOpaque(context).takeUnretainedValue()
    let text = String(cString: utf8)
    box.onOutputLine?(text, suppressNewline != 0)
}

/// The `resolve_import` callback target registered with `topsyturvy_session_create`.
/// - Parameter context: The `Unmanaged<SessionCallbackBox>` opaque pointer for the owning session.
/// - Parameter filename: The UTF-8 filename to resolve.
/// - Returns: A caller-owned, `strdup`-allocated UTF-8 buffer, or `nil` if the import cannot be resolved.
func sessionResolveImportCallback(context: UnsafeMutableRawPointer?, filename: UnsafePointer<UInt8>?) -> UnsafeMutablePointer<UInt8>? {
    guard let context, let filename else { return nil }

    let box = Unmanaged<SessionCallbackBox>.fromOpaque(context).takeUnretainedValue()
    box.freePendingImportBuffer()

    let name = String(cString: filename)
    guard let resolved = box.onResolveImport?(name), let copy = strdup(resolved) else { return nil }

    box.pendingImportBuffer = copy
    return UnsafeMutableRawPointer(copy).assumingMemoryBound(to: UInt8.self)
}
