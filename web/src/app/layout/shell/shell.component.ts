import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthStore } from '../../core/auth/auth.store';
import { NotificationStore } from '../../core/state/notification.store';
import { MessageStore } from '../../core/state/message.store';
import { GamificationStore } from '../../core/state/gamification.store';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { ThemeService } from '../../core/theme.service';
import { IconComponent } from '../../shared/ui/icon/icon.component';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

/** XP required per level — simple linear: each level = 500 XP */
const XP_PER_LEVEL = 500;

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent, DecimalPipe],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private readonly themeService = inject(ThemeService);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly notificationStore = inject(NotificationStore);
  private readonly messageStore = inject(MessageStore);
  private readonly gamificationStore = inject(GamificationStore);
  private readonly presence = inject(PresenceRealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly theme = this.themeService.theme;
  protected readonly user = this.auth.user;
  protected readonly initial = computed(() => this.user()?.displayName?.charAt(0)?.toUpperCase() ?? '·');
  protected readonly isAdmin = computed(() => (this.user()?.roles ?? []).includes('Administrator'));
  protected readonly unreadNotifications = this.notificationStore.unreadCount;
  protected readonly unreadMessages = this.messageStore.unreadTotal;
  protected readonly xpSummary = this.gamificationStore.myXp;
  protected readonly showUserMenu = signal(false);

  protected readonly level = computed(() => {
    const xp = this.xpSummary()?.totalXp ?? 0;
    return Math.floor(xp / XP_PER_LEVEL) + 1;
  });

  protected readonly xpProgress = computed(() => {
    const xp = this.xpSummary()?.totalXp ?? 0;
    return ((xp % XP_PER_LEVEL) / XP_PER_LEVEL) * 100;
  });

  protected readonly nav: readonly NavItem[] = [
    { path: '/app/feed', label: 'Feed', icon: 'home' },
    { path: '/app/rooms', label: 'Rooms', icon: 'rooms' },
    { path: '/app/messages', label: 'Messages', icon: 'messages' },
    { path: '/app/notifications', label: 'Notifications', icon: 'notifications' },
    { path: '/app/friends', label: 'Network', icon: 'profile' },
    { path: '/app/leaderboard', label: 'Leaderboard', icon: 'flame' },
  ];

  constructor() {
    void this.notificationStore.loadUnreadCount();
    void this.messageStore.loadConversations();
    void this.gamificationStore.loadProgress();

    // Presence hub: heartbeat (keeps the server presence key alive) + live
    // notifications for the whole authenticated session.
    void this.presence.connect();
    this.presence.onNotification
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((n) => this.notificationStore.addRealtime(n));
    this.destroyRef.onDestroy(() => void this.presence.disconnect());
  }

  protected toggleTheme(): void {
    this.themeService.toggle();
  }

  protected async logout(): Promise<void> {
    this.showUserMenu.set(false);
    await this.auth.logout();
    await this.router.navigate(['/']);
  }
}
