/**
 * @brief Topsy Turvy Standard Library `Global` namespace native AOT exports (`topsyturvy_std_*`).
 *
 * Matches the C# export surface of
 * operetta/BWHazel.TopsyTurvy.Embedded/NativeExports/StandardLibrary/GlobalExports.cs.
 */

#ifndef TOPSYTURVY_STD_GLOBAL_H
#define TOPSYTURVY_STD_GLOBAL_H

#include "../toolchain.h"

#ifdef __cplusplus
extern "C" {
#endif

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

#ifdef __cplusplus
}
#endif

#endif
