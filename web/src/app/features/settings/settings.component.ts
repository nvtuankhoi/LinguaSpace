import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/api/auth.api';
import { AuthStore } from '../../core/auth/auth.store';
import { ActiveSessionDto } from '../../core/models';
import { relativeTime } from '../../core/util/time';
import { IconComponent } from '../../shared/ui/icon/icon.component';

interface Feedback {
  kind: 'ok' | 'err';
  text: string;
}

@Component({
  selector: 'app-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  private readonly authApi = inject(AuthApi);
  private readonly auth = inject(AuthStore);
  private readonly fb = inject(FormBuilder);

  protected readonly user = this.auth.user;
  protected readonly sessions = signal<ActiveSessionDto[]>([]);

  protected readonly verifyMsg = signal<string | null>(null);
  protected readonly pwMsg = signal<Feedback | null>(null);
  protected readonly emailMsg = signal<Feedback | null>(null);

  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]],
  });

  protected readonly emailForm = this.fb.nonNullable.group({
    newEmail: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected readonly verifyForm = this.fb.nonNullable.group({
    token: ['', [Validators.required]],
  });

  async ngOnInit(): Promise<void> {
    await this.loadSessions();
  }

  protected async loadSessions(): Promise<void> {
    try {
      this.sessions.set(await firstValueFrom(this.authApi.getSessions()));
    } catch {
      /* silent — sessions are non-critical */
    }
  }

  protected async resendVerification(): Promise<void> {
    try {
      await firstValueFrom(this.authApi.resendVerification());
      this.verifyMsg.set('Verification email sent. Check your inbox.');
    } catch {
      this.verifyMsg.set('Could not send verification email.');
    }
  }

  /** Manually submit a verification token (e.g. copied from a dev log or email).
   *  The same endpoint is consumed automatically by the /verify-email link page. */
  protected async verifyToken(): Promise<void> {
    if (this.verifyForm.invalid) {
      this.verifyForm.markAllAsTouched();
      return;
    }
    const token = this.verifyForm.getRawValue().token.trim();
    this.verifyMsg.set(null);
    try {
      await firstValueFrom(this.authApi.verifyEmail({ token }));
      // Refresh the cached user so isEmailConfirmed flips immediately.
      await this.auth.loadCurrentUser();
      this.verifyForm.reset();
      this.verifyMsg.set('Email verified. Thank you!');
    } catch {
      this.verifyMsg.set('Could not verify email. The token may be invalid or expired.');
    }
  }

  protected async changePassword(): Promise<void> {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const v = this.passwordForm.getRawValue();
    if (v.newPassword !== v.confirm) {
      this.pwMsg.set({ kind: 'err', text: 'New passwords do not match.' });
      return;
    }
    this.pwMsg.set(null);
    try {
      await firstValueFrom(
        this.authApi.changePassword({ currentPassword: v.currentPassword, newPassword: v.newPassword }),
      );
      this.passwordForm.reset();
      this.pwMsg.set({ kind: 'ok', text: 'Password changed.' });
    } catch {
      this.pwMsg.set({ kind: 'err', text: 'Could not change password. Check your current password.' });
    }
  }

  protected async changeEmail(): Promise<void> {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }
    const v = this.emailForm.getRawValue();
    this.emailMsg.set(null);
    try {
      await firstValueFrom(this.authApi.changeEmail({ newEmail: v.newEmail, password: v.password }));
      this.emailForm.reset();
      this.emailMsg.set({ kind: 'ok', text: 'Email change requested. Verify your new email to complete.' });
    } catch {
      this.emailMsg.set({ kind: 'err', text: 'Could not change email.' });
    }
  }

  protected async revokeSession(id: number): Promise<void> {
    try {
      await firstValueFrom(this.authApi.revokeSession(id));
      this.sessions.update((list) => list.filter((s) => s.id !== id));
    } catch {
      /* silent */
    }
  }

  protected async revokeAll(): Promise<void> {
    if (!confirm('Sign out of all other sessions?')) {
      return;
    }
    try {
      await firstValueFrom(this.authApi.revokeAllSessions());
      await this.loadSessions();
    } catch {
      /* silent */
    }
  }

  protected time(iso: string): string {
    return relativeTime(iso);
  }
}
