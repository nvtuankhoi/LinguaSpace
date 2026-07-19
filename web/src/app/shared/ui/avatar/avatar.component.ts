import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="av" [style.width.px]="size()" [style.height.px]="size()">
      @if (src()) {
        <img [src]="src()" [alt]="name()" />
      } @else {
        <span class="initial" [style.font-size.px]="size() * 0.4">{{ initial() }}</span>
      }
      @if (presence()) {
        <span class="dot" [class.online]="online()" [class.offline]="!online()"></span>
      }
    </span>
  `,
  styles: [`
    .av {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border-radius: var(--ls-radius-pill);
      background: var(--ls-primary-tint);
      color: var(--ls-primary-strong);
      font-weight: var(--ls-weight-semibold);
      overflow: hidden;
      flex-shrink: 0;
    }
    img { width: 100%; height: 100%; object-fit: cover; }
    .dot {
      position: absolute;
      right: 0;
      bottom: 0;
      width: 30%;
      height: 30%;
      min-width: 10px;
      min-height: 10px;
      border-radius: var(--ls-radius-pill);
      box-shadow: 0 0 0 2px var(--ls-surface);
    }
    .dot.online { background: var(--ls-success); }
    .dot.offline { background: var(--ls-border-strong); }
  `],
})
export class AvatarComponent {
  readonly name = input<string>('');
  readonly src = input<string | null>(null);
  readonly online = input<boolean>(false);
  readonly presence = input<boolean>(false);
  readonly size = input<number>(40);

  protected readonly initial = computed(() => this.name().trim().charAt(0).toUpperCase() || '?');
}
