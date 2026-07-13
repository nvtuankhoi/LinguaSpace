import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { AuthStore } from '../../core/auth/auth.store';
import { FeedStore } from '../../core/state/feed.store';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { UserCache } from '../../core/state/user-cache.service';
import { LANGUAGES } from '../../core/util/languages';
import { relativeTime } from '../../core/util/time';
import { CommentDto, PostSummaryDto, ReactionDetailDto } from '../../core/models';
import { ReactionType } from '../../core/models/enums';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { LanguageChipComponent } from '../../shared/ui/language-chip/language-chip.component';
import { ReportDialogComponent } from '../../shared/ui/report-dialog/report-dialog.component';
import { VocabCardComponent } from './vocab-card.component';

interface ReactionOption {
  type: ReactionType;
  emoji: string;
}

@Component({
  selector: 'app-post-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AvatarComponent, LanguageChipComponent, IconComponent, ReportDialogComponent, VocabCardComponent, ReactiveFormsModule],
  templateUrl: './post-card.component.html',
  styleUrl: './post-card.component.scss',
})
export class PostCardComponent {
  protected readonly users = inject(UserCache);
  private readonly feed = inject(FeedStore);
  private readonly feedApi = inject(FeedApi);
  private readonly auth = inject(AuthStore);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly realtime = inject(PresenceRealtimeService);

  private liveWired = false;

  readonly post = input.required<PostSummaryDto>();

  /** When true (post detail page) comments are fetched and shown immediately. */
  readonly expanded = input<boolean>(false);

  protected readonly comments = signal<CommentDto[]>([]);
  /** Live offset vs. the loaded commentCount (new/deleted comments since load). */
  protected readonly commentDelta = signal(0);
  protected readonly showComments = signal(false);
  protected readonly loadingComments = signal(false);
  protected readonly commentText = signal('');
  protected readonly sending = signal(false);
  protected readonly showPicker = signal(false);
  protected readonly justReacted = signal(false);
  protected readonly showReport = signal(false);
  protected readonly replyTo = signal<number | null>(null);
  protected readonly replyText = signal('');

  // ── Who-reacted popover ─────────────────────────────────────────────
  protected readonly showReactors = signal(false);
  protected readonly reactors = signal<ReactionDetailDto[]>([]);
  protected readonly loadingReactors = signal(false);

