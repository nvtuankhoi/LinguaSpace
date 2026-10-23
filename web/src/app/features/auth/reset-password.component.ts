import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../../core/api/auth.api';
import { IconComponent } from '../../shared/ui/icon/icon.component';

interface ResetMsg {
  kind: 'ok' | 'err';
  text: string;
}

@Component({
  selector: 'app-reset-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, IconComponent],
  templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApi);

  protected readonly token = signal(this.route.snapshot.queryParamMap.get('token') ?? '');
  protected readonly msg = signal<ResetMsg | null>(null);
  protected readonly submitting = signal(false);
  protected readonly done = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    if (v.newPassword !== v.confirm) {
      this.msg.set({ kind: 'err', text: 'Passwords do not match.' });
      return;
    }
    if (!this.token()) {
      this.msg.set({ kind: 'err', text: 'Invalid or missing reset token.' });
      return;
    }
    this.submitting.set(true);
    this.msg.set(null);
    try {
      await firstValueFrom(this.authApi.resetPassword({ token: this.token(), newPassword: v.newPassword }));
      this.done.set(true);
    } catch {
      this.msg.set({ kind: 'err', text: 'Could not reset password. The link may have expired.' });
    } finally {
      this.submitting.set(false);
    }
  }
}
