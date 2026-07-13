import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/api/auth.api';
import { AuthStore } from '../../core/auth/auth.store';
import { IconComponent } from '../../shared/ui/icon/icon.component';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, IconComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /**
   * Where to go after a successful login. authGuard stashes the originally
   * requested URL (incl. query params) as ?returnUrl for deep links such as the
   * email-verification link. Guard against open-redirect: only accept a
   * same-origin path (starts with '/', not '//').
   */
  private afterLoginReturnUrl(): string {
    const raw = this.route.snapshot.queryParamMap.get('returnUrl');
    return raw && raw.startsWith('/') && !raw.startsWith('//') ? raw : '/app/feed';
  }

  protected readonly loading = this.auth.isLoading;
  protected readonly error = this.auth.error;

  protected readonly showForgot = signal(false);
  protected readonly forgotMsg = signal<string | null>(null);
  protected readonly forgotForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    try {
      await this.auth.login(this.form.getRawValue());
      await this.router.navigateByUrl(this.afterLoginReturnUrl());
    } catch {
      /* error surfaced via the store signal */
    }
  }

  protected async submitForgot(): Promise<void> {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }
    // Always responds the same (no email enumeration) — show the message even on error.
    this.forgotMsg.set('If that email exists, a reset link has been sent.');
    try {
      await firstValueFrom(this.authApi.forgotPassword(this.forgotForm.getRawValue().email));
    } catch {
      /* swallow — the message is already set */
    }
  }

  /** One-click demo sign-in (mock only). Real Google uses POST /api/Auth/oauth/google. */
  protected async demoGoogle(): Promise<void> {
    try {
      await this.auth.login({ email: 'demo@lingua.space', password: 'Password1!' });
      await this.router.navigateByUrl(this.afterLoginReturnUrl());
    } catch {
      /* error surfaced via the store signal */
    }
  }
}
