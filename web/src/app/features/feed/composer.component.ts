import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { FeedStore } from '../../core/state/feed.store';
import { LANGUAGES } from '../../core/util/languages';

@Component({
  selector: 'app-composer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './composer.component.html',
  styleUrl: './composer.component.scss',
})
export class ComposerComponent {
  private readonly fb = inject(FormBuilder);
  private readonly feed = inject(FeedStore);

  protected readonly sending = signal(false);
  protected readonly charCount = signal(0);
  protected readonly rows = signal(2);

  protected readonly languages = LANGUAGES;

  protected readonly form = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]],
    languageCode: [''],
    tags: [''],
  });

  protected onContentInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.charCount.set(value.length);
    // Auto-grow: 2 rows base, add 1 row per ~60 chars, max 8 rows
    const lines = value.split('\n').length;
    const estimatedRows = Math.max(2, Math.min(8, lines + Math.floor(value.length / 60)));
    this.rows.set(estimatedRows);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.sending.set(true);
    try {
      await this.feed.createPost({
        content: value.content.trim(),
        postType: 'Text',
        languageCode: value.languageCode || null,
        tags: value.tags
          ? value.tags.split(',').map((t) => t.trim()).filter(Boolean).slice(0, 5)
          : undefined,
      });
      this.form.reset({ content: '', languageCode: '', tags: '' });
      this.charCount.set(0);
      this.rows.set(2);
    } finally {
      this.sending.set(false);
    }
  }
}
