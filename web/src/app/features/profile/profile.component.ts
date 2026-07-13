import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ProfileStore } from '../../core/state/profile.store';
import { GamificationStore } from '../../core/state/gamification.store';
import { relativeTime } from '../../core/util/time';
import { languageName } from '../../core/util/language';
import { LANGUAGES } from '../../core/util/languages';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { UserLanguageDto, XpDailyDto } from '../../core/models';
import { LanguageLevel, LanguageType, XpHistoryPeriod } from '../../core/models/enums';

/** XP needed per level */
const XP_PER_LEVEL = 500;

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, AvatarComponent, IconComponent, DecimalPipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent {
  protected readonly store = inject(ProfileStore);
  private readonly gamification = inject(GamificationStore);
  private readonly fb = inject(FormBuilder);

  protected readonly profile = this.store.profile;
  protected readonly status = this.store.status;
  protected readonly editing = this.store.editing;
  protected readonly saving = this.store.saving;
  protected readonly xp = this.gamification.myXp;
  protected readonly badges = this.gamification.myBadges;
  protected readonly xpHistory = this.gamification.xpHistory;
  protected readonly xpHistoryPeriod = this.gamification.xpHistoryPeriod;
  protected readonly progressStatus = this.gamification.progressStatus;

  protected readonly xpPerLevel = XP_PER_LEVEL;

  protected readonly level = computed(() => {
    const total = this.xp()?.totalXp ?? 0;
    return Math.floor(total / XP_PER_LEVEL) + 1;
  });

  protected readonly xpInLevel = computed(() => {
    const total = this.xp()?.totalXp ?? 0;
    return total % XP_PER_LEVEL;
  });

  protected readonly xpProgress = computed(() => (this.xpInLevel() / XP_PER_LEVEL) * 100);

  /** Days of XP activity, newest day + newest transaction first (backend returns ascending). */
  protected readonly activityDays = computed<XpDailyDto[]>(() =>
    this.xpHistory()
      .map((d) => ({ ...d, transactions: [...d.transactions].reverse() }))
      .reverse(),
  );

  protected readonly xpHistoryPeriods: XpHistoryPeriod[] = ['week', 'month'];

  protected readonly nativeLanguages = computed<UserLanguageDto[]>(
    () => this.profile()?.languages.filter((l) => l.type === 'Native') ?? [],
  );
  protected readonly learningLanguages = computed<UserLanguageDto[]>(
    () => this.profile()?.languages.filter((l) => l.type === 'Learning') ?? [],
  );

  protected readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(64)]],
    bio: ['', [Validators.maxLength(280)]],
    avatarUrl: [''],
  });

  /** Tracks whether the edit form was hydrated from the loaded profile. */
  private readonly seeded = signal(false);

  // ── Language management ──
  protected readonly languages = LANGUAGES;
  protected readonly levels: LanguageLevel[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'];
  protected readonly langForm = this.fb.nonNullable.group({
    languageCode: ['', Validators.required],
    type: ['Learning' as LanguageType, Validators.required],
    level: ['A1' as LanguageLevel],
  });

  constructor() {
    void this.store.load();
    void this.gamification.loadProgress();
  }

  protected startEdit(): void {
    const p = this.profile();
    if (!p) return;
    this.form.setValue({ displayName: p.displayName, bio: p.bio ?? '', avatarUrl: p.avatarUrl ?? '' });
    this.seeded.set(true);
    this.store.beginEdit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    await this.store.save({
      displayName: value.displayName.trim(),
      bio: value.bio.trim() ? value.bio.trim() : null,
      avatarUrl: value.avatarUrl.trim() ? value.avatarUrl.trim() : null,
    });
  }

  protected langName(code: string): string {
    return languageName(code);
  }

  protected time(iso: string | null): string {
    return iso ? relativeTime(iso) : '';
  }

  protected async addLanguage(): Promise<void> {
    if (this.langForm.invalid) {
      this.langForm.markAllAsTouched();
      return;
    }
    const v = this.langForm.getRawValue();
    await this.store.addLanguage({
      languageCode: v.languageCode,
      type: v.type,
      level: v.type === 'Native' ? null : v.level,
    });
    this.langForm.reset({ languageCode: '', type: v.type, level: 'A1' });
  }

  protected onLangLevelChange(l: UserLanguageDto, event: Event): void {
    const level = (event.target as HTMLSelectElement).value as LanguageLevel;
    void this.store.updateLanguage(l.id, level);
  }

  protected async removeLang(l: UserLanguageDto): Promise<void> {
    await this.store.removeLanguage(l.id);
  }

  // ── XP activity ──

  protected setXpPeriod(period: XpHistoryPeriod): void {
    if (period === this.xpHistoryPeriod()) {
      return;
    }
    void this.gamification.loadXpHistory(period);
  }

  /** Friendly label for a `YYYY-MM-DD` day bucket: Today / Yesterday / weekday + date. */
  protected dayLabel(date: string): string {
    const d = new Date(`${date}T00:00:00`);
    const now = new Date();
    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const diffDays = Math.round((startOfToday.getTime() - d.getTime()) / 86_400_000);
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    return d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  }
}
