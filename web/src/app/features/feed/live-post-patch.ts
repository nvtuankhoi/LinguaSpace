import { PostSummaryDto } from '../../core/models';

/**
 * Pure helpers that apply a single live feed event to a post list.
 *
 * Used by surfaces that hold their post list in a component-local signal
 * (search results, public profile) — the main feed uses the equivalent
 * methods on {@link FeedStore}, which owns its own `items`.
 *
 * Each helper returns a NEW array (immutably), so a signal fed by it
 * re-evaluates. Unknown ids are ignored.
 */

/** PostEdited: update a post's content (and language, when provided). */
export function patchPostEdit(
  items: PostSummaryDto[],
  id: number,
  content: string,
  languageCode: string | null,
): PostSummaryDto[] {
  return items.map((p) =>
    p.id === id ? { ...p, content, languageCode: languageCode ?? p.languageCode } : p,
  );
}

/** PostDeleted: drop the post from the list. */
export function patchPostDelete(items: PostSummaryDto[], id: number): PostSummaryDto[] {
  return items.filter((p) => p.id !== id);
}

/** NewReaction(Post): set the post's absolute like count. */
export function patchPostReaction(
  items: PostSummaryDto[],
  id: number,
  likeCount: number,
): PostSummaryDto[] {
  return items.map((p) => (p.id === id ? { ...p, likeCount } : p));
}

/** NewComment (+1) / CommentDeleted (-1): adjust a post's comment count. */
export function patchCommentDelta(
  items: PostSummaryDto[],
  postId: number,
  delta: number,
): PostSummaryDto[] {
  return items.map((p) =>
    p.id === postId ? { ...p, commentCount: Math.max(0, p.commentCount + delta) } : p,
  );
}
