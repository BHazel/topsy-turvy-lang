/**
 * @brief Topsy Turvy Toolchain Native AOT Exports v1.
 *
 * Matches the C# export surface of operetta/BWHazel.TopsyTurvy.Embedded/NativeExports.cs.
 *
 * @remark JSON diagnostic spans (`topsyturvy_analyse` `DiagnosticInfo`) use 1-indexed, half-open line/column
 * pairs, matching the toolchain `SourceSpan` and `SourceLocation` convention.
 *
 * @remark JSON hover and completion results (`topsyturvy_hover` `HoverResult` and `topsyturvy_complete` `CompletionResult`)
 * use 0-indexed line/column pairs, matching the LSP convention already used elsewhere in the toolchain.
 */

#ifndef TOPSYTURVYTOOLCHAIN_H
#define TOPSYTURVYTOOLCHAIN_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Opaque handle to a native session created by `topsyturvy_session_create`.
 */
typedef void *topsyturvy_session;

/**
 * @brief Invoked once per output line written by a running programme; `suppress_newline` is non-zero when the
 * trailing newline should be omitted, i.e. the message continues the previous output line.
 * @param context The context pointer passed to `topsyturvy_session_create`.
 * @param utf8String The null-terminated, UTF-8 encoded output line.
 * @param suppress_newline A value indicating whether the trailing newline should be omitted (non-zero) or not (zero).
 */
typedef void (*topsyturvy_output_line_fn)(void *context, const uint8_t *utf8String, uint8_t suppress_newline);

/**
 * @brief Invoked to resolve a `PRAY ADMIT` import filename to its source text.  Returns a caller-owned,
 * null-terminated, UTF-8 encoded buffer, or `NULL` if the import cannot be resolved.  The toolchain copies
 * the returned string immediately and never frees or retains the pointer.  Ownership stays with the
 * caller throughout.
 * @param context The context pointer passed to `topsyturvy_session_create`.
 * @param filename_utf8 The null-terminated, UTF-8 encoded import filename.
 * @return uint8_t* A caller-owned, null-terminated, UTF-8 encoded buffer containing the import source text, or `NULL` if the import cannot be resolved.
 */
typedef uint8_t *(*topsyturvy_resolve_import_fn)(void *context, const uint8_t *filename_utf8);

/**
 * @brief Returns the native export contract version.
 * @return The native export contract version as an integer.
 */
int32_t topsyturvy_api_version(void);

/**
 * @brief Creates a new session, capturing the given callbacks and context pointer, both of which are
 * registered for the lifetime of the session.  Returns an opaque session handle, or `NULL` if an
 * exception was thrown.
 * @param output_line The callback function to be invoked for each output line written by a running programme.
 * @param resolve_import The callback function to be invoked to resolve a `PRAY ADMIT` import filename to its source text.
 * @param context A context pointer that will be passed to the callback functions. This pointer can be used to maintain state or pass additional information to the callbacks.
 * @return topsyturvy_session An opaque session handle, or `NULL` if an exception was thrown.
 */
topsyturvy_session topsyturvy_session_create(topsyturvy_output_line_fn output_line, topsyturvy_resolve_import_fn resolve_import, void *context);

/**
 * @brief Destroys a session created by `topsyturvy_session_create`.  An invalid or already-destroyed handle is
 * silently ignored rather than crashing the host process.
 * @param session The session handle to be destroyed.
 */
void topsyturvy_session_destroy(topsyturvy_session session);

/**
 * @brief Parses and type-checks the given null-terminated, UTF-8 encoded Topsy Turvy source, returning every
 * diagnostic produced as a JSON `AnalysisResult` that must be released via `topsyturvy_free`.  Returns `NULL`
 * if the session handle is invalid or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @return uint8_t* A JSON `AnalysisResult` that must be released via `topsyturvy_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_analyse(topsyturvy_session session, const uint8_t *source_utf8);

/**
 * @brief Builds Markdown hover content for the symbol at the given 0-indexed line/column, returning a JSON
 * `HoverResult` that must be released via `topsyturvy_free`.  Returns `NULL` if the session handle is invalid
 * or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param line The 0-indexed line number.
 * @param column The 0-indexed column number.
 * @return uint8_t* A JSON `HoverResult` that must be released via `topsyturvy_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_hover(topsyturvy_session session, const uint8_t *source_utf8, int32_t line, int32_t column);

/**
 * @brief Builds keyword and symbol completion candidates for the given 0-indexed line/column, returning a JSON
 * `CompletionResult` that must be released via `topsyturvy_free`.  Keywords are always offered, even
 * when the source fails to parse.  Returns `NULL` if the session handle is invalid or an exception was
 * thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param line The 0-indexed line number.
 * @param column The 0-indexed column number.
 * @return uint8_t* A JSON `CompletionResult` that must be released via `topsyturvy_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_complete(topsyturvy_session session, const uint8_t *source_utf8, int32_t line, int32_t column);

/**
 * @brief Formats the given null-terminated, UTF-8 encoded Topsy Turvy source with canonical keyword casing
 * and libretto indentation, returning the formatted source text (not JSON) that must be released via
 * `topsyturvy_free`.  Returns `NULL` if the session handle is invalid or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @return uint8_t* The formatted source text that must be released via `topsyturvy_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_format(topsyturvy_session session, const uint8_t *source_utf8);

/**
 * @brief Parses, type-checks and interprets the given null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param args_json_utf8 An optional JSON string array exposed as `THE PROPS`, or `NULL` for none.
 * @param stdin_utf8 An optional newline-delimited buffer pre-seeding `PRAY TELL` input, or `NULL` for none.
 * @return int32_t Returns 0 on success, 1 if parsing failed, 2 if type-checking failed, 3 if an exception was thrown (including at runtime, or if the session handle is invalid) or 4 if execution was cancelled via topsyturvy_cancel.
 */
int32_t topsyturvy_execute(topsyturvy_session session, const uint8_t *source_utf8, const uint8_t *args_json_utf8, const uint8_t *stdin_utf8);

/**
 * @brief Cooperatively cancels the session currently running execution. A no-op if the handle is invalid
 * or no execution has started yet.
 * @param session The session handle.
 */
void topsyturvy_cancel(topsyturvy_session session);

/**
 * @brief Returns the message of the most recently caught exception for the given session as a UTF-8 buffer
 * that must be released via `topsyturvy_free`. Returns `NULL` if the handle is invalid or no error has
 * occurred.
 * @param session The session handle.
 * @return uint8_t* The message of the most recently caught exception, or `NULL` if no error has occurred.
 */
uint8_t *topsyturvy_last_error(topsyturvy_session session);

/**
 * @brief Releases a buffer previously returned.
 * @param pointer The buffer to release.
 */
void topsyturvy_free(uint8_t *pointer);

#ifdef __cplusplus
}
#endif

#endif
