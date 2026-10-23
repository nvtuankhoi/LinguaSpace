import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  input,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthStore } from '../../core/auth/auth.store';
import { MessageStore } from '../../core/state/message.store';
import { DmRealtimeService } from '../../core/state/dm-realtime.service';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { relativeTime } from '../../core/util/time';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { DirectMessageDto } from '../../core/models';

/** A message group: consecutive messages from the same sender */
export interface MessageGroup {
  senderId: string;
  isMine: boolean;
  messages: DirectMessageDto[];
}

@Component({
  selector: 'app-message-thread',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, AvatarComponent, IconComponent],
  templateUrl: './message-thread.component.html',
  styleUrl: './message-thread.component.scss',
})
export class MessageThreadComponent implements OnInit, AfterViewChecked {
  private readonly store = inject(MessageStore);
  private readonly realtime = inject(DmRealtimeService);
  private readonly presence = inject(PresenceRealtimeService);
  private readonly auth = inject(AuthStore);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly id = input.required<string>();

  protected readonly active = this.store.active;
  protected readonly messages = this.store.messages;
  protected readonly status = this.store.status;

  /** Whether the scroll container is scrolled far enough from the bottom to show the button */
  protected readonly showScrollBtn = signal(false);
  protected readonly hasNewMessages = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly editText = signal('');
  private wasAtBottom = true;
  private prevMessageCount = 0;

  private readonly thread = viewChild<ElementRef<HTMLDivElement>>('thread');

  protected readonly form = this.fb.nonNullable.group({ content: [''] });

  /** Group consecutive messages from the same sender */
  protected readonly groups = computed<MessageGroup[]>(() => {
    const msgs = this.messages();
    const myId = this.auth.user()?.userId;
    const groups: MessageGroup[] = [];
    for (const msg of msgs) {
      const last = groups.at(-1);
      if (last && last.senderId === msg.senderId) {
        last.messages.push(msg);
      } else {
        groups.push({ senderId: msg.senderId, isMine: msg.senderId === myId, messages: [msg] });
      }
    }
    return groups;
  });

  constructor() {
    // Own outgoing messages (sender echo + mock replies): append + autoscroll.
    this.realtime.onMessage.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((dm) => {
      this.store.appendMessage(dm);
      this.afterLiveMessage();
    });

    // Live incoming DMs pushed by PresenceHub's "NewDirectMessage". The store
    // update (append + auto mark-read for this thread, unread-badge bump for
    // other conversations) is routed globally by the shell; here we only keep
    // the open thread scrolled to the newly arrived message.
    this.presence.onDirectMessage.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((dm) => {
      if (dm.conversationId !== Number(this.id())) {
        return;
      }
      this.afterLiveMessage();
    });

    // Live DM edits/deletes from the other participant — only relevant to this thread.
    this.presence.onDmEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.conversationId !== Number(this.id())) {
        return;
      }
      this.store.applyEdited(ev.id, ev.content, ev.editedAt);
      this.cdr.markForCheck();
    });
    this.presence.onDmDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.conversationId !== Number(this.id())) {
        return;
      }
      this.store.applyDeleted(ev.id);
      this.cdr.markForCheck();
    });

    this.destroyRef.onDestroy(() => void this.store.closeConversation());
  }

  /** Append-side effects shared by own-send and incoming DMs: autoscroll or flag. */
  private afterLiveMessage(): void {
    if (this.isNearBottom()) {
      this.scrollToBottom();
    } else {
      this.hasNewMessages.set(true);
      this.showScrollBtn.set(true);
    }
    this.cdr.markForCheck();
  }

  async ngOnInit(): Promise<void> {
    await this.store.openConversation(Number(this.id()));
    this.scrollToBottom();
    this.prevMessageCount = this.messages().length;
  }

  ngAfterViewChecked(): void {
    const count = this.messages().length;
    if (count !== this.prevMessageCount && this.wasAtBottom) {
      this.scrollToBottom();
      this.prevMessageCount = count;
    }
  }

  protected isMine(msg: DirectMessageDto): boolean {
    return msg.senderId === this.auth.user()?.userId;
  }

  protected time(iso: string): string {
    return relativeTime(iso);
  }

  protected onThreadScroll(): void {
    const isNear = this.isNearBottom();
    this.wasAtBottom = isNear;
    this.showScrollBtn.set(!isNear);
    if (isNear) {
      this.hasNewMessages.set(false);
    }
  }

  protected scrollDown(): void {
    this.scrollToBottom();
    this.hasNewMessages.set(false);
    this.showScrollBtn.set(false);
  }

  protected async send(): Promise<void> {
    const content = this.form.getRawValue().content.trim();
    if (!content) return;
    this.form.reset();
    await this.store.send(content);
    this.scrollToBottom();
  }

  protected startEdit(m: DirectMessageDto): void {
    this.editingId.set(m.id);
    this.editText.set(m.content);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
    this.editText.set('');
  }

  protected onEditInput(event: Event): void {
    this.editText.set((event.target as HTMLTextAreaElement).value);
  }

  protected async saveEdit(): Promise<void> {
    const id = this.editingId();
    if (id == null) {
      return;
    }
    const text = this.editText().trim();
    if (!text) {
      return;
    }
    await this.store.editMessage(id, text);
    this.editingId.set(null);
    this.editText.set('');
    this.scrollToBottom();
  }

  protected async deleteMsg(m: DirectMessageDto): Promise<void> {
    if (!confirm('Delete this message?')) {
      return;
    }
    await this.store.deleteMessage(m.id);
  }

  // ---- Conversation search + clear-my-messages ----
  protected readonly searchResults = this.store.searchResults;
  protected readonly searchStatus = this.store.searchStatus;
  protected readonly showSearch = signal(false);
  protected readonly searchForm = this.fb.nonNullable.group({ term: [''] });

  protected toggleSearch(): void {
    const next = !this.showSearch();
    this.showSearch.set(next);
    if (!next) {
      this.store.clearSearch();
      this.searchForm.reset();
    }
  }

  protected async runSearch(): Promise<void> {
    const term = this.searchForm.getRawValue().term.trim();
    await this.store.searchConversation(term);
  }

  protected async clearMine(): Promise<void> {
    if (!confirm('Delete every message you sent in this conversation? This cannot be undone.')) {
      return;
    }
    await this.store.clearMyMessages();
  }

  protected onInput(event: Event): void {
    this.form.controls.content.setValue((event.target as HTMLInputElement).value);
  }

  private isNearBottom(): boolean {
    const el = this.thread()?.nativeElement;
    if (!el) return true;
    return el.scrollHeight - el.scrollTop - el.clientHeight < 80;
  }

  private scrollToBottom(): void {
    queueMicrotask(() => {
      const el = this.thread()?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
        this.wasAtBottom = true;
      }
    });
  }
}
