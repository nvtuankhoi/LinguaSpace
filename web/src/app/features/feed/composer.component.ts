import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { FeedStore } from '../../core/state/feed.store';
import { MediaApi } from '../../core/api/media.api';
import { LANGUAGES } from '../../core/util/languages';
import { PostMetadataDto, UploadedFileResponse } from '../../core/models';
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
  private readonly mediaApi = inject(MediaApi);

  protected readonly sending = signal(false);
  protected readonly charCount = signal(0);
  protected readonly rows = signal(2);
  protected readonly uploadingMedia = signal(false);
  protected readonly mediaError = signal<string | null>(null);
  protected readonly media = signal<UploadedFileResponse[]>([]);

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

  protected async onMediaSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = ''; // reset so the same file can be re-selected after removal
    if (!files.length) {
      return;
    }
    if (this.media().length + files.length > 4) {
      this.mediaError.set('Maximum 4 media items per post.');
      return;
    }

    this.mediaError.set(null);
    this.uploadingMedia.set(true);
    try {
      const uploaded = await firstValueFrom(this.mediaApi.uploadPostMedia(files));
      this.media.update((current) => [...current, ...uploaded]);
    } catch {
      this.mediaError.set('Upload failed. Please try again.');
    } finally {
      this.uploadingMedia.set(false);
    }
  }

  protected removeMedia(index: number): void {
    this.media.update((current) => current.filter((_, i) => i !== index));
  }

  /** Infers image vs video from the stored URL's extension (GUID filenames keep the ext). */
  protected mediaKind(url: string): 'image' | 'video' {
    return /\.(mp4|webm)$/i.test(url) ? 'video' : 'image';
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
        mediaUrls: this.media().length ? this.media().map((m) => m.url) : undefined,
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
      this.media.set([]);
      this.charCount.set(0);
      this.rows.set(2);
    } finally {
      this.sending.set(false);
    }
  }
}
