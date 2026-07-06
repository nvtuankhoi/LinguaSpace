import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  OnInit,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { Room as LkRoom, RoomEvent, Track } from 'livekit-client';

import { AuthStore } from '../../core/auth/auth.store';
import { RoomStore } from '../../core/state/room.store';
import { RoomRealtimeService } from '../../core/state/room-realtime.service';
import { RoomsApi } from '../../core/api/rooms.api';
import { relativeTime } from '../../core/util/time';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { LanguageChipComponent } from '../../shared/ui/language-chip/language-chip.component';
import { MessageDto, RoomType, UpdateRoomRequest } from '../../core/models';

interface MediaTile {
  identity: string;
  displayName: string;
  /** A LiveKit video track to render, or null for an audio-only/avatar fallback tile. */
  videoTrack: Track | null;
  isLocal: boolean;
}

@Component({
  selector: 'app-room-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, AvatarComponent, IconComponent, LanguageChipComponent],
  templateUrl: './room-detail.component.html',
  styleUrl: './room-detail.component.scss',
})
export class RoomDetailComponent implements OnInit {
  private readonly store = inject(RoomStore);
  private readonly realtime = inject(RoomRealtimeService);
  private readonly auth = inject(AuthStore);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly roomsApi = inject(RoomsApi);
  private readonly router = inject(Router);

  /** Bound from the :id route param via withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly current = this.store.current;
  protected readonly messages = this.store.messages;
  protected readonly status = this.store.status;

  /** True when the current user is this room's host (drives host controls). */
  protected readonly isHost = computed(() => {
    const room = this.current();
    const me = this.auth.user();
    return !!room && !!me && room.hostId === me.userId;
  });

  protected readonly showManage = signal(false);
  protected readonly manageForm = this.fb.nonNullable.group({
    title: ['', [Validators.required]],
    description: [''],
    maxParticipants: [8, [Validators.min(2), Validators.max(50)]],
  });

  // Voice/video state
  protected readonly avConnected = signal(false);
  protected readonly avConnecting = signal(false);
  protected readonly avError = signal<string | null>(null);
  protected readonly micMuted = signal(false);
  protected readonly camOff = signal(false);
  protected readonly videoTiles = signal<MediaTile[]>([]);

  private lkRoom: LkRoom | null = null;
  private readonly thread = viewChild<ElementRef<HTMLDivElement>>('thread');
  private readonly videoEls = viewChildren<ElementRef<HTMLVideoElement>>('videoEl');

  protected readonly form = this.fb.nonNullable.group({ content: [''] });

