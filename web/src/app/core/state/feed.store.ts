import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../api/feed.api';
import {
  CreatePostRequest,
  CursorPagedResult,
  PostSummaryDto,
  ReactionType,
  UpdatePostRequest,
} from '../models';
import { AuthStore } from '../auth/auth.store';

export type FeedTab = 'following' | 'explore';
type Status = 'idle' | 'loading' | 'error';

interface FeedState {
  items: PostSummaryDto[];
  status: Status;
  tab: FeedTab;
  hasMore: boolean;
  nextCursor: string | null;
  /** Tracks the current user's reaction per post (the DTO has no such field). */
  myReactions: Record<number, ReactionType>;
}

const initialState: FeedState = {
  items: [],
  status: 'idle',
  tab: 'following',
  hasMore: false,
  nextCursor: null,
  myReactions: {},
};

export const FeedStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, feedApi = inject(FeedApi), auth = inject(AuthStore)) => {
    const load = async (cursor: string | null): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const tab = store.tab();
        const res: CursorPagedResult<PostSummaryDto> =
          tab === 'following'
            ? await firstValueFrom(feedApi.getFeed(cursor))
            : await firstValueFrom(feedApi.getExplore({ beforeCursor: cursor }));
        patchState(store, {
          items: cursor ? [...store.items(), ...res.items] : res.items,
          hasMore: res.hasMore,
          nextCursor: res.nextCursor,
          status: 'idle',
        });
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    const bumpCount = (postId: number, delta: number): PostSummaryDto[] =>
      store
        .items()
        .map((p) => (p.id === postId ? { ...p, likeCount: Math.max(0, p.likeCount + delta) } : p));

    return {
      async loadFirst(): Promise<void> {
        await load(null);
      },

      async loadMore(): Promise<void> {
        if (store.hasMore() && store.nextCursor()) {
          await load(store.nextCursor()!);
        }
      },

      async setTab(tab: FeedTab): Promise<void> {
        if (store.tab() === tab && store.items().length) {
          return;
        }
        patchState(store, { tab, items: [], nextCursor: null, hasMore: false });
        await load(null);
      },

      async createPost(req: CreatePostRequest): Promise<void> {
        try {
          const { postId } = await firstValueFrom(feedApi.createPost(req));
          const summary: PostSummaryDto = {
            id: postId,
            authorId: auth.user()?.userId ?? 'me',
            content: req.content,
            postType: req.postType,
            languageCode: req.languageCode ?? null,
            metadata: req.metadata ?? null,
            likeCount: 0,
            commentCount: 0,
            createdAt: new Date().toISOString(),
            tags: req.tags ?? [],
          };
          patchState(store, { items: [summary, ...store.items()] });
        } catch {
          /* surfaced elsewhere */
        }
      },

      async deletePost(postId: number): Promise<void> {
        try {
          await firstValueFrom(feedApi.deletePost(postId));
          patchState(store, { items: store.items().filter((p) => p.id !== postId) });
        } catch {
          /* surfaced elsewhere */
        }
      },

      async editPost(postId: number, req: UpdatePostRequest): Promise<void> {
        try {
          await firstValueFrom(feedApi.updatePost(postId, req));
          patchState(store, {
            items: store.items().map((p) =>
              p.id === postId
                ? { ...p, content: req.content, languageCode: req.languageCode ?? p.languageCode }
                : p,
            ),
          });
        } catch {
          /* surfaced elsewhere */
        }
      },

      /** Live: a followed user published a post. Fetch it and prepend to the
       *  "following" feed so new posts appear without a manual reload. */
      async prependNewPost(postId: number): Promise<void> {
        if (store.tab() !== 'following') {
          return;
        }
        if (store.items().some((p) => p.id === postId)) {
          return; // already present (e.g. own post prepended optimistically on create)
        }
        try {
          const post = await firstValueFrom(feedApi.getPost(postId));
          const summary: PostSummaryDto = {
            id: post.id,
            authorId: post.authorId,
            content: post.content,
            postType: post.postType,
            languageCode: post.languageCode,
            metadata: post.metadata,
            likeCount: post.likeCount,
            commentCount: post.commentCount,
            createdAt: post.createdAt,
            tags: post.tags,
          };
          patchState(store, { items: [summary, ...store.items()] });
        } catch {
          /* ignore — post may not be visible to this user */
        }
      },

      async react(postId: number, type: ReactionType): Promise<void> {
        const my = store.myReactions();
        const current = my[postId];
        try {
          if (current === type) {
            await firstValueFrom(feedApi.removeReaction(postId, type));
            const next = { ...my };
            delete next[postId];
            patchState(store, { myReactions: next, items: bumpCount(postId, -1) });
          } else {
            await firstValueFrom(feedApi.react(postId, type));
            patchState(store, { myReactions: { ...my, [postId]: type }, items: bumpCount(postId, current ? 0 : 1) });
          }
        } catch {
          /* ignore optimistic failure */
        }
      },
    };
  }),
);
