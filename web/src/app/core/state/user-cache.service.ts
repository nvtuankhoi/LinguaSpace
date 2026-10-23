import { inject, Injectable, signal } from '@angular/core';

import { UsersApi } from '../api/users.api';
import { UserProfileDto } from '../models';

/**
 * Caches user profiles by id so feed cards, room participants, DMs, and
 * notifications can resolve author/actor display data without N+1 refetches.
 */
@Injectable({ providedIn: 'root' })
export class UserCache {
  private readonly api = inject(UsersApi);
  private readonly _users = signal<Record<string, UserProfileDto>>({});

  readonly users = this._users.asReadonly();

  user(id: string): UserProfileDto | undefined {
    return this._users()[id];
  }

  /** Fetches the profile if it is not already cached. */
  ensure(id: string): void {
    if (!id || this._users()[id]) {
      return;
    }
    this.api.getUser(id).subscribe((u) => this._users.update((map) => ({ ...map, [id]: u })));
  }
}
