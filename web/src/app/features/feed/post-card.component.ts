import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { AuthStore } from '../../core/auth/auth.store';
import { FeedStore } from '../../core/state/feed.store';
import { UserCache } from '../../core/state/user-cache.service';
import { LANGUAGES } from '../../core/util/languages';
import { relativeTime } from '../../core/util/time';
import { CommentDto, PostSummaryDto } from '../../core/models';
import { ReactionType } from '../../core/models/enums';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { LanguageChipComponent } from '../../shared/ui/language-chip/language-chip.component';
import { ReportDialogComponent } from '../../shared/ui/report-dialog/report-dialog.component';

interface ReactionOption {
  type: ReactionType;
  emoji: string;
}

@Component({
  selector: 'app-post-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, LanguageChipComponent, IconComponent, ReportDialogComponent, ReactiveFormsModule],
  templateUrl: './post-card.component.html',
  styleUrl: './post-card.component.scss',
})
export class PostCardComponent {
  protected readonly users = inject(UserCache);
  private readonly feed = inject(FeedStore);
  private readonly feedApi = inject(FeedApi);
  private readonly auth = inject(AuthStore);
  private readonly fb = inject(FormBuilder);

  readonly post = input.required<PostSummaryDto>();

  protected readonly comments = signal<CommentDto[]>([]);
  protected readonly showComments = signal(false);
  protected readonly loadingComments = signal(false);
  protected readonly commentText = signal('');
  protected readonly sending = signal(false);
  protected readonly showPicker = signal(false);
  protected readonly justReacted = signal(false);
  protected readonly showReport = signal(false);
  protected readonly replyTo = signal<number | null>(null);
  protected readonly replyText = signal('');

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
