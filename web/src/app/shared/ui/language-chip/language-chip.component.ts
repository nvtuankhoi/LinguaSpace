import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Language chip — teal wash pill, e.g. "JA · B1". */
@Component({
  selector: 'app-lang-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="chip">{{ label() }}</span>`,
})
export class LanguageChipComponent {
  readonly code = input<string | null>(null);
  readonly level = input<string | null>(null);

  protected readonly label = computed(() => {
    const code = (this.code() ?? '').toUpperCase();
    const level = this.level();
    return level ? `${code} · ${level}` : code;
  });
}
