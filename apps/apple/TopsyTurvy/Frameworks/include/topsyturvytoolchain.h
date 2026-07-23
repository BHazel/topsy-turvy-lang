/**
 * @brief Topsy Turvy Toolchain Native AOT Exports v2.
 *
 * Matches the C# export surface of operetta/BWHazel.TopsyTurvy.Embedded/NativeExports/ToolchainExports.cs
 * (`topsyturvy_tc_*`) and operetta/BWHazel.TopsyTurvy.Embedded/NativeExports/StandardLibrary/GlobalExports.cs
 * (`topsyturvy_std_*`).
 *
 * @remark JSON diagnostic spans (`topsyturvy_tc_analyse` `DiagnosticInfo`) and token spans (`topsyturvy_tc_tokens`
 * `TokenInfo`) use 1-indexed, half-open line/column pairs, matching the toolchain `SourceSpan` and
 * `SourceLocation` convention: both describe a span of source text, not a cursor position.
 *
 * @remark JSON hover and completion results (`topsyturvy_tc_hover` `HoverResult` and `topsyturvy_tc_complete` `CompletionResult`)
 * use 0-indexed line/column pairs, matching the LSP convention already used elsewhere in the toolchain: both
 * describe a cursor position, not a span.
 */

#ifndef TOPSYTURVYTOOLCHAIN_H
#define TOPSYTURVYTOOLCHAIN_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Opaque handle to a native session created by `topsyturvy_tc_session_create`.
 */
typedef void *topsyturvy_session;

/**
 * @brief Invoked once per output line written by a running programme; `suppress_newline` is non-zero when the
 * trailing newline should be omitted, i.e. the message continues the previous output line.
 * @param context The context pointer passed to `topsyturvy_tc_session_create`.
 * @param utf8String The null-terminated, UTF-8 encoded output line.
 * @param suppress_newline A value indicating whether the trailing newline should be omitted (non-zero) or not (zero).
 */
typedef void (*topsyturvy_output_line_fn)(void *context, const uint8_t *utf8String, uint8_t suppress_newline);

/**
 * @brief Invoked to resolve a `PRAY ADMIT` import filename to its source text.  Returns a caller-owned,
 * null-terminated, UTF-8 encoded buffer, or `NULL` if the import cannot be resolved.  The toolchain copies
 * the returned string immediately and never frees or retains the pointer.  Ownership stays with the
 * caller throughout.
 * @param context The context pointer passed to `topsyturvy_tc_session_create`.
 * @param filename_utf8 The null-terminated, UTF-8 encoded import filename.
 * @return uint8_t* A caller-owned, null-terminated, UTF-8 encoded buffer containing the import source text, or `NULL` if the import cannot be resolved.
 */
typedef uint8_t *(*topsyturvy_resolve_import_fn)(void *context, const uint8_t *filename_utf8);

/**
 * @brief Invoked to read one line of input on demand, e.g. for `PRAY TELL` once the pre-supplied `stdin_utf8`
 * queue passed to `topsyturvy_tc_execute` has been drained.  Returns a caller-owned, null-terminated, UTF-8
 * encoded buffer, or `NULL` for no input available.  The toolchain copies the returned string immediately
 * and never frees or retains the pointer.  Ownership stays with the caller throughout.  May be `NULL` if
 * the caller does not want to register one, in which case a drained queue simply yields an empty string.
 * @param context The context pointer passed to `topsyturvy_tc_session_create`.
 * @return uint8_t* A caller-owned, null-terminated, UTF-8 encoded buffer containing the input line, or `NULL` if no input is available.
 */
typedef uint8_t *(*topsyturvy_input_line_fn)(void *context);

/**
 * @brief Returns the native export contract version.
 * @return The native export contract version as an integer.
 */
int32_t topsyturvy_tc_api_version(void);

/**
 * @brief Creates a new session, capturing the given callbacks and context pointer, all of which are
 * registered for the lifetime of the session.  Returns an opaque session handle, or `NULL` if an
 * exception was thrown.
 * @param output_line The callback function to be invoked for each output line written by a running programme.
 * @param resolve_import The callback function to be invoked to resolve a `PRAY ADMIT` import filename to its source text.
 * @param input_line The callback function to be invoked to read one line of input on demand, or `NULL` to leave input unregistered.
 * @param context A context pointer that will be passed to the callback functions. This pointer can be used to maintain state or pass additional information to the callbacks.
 * @return topsyturvy_session An opaque session handle, or `NULL` if an exception was thrown.
 */
topsyturvy_session topsyturvy_tc_session_create(topsyturvy_output_line_fn output_line, topsyturvy_resolve_import_fn resolve_import, topsyturvy_input_line_fn input_line, void *context);

/**
 * @brief Destroys a session created by `topsyturvy_tc_session_create`.  An invalid or already-destroyed handle is
 * silently ignored rather than crashing the host process.
 * @param session The session handle to be destroyed.
 */
void topsyturvy_tc_session_destroy(topsyturvy_session session);