  constructor() {
    // Append incoming realtime messages, then keep the thread scrolled to the bottom.
    this.realtime.onMessage.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.store.appendMessage(ev);
      this.scrollToBottom();
    });

    // Attach LiveKit video tracks to their <video> elements whenever the tile set changes.
    effect(() => {
      const tiles = this.videoTiles();
      const els = this.videoEls();
      for (const el of els) {
        const identity = el.nativeElement.getAttribute('data-identity');
        const tile = tiles.find((t) => t.identity === identity);
        if (tile?.videoTrack) {
          tile.videoTrack.attach(el.nativeElement);
        }
      }
    });

    this.destroyRef.onDestroy(async () => {
      await this.disconnectLiveKit();
      void this.store.closeRoom();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.store.openRoom(Number(this.id()));
    this.scrollToBottom();
  }

  protected isMine(message: MessageDto): boolean {
    return message.senderId === this.auth.user()?.userId;
  }

  protected time(iso: string): string {
    return relativeTime(iso);
  }

  protected roomTypeLabel(type: RoomType): string {
    return type === 'VoiceOnly' ? 'Voice' : type === 'VideoEnabled' ? 'Video' : 'Text';
  }

  protected roomTypeIcon(type: RoomType): string {
    return type === 'VoiceOnly' ? 'mic' : type === 'VideoEnabled' ? 'camera' : 'messages';
  }

  // ---- LiveKit voice/video ----

  protected async toggleMic(): Promise<void> {
    const muted = !this.micMuted();
    this.micMuted.set(muted);
    try {
      await this.lkRoom?.localParticipant.setMicrophoneEnabled(!muted);
    } catch (e) {
      console.error('[livekit] mic toggle failed', e);
    }
  }

  protected async toggleCam(): Promise<void> {
    const off = !this.camOff();
    this.camOff.set(off);
    try {
      await this.lkRoom?.localParticipant.setCameraEnabled(!off);
    } catch (e) {
      console.error('[livekit] cam toggle failed', e);
    }
  }

  protected async joinAv(): Promise<void> {
    const room = this.current();
    if (!room || this.avConnecting() || this.avConnected()) {
      return;
    }
    this.avConnecting.set(true);
    this.avError.set(null);
    try {
      const { token, liveKitUrl } = await firstValueFrom(this.roomsApi.mediaToken(room.id));
      const lk = new LkRoom({ adaptiveStream: true, dynacast: true });
      await lk.connect(liveKitUrl, token);

      // Publish local tracks up front (mic always; camera only for video rooms).
      await lk.localParticipant.setMicrophoneEnabled(!this.micMuted());
      if (room.roomType === 'VideoEnabled') {
        await lk.localParticipant.setCameraEnabled(!this.camOff());
      }

      lk.on(RoomEvent.ParticipantConnected, () => this.syncTiles());
      lk.on(RoomEvent.ParticipantDisconnected, () => this.syncTiles());
      lk.on(RoomEvent.TrackSubscribed, () => this.syncTiles());
      lk.on(RoomEvent.TrackUnsubscribed, () => this.syncTiles());
      lk.on(RoomEvent.LocalTrackPublished, () => this.syncTiles());
      lk.on(RoomEvent.LocalTrackUnpublished, () => this.syncTiles());

      this.lkRoom = lk;
      this.syncTiles();
      this.avConnected.set(true);
    } catch (err) {
      console.error('[livekit] connect failed', err);
      this.avError.set('Could not connect to the room media server. You can still use the text chat.');
      this.avConnected.set(false);
      await this.disconnectLiveKit();
    } finally {
      this.avConnecting.set(false);
    }
  }

  protected async leaveAv(): Promise<void> {
    await this.disconnectLiveKit();
    this.avConnected.set(false);
    this.micMuted.set(false);
    this.camOff.set(false);
  }

  private async disconnectLiveKit(): Promise<void> {
    const lk = this.lkRoom;
    this.lkRoom = null;
    if (lk) {
      lk.removeAllListeners();
      try {
        await lk.disconnect();
      } catch (e) {
        console.error('[livekit] disconnect failed', e);
      }
    }
    this.videoTiles.set([]);
  }

  /** Rebuild the video tile set from the current LiveKit participants. */
  private syncTiles(): void {
    const lk = this.lkRoom;
    if (!lk) {
      this.videoTiles.set([]);
      return;
    }
    const me = this.auth.user();
    const tiles: MediaTile[] = [];

    const localCam = lk.localParticipant.getTrackPublication(Track.Source.Camera)?.track ?? null;
    tiles.push({
      identity: lk.localParticipant.identity,
      displayName: me?.displayName ?? 'You',
      videoTrack: localCam,
      isLocal: true,
    });

    for (const p of lk.remoteParticipants.values()) {
      const cam = p.getTrackPublication(Track.Source.Camera)?.track ?? null;
      tiles.push({
        identity: p.identity,
        displayName: p.name ?? p.identity,
        videoTrack: cam,
        isLocal: false,
      });
    }

    this.videoTiles.set(tiles);
  }

  protected async send(): Promise<void> {
    const content = this.form.getRawValue().content.trim();
    if (!content) {
      return;
    }
    this.form.reset();
    await this.store.send(content);
    this.scrollToBottom();
  }

  /** Deletes a room message. Backend allows the message owner OR the room host. */
  protected async deleteMsg(msg: MessageDto): Promise<void> {
    if (!confirm('Delete this message?')) {
      return;
    }
    await this.store.deleteMessage(msg.id);
  }

  // ---- Host controls ----

  protected openManage(): void {
    const room = this.current();
    if (!room) {
      return;
    }
    this.manageForm.setValue({
      title: room.title,
      description: room.description ?? '',
      maxParticipants: room.maxParticipants,
    });
    this.showManage.set(true);
  }

  protected async submitManage(): Promise<void> {
    if (this.manageForm.invalid) {
      this.manageForm.markAllAsTouched();
      return;
    }
    const v = this.manageForm.getRawValue();
    const req: UpdateRoomRequest = {
      title: v.title.trim(),
      description: v.description.trim() || null,
      maxParticipants: v.maxParticipants,
    };
    await this.store.updateRoom(req);
    this.showManage.set(false);
  }

  protected async makeHost(userId: string): Promise<void> {
    if (!confirm('Transfer host to this participant? You will no longer be the host.')) {
      return;
    }
    await this.store.transferHost(userId);
    this.showManage.set(false);
  }

  protected async kickUser(userId: string): Promise<void> {
    if (!confirm('Remove this participant from the room?')) {
      return;
    }
    await this.store.kickParticipant(userId);
  }

  protected async endRoom(): Promise<void> {
    if (!confirm('End this room for everyone? This cannot be undone.')) {
      return;
    }
    await this.store.deleteRoom();
    this.showManage.set(false);
    await this.router.navigate(['/app/rooms']);
  }

  protected onInput(event: Event): void {
    this.form.controls.content.setValue((event.target as HTMLInputElement).value);
  }

  private scrollToBottom(): void {
    queueMicrotask(() => {
      const el = this.thread()?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }
}
