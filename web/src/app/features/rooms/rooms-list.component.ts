import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { UpperCasePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { RoomStore } from '../../core/state/room.store';
import { RoomType } from '../../core/models';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { LanguageChipComponent } from '../../shared/ui/language-chip/language-chip.component';

type TypeFilter = 'all' | RoomType;

interface TypeFilterOption {
  value: TypeFilter;
  label: string;
}

@Component({
  selector: 'app-rooms-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, UpperCasePipe, LanguageChipComponent, IconComponent],
  templateUrl: './rooms-list.component.html',
  styleUrl: './rooms-list.component.scss',
})
export class RoomsListComponent {
  private readonly store = inject(RoomStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly rooms = this.store.rooms;
  protected readonly status = this.store.status;
  protected readonly showCreate = signal(false);
  protected readonly typeFilter = signal<TypeFilter>('all');
  protected readonly source = signal<'discover' | 'mine'>('discover');

  protected readonly typeFilters: TypeFilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'TextOnly', label: 'Text' },
    { value: 'VoiceOnly', label: 'Voice' },
    { value: 'VideoEnabled', label: 'Video' },
  ];

  protected readonly filteredRooms = computed(() => {
    const filter = this.typeFilter();
    const rooms = this.rooms();
    return filter === 'all' ? rooms : rooms.filter((r) => r.roomType === filter);
  });

  protected readonly languages = ['en', 'es', 'ja', 'de', 'fr', 'it', 'ko', 'zh', 'pt', 'ru'];

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required]],
    languageCode: ['en', [Validators.required]],
    roomType: ['TextOnly' as RoomType],
    maxParticipants: [8, [Validators.min(2), Validators.max(50)]],
  });

  constructor() {
    void this.store.loadRooms();
  }

  protected setTypeFilter(filter: TypeFilter): void {
    this.typeFilter.set(filter);
  }

  /** Switch between the public directory (Discover) and rooms the user has
   *  joined (My rooms). Resets the type refinement and re-loads the list. */
  protected setSource(s: 'discover' | 'mine'): void {
    if (s === this.source()) {
      return;
    }
    this.source.set(s);
    this.typeFilter.set('all');
    void (s === 'mine' ? this.store.loadMyRooms() : this.store.loadRooms());
  }

  protected open(id: number): void {
    void this.router.navigate(['/app/rooms', id]);
  }

  protected async create(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const id = await this.store.createRoom({
      title: value.title.trim(),
      languageCode: value.languageCode,
      roomType: value.roomType,
      maxParticipants: value.maxParticipants,
    });
    this.showCreate.set(false);
    this.open(id);
  }

  protected typeLabel(type: RoomType): string {
    return type === 'VoiceOnly' ? 'Voice' : type === 'VideoEnabled' ? 'Video' : 'Text';
  }

  protected typeIcon(type: RoomType): string {
    return type === 'VoiceOnly' ? 'mic' : type === 'VideoEnabled' ? 'camera' : 'messages';
  }
}
