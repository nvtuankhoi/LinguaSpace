import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { ModerationApi } from '../../../core/api/moderation.api';
import { ReportTargetType } from '../../../core/models';
import { IconComponent } from '../icon/icon.component';

/**
 * Reusable "Report content" modal. Submit a moderation report for a user, post,
 * room, or message via ModerationApi. Emits `closed` when the user dismisses or
 * after a successful submission.
 */
@Component({
  selector: 'app-report-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent],
  templateUrl: './report-dialog.component.html',
  styleUrl: './report-dialog.component.scss',
})
export class ReportDialogComponent {
  private readonly api = inject(ModerationApi);
  private readonly fb = inject(FormBuilder);

  readonly targetType = input.required<ReportTargetType>();
  readonly targetId = input.required<string>();
  readonly targetLabel = input<string>('');
  readonly closed = output<void>();

  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(500)]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.api.report({
          targetType: this.targetType(),
          targetId: this.targetId(),
          reason: this.form.getRawValue().reason.trim(),
        }),
      );
      this.submitted.set(true);
    } catch {
      this.error.set('Could not submit your report. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