/**
 * @brief Parses and type-checks the given null-terminated, UTF-8 encoded Topsy Turvy source, returning every
 * diagnostic produced as a JSON `AnalysisResult` that must be released via `topsyturvy_tc_free`.  Returns `NULL`
 * if the session handle is invalid or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @return uint8_t* A JSON `AnalysisResult` that must be released via `topsyturvy_tc_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_tc_analyse(topsyturvy_session session, const uint8_t *source_utf8);

/**
 * @brief Builds Markdown hover content for the symbol at the given 0-indexed line/column, returning a JSON
 * `HoverResult` that must be released via `topsyturvy_tc_free`.  Returns `NULL` if the session handle is invalid
 * or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param line The 0-indexed line number.
 * @param column The 0-indexed column number.
 * @return uint8_t* A JSON `HoverResult` that must be released via `topsyturvy_tc_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_tc_hover(topsyturvy_session session, const uint8_t *source_utf8, int32_t line, int32_t column);

/**
 * @brief Builds keyword and symbol completion candidates for the given 0-indexed line/column, returning a JSON
 * `CompletionResult` that must be released via `topsyturvy_tc_free`.  Keywords are always offered, even
 * when the source fails to parse.  Returns `NULL` if the session handle is invalid or an exception was
 * thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param line The 0-indexed line number.
 * @param column The 0-indexed column number.
 * @return uint8_t* A JSON `CompletionResult` that must be released via `topsyturvy_tc_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_tc_complete(topsyturvy_session session, const uint8_t *source_utf8, int32_t line, int32_t column);

/**
 * @brief Formats the given null-terminated, UTF-8 encoded Topsy Turvy source with canonical keyword casing
 * and libretto indentation, returning the formatted source text (not JSON) that must be released via
 * `topsyturvy_tc_free`.  Returns `NULL` if the session handle is invalid or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @return uint8_t* The formatted source text that must be released via `topsyturvy_tc_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_tc_format(topsyturvy_session session, const uint8_t *source_utf8);

/**
 * @brief Scans the given null-terminated, UTF-8 encoded Topsy Turvy source into categorised token spans for
 * editor syntax highlighting, returning a JSON `TokenResult` that must be released via `topsyturvy_tc_free`.
 * This is a lexical scan, not a parse: it succeeds even when the source does not currently form valid
 * syntax.  Returns `NULL` if the session handle is invalid or an exception was thrown.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @return uint8_t* A JSON `TokenResult` that must be released via `topsyturvy_tc_free`, or `NULL` if an error occurred.
 */
uint8_t *topsyturvy_tc_tokens(topsyturvy_session session, const uint8_t *source_utf8);

/**
 * @brief Parses, type-checks and interprets the given null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param session The session handle.
 * @param source_utf8 The null-terminated, UTF-8 encoded Topsy Turvy source.
 * @param args_json_utf8 An optional JSON string array exposed as `THE PROPS`, or `NULL` for none.
 * @param stdin_utf8 An optional newline-delimited buffer pre-seeding `PRAY TELL` input, or `NULL` for none.
 * @return int32_t Returns 0 on success, 1 if parsing failed, 2 if type-checking failed, 3 if an exception was thrown (including at runtime, or if the session handle is invalid) or 4 if execution was cancelled via topsyturvy_tc_cancel.
 */
int32_t topsyturvy_tc_execute(topsyturvy_session session, const uint8_t *source_utf8, const uint8_t *args_json_utf8, const uint8_t *stdin_utf8);

/**
 * @brief Cooperatively cancels the session currently running execution. A no-op if the handle is invalid
 * or no execution has started yet.
 * @param session The session handle.
 */
void topsyturvy_tc_cancel(topsyturvy_session session);

/**
 * @brief Prints text through the session output callback via the Standard Library `PreviewBehold` function,
 * without a running programme.  Routes through the same code path as a `SUMMON PreviewBehold` reached
 * mid-programme via `topsyturvy_tc_execute`.
 * @param session The session handle.
 * @param text_utf8 The null-terminated, UTF-8 encoded text to print.
 * @param with_ceremony Non-zero to apply a trailing newline; zero to suppress it.
 * @return int32_t Returns 0 on success, or 1 if the session handle is invalid or the call failed.
 */
int32_t topsyturvy_std_preview_behold(topsyturvy_session session, const uint8_t *text_utf8, uint8_t with_ceremony);

/**
 * @brief Reads a line through the session input callback via the Standard Library `PreviewPrayTell` function,
 * without a running programme.  Routes through the same code path as a `SUMMON PreviewPrayTell` reached
 * mid-programme via `topsyturvy_tc_execute`.
 * @param session The session handle.
 * @return uint8_t* A null-terminated, UTF-8 encoded buffer that must be released via `topsyturvy_tc_free`, or `NULL` if the session handle is invalid or the call failed.
 */
uint8_t *topsyturvy_std_preview_pray_tell(topsyturvy_session session);

/**
 * @brief Returns the message of the most recently caught exception for the given session as a UTF-8 buffer
 * that must be released via `topsyturvy_tc_free`. Returns `NULL` if the handle is invalid or no error has
 * occurred.
 * @param session The session handle.
 * @return uint8_t* The message of the most recently caught exception, or `NULL` if no error has occurred.
 */
uint8_t *topsyturvy_tc_last_error(topsyturvy_session session);

/**
 * @brief Releases a buffer previously returned.
 * @param pointer The buffer to release.
 */
void topsyturvy_tc_free(uint8_t *pointer);

#ifdef __cplusplus
}
#endif

#endif
