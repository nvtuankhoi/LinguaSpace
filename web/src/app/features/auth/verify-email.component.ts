import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/api/auth.api';
import { AuthStore } from '../../core/auth/auth.store';
import { IconComponent } from '../../shared/ui/icon/icon.component';

type Status = 'verifying' | 'ok' | 'err';

/**
 * Consumes an email-verification link: reads the `token` query param the
 * verification email carries (https://…/verify-email?userId=…&token=…) and
 * submits it to POST /Auth/verify-email. The route sits behind authGuard, so
 * the user is authenticated by the time this inits; a logged-out click is sent
 * to /login with returnUrl so the token survives the login round-trip.
 */
@Component({
  selector: 'app-verify-email',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  templateUrl: './verify-email.component.html',
})
export class VerifyEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApi);
  private readonly auth = inject(AuthStore);

  protected readonly status = signal<Status>('verifying');

  async ngOnInit(): Promise<void> {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.status.set('err');
      return;
    }
    try {
      await firstValueFrom(this.authApi.verifyEmail({ token }));
      // Refresh the cached user so isEmailConfirmed flips across the app.
      await this.auth.loadCurrentUser();
      this.status.set('ok');
    } catch {
      this.status.set('err');
    }
  }
}