  // ── Inline post edit ────────────────────────────────────────────────
  protected readonly editingPost = signal(false);
  protected readonly editForm = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]],
    languageCode: [''],
  });
  protected readonly languages = LANGUAGES;

  // ── Inline comment edit ─────────────────────────────────────────────
  protected readonly editingCommentId = signal<number | null>(null);
  protected readonly editCommentText = signal('');

  protected readonly isMine = computed(() => this.post().authorId === this.auth.user()?.userId);

  protected readonly reactions: ReactionOption[] = [
    { type: 'Like',  emoji: '👍' },
    { type: 'Love',  emoji: '❤️' },
    { type: 'Haha',  emoji: '😄' },
    { type: 'Wow',   emoji: '😮' },
    { type: 'Sad',   emoji: '😢' },
    { type: 'Angry', emoji: '😠' },
  ];

  protected readonly author = computed(() => this.users.user(this.post().authorId));
  protected readonly myReaction = computed(() => this.feed.myReactions()[this.post().id] ?? null);
  protected readonly time = computed(() => relativeTime(this.post().createdAt));

  constructor() {
    // Resolve (and cache) the author whenever the bound post changes.
    effect(() => this.users.ensure(this.post().authorId));

    // On a dedicated post page, expand comments immediately (once).
    effect(() => {
      if (this.expanded() && this.comments().length === 0 && !this.loadingComments()) {
        this.showComments.set(true);
        void this.loadComments();
      }
    });

    // Live comment updates from the post group (post-detail page only). Wired
    // once when expanded becomes true; events arrive because the
    // PostDetailComponent joins the post group on the shared presence connection.
    effect(() => {
      if (!this.expanded() || this.liveWired) return;
      this.liveWired = true;
      this.wireLiveComments();
    });
  }

  private wireLiveComments(): void {
    // New comment from another viewer (or our own send — deduped by id).
    this.realtime.onNewComment.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.postId !== this.post().id) return;
      if (this.comments().some((c) => c.id === ev.id)) return;
      this.users.ensure(ev.authorId);
      this.comments.update((cs) => [
        ...cs,
        {
          id: ev.id,
          postId: ev.postId,
          authorId: ev.authorId,
          content: ev.content,
          parentCommentId: ev.parentCommentId,
          likeCount: 0,
          createdAt: ev.createdAt,
        },
      ]);
      this.commentDelta.update((n) => n + 1);
    });
    this.realtime.onCommentEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.postId !== this.post().id) return;
      this.comments.update((cs) => cs.map((c) => (c.id === ev.id ? { ...c, content: ev.content } : c)));
    });
    this.realtime.onCommentDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.postId !== this.post().id) return;
      const had = this.comments().some((c) => c.id === ev.id);
      this.comments.update((cs) => cs.filter((c) => c.id !== ev.id));
      if (had) this.commentDelta.update((n) => n - 1);
    });
    // Live reaction-count change on a comment of this post.
    this.realtime.onNewReaction.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.targetType !== 'Comment') return;
      this.comments.update((cs) => cs.map((c) => (c.id === ev.targetId ? { ...c, likeCount: ev.likeCount } : c)));
    });
  }

  protected reactionEmoji(type: ReactionType | null): string {
    if (!type) return '👍';
    return this.reactions.find((r) => r.type === type)?.emoji ?? '👍';
  }

  protected toggleComments(): void {
    if (this.showComments()) {
      this.showComments.set(false);
      return;
    }
    this.showComments.set(true);
    if (!this.comments().length) {
      void this.loadComments();
    }
  }

  protected async loadComments(): Promise<void> {
    this.loadingComments.set(true);
    try {
      const res = await firstValueFrom(this.feedApi.getComments(this.post().id));
      this.comments.set(res.items);
      res.items.forEach((c) => this.users.ensure(c.authorId));
    } finally {
      this.loadingComments.set(false);
    }
  }

  protected async react(type: ReactionType): Promise<void> {
    this.showPicker.set(false);
    this.justReacted.set(true);
    setTimeout(() => this.justReacted.set(false), 400);
    await this.feed.react(this.post().id, type);
  }

  /** Toggle the "who reacted" popover for this post. Refetches on each open so
   *  the list stays fresh (a single GET; cheap). */
  protected async toggleReactors(): Promise<void> {
    if (this.showReactors()) {
      this.showReactors.set(false);
      return;
    }
    this.showReactors.set(true);
    this.loadingReactors.set(true);
    try {
      const res = await firstValueFrom(this.feedApi.getReactions(this.post().id));
      this.reactors.set(res.items);
    } catch {
      this.reactors.set([]);
    } finally {
      this.loadingReactors.set(false);
    }
  }

  protected onCommentInput(event: Event): void {
    this.commentText.set((event.target as HTMLInputElement).value);
  }

  protected async sendComment(): Promise<void> {
    const text = this.commentText().trim();
    if (!text) {
      return;
    }
    this.sending.set(true);
    try {
      await firstValueFrom(this.feedApi.addComment(this.post().id, { content: text }));
      this.commentText.set('');
      await this.loadComments();
    } finally {
      this.sending.set(false);
    }
  }

  protected isCommentMine(authorId: string): boolean {
    return authorId === this.auth.user()?.userId;
  }

  protected toggleReply(commentId: number): void {
    this.replyTo.set(this.replyTo() === commentId ? null : commentId);
    this.replyText.set('');
  }

  protected onReplyInput(event: Event): void {
    this.replyText.set((event.target as HTMLInputElement).value);
  }

  protected async sendReply(parentId: number): Promise<void> {
    const text = this.replyText().trim();
    if (!text) {
      return;
    }
    try {
      await firstValueFrom(this.feedApi.addComment(this.post().id, { content: text, parentCommentId: parentId }));
      this.replyTo.set(null);
      this.replyText.set('');
      await this.loadComments();
    } catch {
      /* ignore */
    }
  }

  protected async deletePost(): Promise<void> {
    if (!confirm('Delete this post?')) {
      return;
    }
    await this.feed.deletePost(this.post().id);
  }

  protected async deleteComment(commentId: number): Promise<void> {
    if (!confirm('Delete this comment?')) {
      return;
    }
    try {
      await firstValueFrom(this.feedApi.deleteComment(commentId));
      this.comments.set(this.comments().filter((c) => c.id !== commentId));
    } catch {
      /* ignore */
    }
  }

  // ── Post edit ──

  protected startEditPost(): void {
    this.editForm.setValue({ content: this.post().content, languageCode: this.post().languageCode ?? '' });
    this.editingPost.set(true);
  }

  protected cancelEditPost(): void {
    this.editingPost.set(false);
  }

  protected async saveEditPost(): Promise<void> {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    const v = this.editForm.getRawValue();
    await this.feed.editPost(this.post().id, {
      content: v.content.trim(),
      languageCode: v.languageCode || null,
    });
    this.editingPost.set(false);
  }

  protected onEditPostInput(event: Event): void {
    this.editForm.controls.content.setValue((event.target as HTMLTextAreaElement).value);
  }

  // ── Comment edit ──

  protected startEditComment(c: CommentDto): void {
    this.editingCommentId.set(c.id);
    this.editCommentText.set(c.content);
  }

  protected cancelEditComment(): void {
    this.editingCommentId.set(null);
    this.editCommentText.set('');
  }

  protected onEditCommentInput(event: Event): void {
    this.editCommentText.set((event.target as HTMLInputElement).value);
  }

  protected async saveEditComment(commentId: number): Promise<void> {
    const text = this.editCommentText().trim();
    if (!text) {
      return;
    }
    try {
      await firstValueFrom(this.feedApi.updateComment(commentId, { content: text }));
      this.comments.set(this.comments().map((c) => (c.id === commentId ? { ...c, content: text } : c)));
      this.editingCommentId.set(null);
      this.editCommentText.set('');
    } catch {
      /* ignore */
    }
  }
}
