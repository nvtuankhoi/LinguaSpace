import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

/**
 * Renders a vocabulary card that flips to reveal the translation.
 *
 * `front` is the target-language word (the post's `content`); `back` is its
 * meaning (`metadata.backText`). Pronunciation + example sentence are optional.
 * Flips on click or Enter/Space when focused.
 */
@Component({
  selector: 'app-vocab-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vocab-card.component.html',
  styleUrl: './vocab-card.component.scss',
})
export class VocabCardComponent {
  readonly front = input.required<string>();
  readonly back = input.required<string>();
  readonly pronunciation = input<string | null>(null);
  readonly example = input<string | null>(null);

  protected readonly flipped = signal(false);

  protected toggle(): void {
    this.flipped.update((f) => !f);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.toggle();
    }
  }
}
