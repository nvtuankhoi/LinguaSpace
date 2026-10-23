import { effect, inject, Injectable, Injector, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'ls-theme';
const VALID_THEMES: readonly Theme[] = ['light', 'dark'];

/**
 * Owns the light/dark theme. The initial value is read from the `data-theme`
 * attribute that index.html sets before paint (no FOUC); afterwards this
 * service keeps the attribute and localStorage in sync.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly injector = inject(Injector);

  readonly theme = signal<Theme>(this.readInitial());

  constructor() {
    effect(() => {
      const value = this.theme();
      if (typeof document !== 'undefined') {
        document.documentElement.setAttribute('data-theme', value);
      }
      try {
        localStorage.setItem(STORAGE_KEY, value);
      } catch {
        /* storage unavailable (private mode) — ignore */
      }
    }, { injector: this.injector });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  set(theme: Theme): void {
    this.theme.set(theme);
  }

  private readInitial(): Theme {
    if (typeof document !== 'undefined') {
      const attr = document.documentElement.getAttribute('data-theme');
      if (VALID_THEMES.includes(attr as Theme)) {
        return attr as Theme;
      }
    }
    return 'dark';
  }
}
