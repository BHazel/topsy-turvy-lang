import Foundation
import TopsyTurvyToolchain

/// Defines callbacks expected by the Topsy Turvy toolchain.
///
/// `@convention(c)` function pointers can only be formed from top-level functions or literal closures, never
/// from methods with captured instance state — so the two callback targets below are free functions that
/// recover the box owned by their session via `Unmanaged`, rather than instance methods on `TopsyTurvySession`
/// itself. This also allows more than one session (e.g. multiple open documents) to register distinct
/// callback targets safely, since each box is routed to independently via its own context pointer.
final class SessionCallbacks {
    /// Invoked for each output line the running programme writes; `suppressNewline` means "append to the
    /// last line" rather than "start a new line".
    var onOutputLine: ((_ text: String, _ suppressNewline: Bool) -> Void)?

    /// Resolves a `PRAY ADMIT` import filename to its source text, or `nil` if it cannot be resolved.
    var onResolveImport: ((_ filename: String) -> String?)?

    /// The most recently returned buffer from `resolve_import`.
    ///
    /// Freeing is required as the native toolchain copies the returned string but does not free it so the caller
    /// must free it.  It is safe, however, to free a buffer from a previous call as the native toolchain reads
    /// the buffer synchronously before the `resolve_import` callback returns.
    var pendingImportBuffer: UnsafeMutablePointer<CChar>?

    /// Frees `pendingImportBuffer` if one is outstanding.
    func freePendingImportBuffer() {
        if let pendingImportBuffer {
            free(pendingImportBuffer)
        }
        
        self.pendingImportBuffer = nil
    }
}

/// The callback when an output line is written by a running program.
///
/// This corresponds to the `output_line` callback target registered with `topsyturvy_tc_session_create`.
///
/// - Parameters:
///   - context: The `Unmanaged<SessionCallbacks>` opaque pointer for the owning session.
///   - utf8Line: The UTF-8 output line.
///   - suppressNewline: A value indicating whether a newline should be omitted, non-zero when the trailing newline should be omitted.
func sessionOutputLineCallback(context: UnsafeMutableRawPointer?, utf8Line: UnsafePointer<UInt8>?, suppressNewline: UInt8) {
    guard let context, let utf8Line else {
        return
    }

    let sessionCallbacks = Unmanaged<SessionCallbacks>.fromOpaque(context).takeUnretainedValue()
    let lineText = String(cString: utf8Line)
    sessionCallbacks.onOutputLine?(lineText, suppressNewline != 0)
}

/// The callback when a `PRAY ADMIT` import resolves a filename.
///
/// This corresponds to the `resolve_import` callback target registered with `topsyturvy_tc_session_create`.
///
/// - Parameters:
///   - context: The `Unmanaged<SessionCallbacks>` opaque pointer for the owning session.
///   - filename: The filename (in UTF-8 encoding) to resolve.
///
/// - Returns: A newly allocated UTF-8 buffer with the resolved import source text, or `nil` if the import
///   cannot be resolved.
func sessionResolveImportCallback(context: UnsafeMutableRawPointer?, filename: UnsafePointer<UInt8>?) -> UnsafeMutablePointer<UInt8>? {
    guard let context, let filename else {
        return nil
    }

    let sessionCallbacks = Unmanaged<SessionCallbacks>.fromOpaque(context).takeUnretainedValue()
    sessionCallbacks.freePendingImportBuffer()

    let theFilename = String(cString: filename)
    guard let resolvedImport = sessionCallbacks.onResolveImport?(theFilename), let copy = strdup(resolvedImport) else {
        return nil
    }

    sessionCallbacks.pendingImportBuffer = copy
    return UnsafeMutableRawPointer(copy).assumingMemoryBound(to: UInt8.self)
}
