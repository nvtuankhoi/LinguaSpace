import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FriendsStore } from '../../core/state/friends.store';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';

export type FriendsTab = 'friends' | 'followers' | 'following' | 'requests' | 'blocked';

@Component({
  selector: 'app-friends',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AvatarComponent, IconComponent],
  templateUrl: './friends.component.html',
  styleUrl: './friends.component.scss',
})
export class FriendsComponent {
  protected readonly store = inject(FriendsStore);

  protected readonly tab = signal<FriendsTab>('friends');

  constructor() {
    void this.store.loadAll();
  }

  setTab(t: FriendsTab): void {
    this.tab.set(t);
  }

  acceptRequest(id: number): void {
    void this.store.respondToRequest(id, true);
  }

  declineRequest(id: number): void {
    void this.store.respondToRequest(id, false);
  }

  unfriend(userId: string): void {
    void this.store.removeFriend(userId);
  }

  unfollow(userId: string): void {
    void this.store.unfollow(userId);
  }

  cancelRequest(id: number): void {
    void this.store.cancelRequest(id);
  }

  unblock(userId: string): void {
    void this.store.unblock(userId);
  }
}
