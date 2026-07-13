import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { FeedStore } from '../../core/state/feed.store';
import { LANGUAGES } from '../../core/util/languages';
import { PostMetadataDto } from '../../core/models';
import { PostType } from '../../core/models/enums';

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
  protected readonly postTypes: { value: PostType; label: string }[] = [
    { value: 'Text', label: 'Text' },
    { value: 'VocabCard', label: 'Vocab card' },
  ];

  protected readonly form = this.fb.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(1000)]],
    postType: ['Text' as PostType],
    backText: ['', [Validators.maxLength(500)]],
    pronunciation: ['', [Validators.maxLength(120)]],
    example: ['', [Validators.maxLength(500)]],
    languageCode: [''],
    tags: [''],
  });

  protected readonly isVocab = computed(() => this.form.controls.postType.value === 'VocabCard');

  /** Textarea placeholder adapts to the selected post type. */
  protected readonly contentPlaceholder = computed(() =>
    this.isVocab()
      ? 'Word or phrase (target language)'
      : 'Share something you practised, a question, or a vocab card…',
  );

  protected selectType(type: PostType): void {
    this.form.controls.postType.setValue(type);
  }

  protected onContentInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.charCount.set(value.length);
    // Auto-grow: 2 rows base, add 1 row per ~60 chars, max 8 rows
    const lines = value.split('\n').length;
    const estimatedRows = Math.max(2, Math.min(8, lines + Math.floor(value.length / 60)));
    this.rows.set(estimatedRows);
  }

  protected async submit(): Promise<void> {
    // Content is required for both; a vocab card also needs a meaning (back).
    const backMissing = this.isVocab() && !this.form.controls.backText.value.trim();
    if (this.form.controls.content.invalid || backMissing) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    this.sending.set(true);
    try {
      const metadata: PostMetadataDto | null = this.isVocab()
        ? {
            audioUrl: null,
            durationSeconds: null,
            thumbnailUrl: null,
            linkUrl: null,
            linkTitle: null,
            linkDescription: null,
            backText: value.backText.trim(),
            pronunciation: value.pronunciation.trim() || null,
            example: value.example.trim() || null,
          }
        : null;

      await this.feed.createPost({
        content: value.content.trim(),
        postType: value.postType,
        languageCode: value.languageCode || null,
        metadata,
        tags: value.tags
          ? value.tags.split(',').map((t) => t.trim()).filter(Boolean).slice(0, 5)
          : undefined,
      });
      this.form.reset({
        content: '',
        postType: 'Text',
        backText: '',
        pronunciation: '',
        example: '',
        languageCode: '',
        tags: '',
      });
      this.charCount.set(0);
      this.rows.set(2);
    } finally {
      this.sending.set(false);
    }
  }
}
